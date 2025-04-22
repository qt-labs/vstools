// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Editors
{
    using Core;
    using Core.MsBuild;
    using Core.Options;
    using QtVsTools.Core.CMake;
    using QtVsTools.Core.Common;
    using VisualStudio;

    public interface IFileTypeSniffer
    {
        bool IsSupportedFile(string filePath);
    }

    public abstract class Editor : IVsEditorFactory
    {
        public abstract Guid Guid { get; }
        public abstract string ExecutableName { get; }

        public virtual Func<string, bool> WindowFilter => _ => true;

        protected virtual string GetTitle(Process editorProcess)
        {
            return editorProcess.StartInfo.FileName;
        }

        private readonly IFileTypeSniffer fileTypeSniffer;

        protected Editor(IFileTypeSniffer fileSniffer)
        {
            fileTypeSniffer = fileSniffer ?? throw new ArgumentNullException(nameof(fileSniffer));
        }

        protected virtual string GetToolsPath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetQtToolsPath() ?? GetDefaultQtToolsPath();
        }

        protected IVsHierarchy Context { get; private set; }
        protected uint ItemId { get; private set; }

        private string GetQtToolsPath()
        {
            return ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (CMakeProject.ActiveProject is {} cMakeProject) {
                    var path = cMakeProject["cmake", "CMAKE_PREFIX_PATH"];
                    return string.IsNullOrWhiteSpace(path) ? null : Path.Combine(path, "bin");
                }

                if (Context == null)
                    return null;
                if (MsBuildProject.GetOrAdd(VsShell.GetProject(Context)) is not {} project)
                    return null;
                var qtToolsPath = project.GetPropertyValue("QtToolsPath");
                return string.IsNullOrWhiteSpace(qtToolsPath) ? null : qtToolsPath;
            });
        }

        private static string GetDefaultQtToolsPath()
        {
            var defaultVersion = QtVersionManager.GetDefaultVersion();
            var defaultVersionInfo = VersionInformation.GetOrAddByName(defaultVersion);
            return string.IsNullOrEmpty(defaultVersionInfo?.QtDir)
                ? null
                : Path.Combine(defaultVersionInfo.QtDir, "bin");
        }

        [EnvironmentPermission(SecurityAction.Demand, Unrestricted = true)]
        public virtual int CreateEditorInstance(
            uint grfCreateDoc,
            string pszMkDocument,
            string pszPhysicalView,
            IVsHierarchy pvHier,
            uint itemid,
            IntPtr punkDocDataExisting,
            out IntPtr ppunkDocView,
            out IntPtr ppunkDocData,
            out string pbstrEditorCaption,
            out Guid pguidCmdUI,
            out int pgrfCDW)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Initialize to null
            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pguidCmdUI = Guid;
            pgrfCDW = 0;
            pbstrEditorCaption = null;

            // Validate inputs
            if (!fileTypeSniffer.IsSupportedFile(pszMkDocument))
                return VSConstants.VS_E_UNSUPPORTEDFORMAT;

            if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0) {
                return VSConstants.E_INVALIDARG;
            }
            if (punkDocDataExisting != IntPtr.Zero) {
                return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
            }
            Context = pvHier;
            ItemId = itemid;

            var toolsPath = GetToolsPath();
            if (string.IsNullOrEmpty(toolsPath))
                return VSConstants.VS_E_INCOMPATIBLEDOCDATA;

            // Create the Document (editor)
            var newEditor = new EditorPane(this, toolsPath);
            ppunkDocView = Marshal.GetIUnknownForObject(newEditor);
            ppunkDocData = Marshal.GetIUnknownForObject(newEditor);
            pbstrEditorCaption = "";
            pgrfCDW = (int)(_VSRDTFLAGS.RDT_CantSave | _VSRDTFLAGS.RDT_DontAutoOpen);

            return VSConstants.S_OK;
        }

        public virtual int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            return VSConstants.S_OK;
        }

        public virtual int Close()
        {
            return VSConstants.S_OK;
        }

        public virtual int MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
        {
            pbstrPhysicalView = null;   // initialize out parameter

            // we support only a single physical view
            return VSConstants.LOGVIEWID_Primary == rguidLogicalView
                ? VSConstants.S_OK // primary view uses NULL as pbstrPhysicalView
                : VSConstants.E_NOTIMPL; // return E_NOTIMPL for any unrecognized rguidLogicalView
        }

        protected virtual Dictionary<string, (string, bool)> GetArguments(string filePath)
        {
            var result = new Dictionary<string, (string Value, bool WithOption)>
            {
                { "filepath", (Value: Utils.SafeQuote(filePath), WithOption: false) }
            };

            var styleValue = QtOptionsPage.ColorTheme switch
            {
                QtOptionsPage.EditorColorTheme.Dark => (Value: "fusion", WithOption: true),
                QtOptionsPage.EditorColorTheme.Consistent when VSColorTheme
                    .GetThemedColor(EnvironmentColors.EditorExpansionFillBrushKey)
                    .GetBrightness() < 0.5f => (Value: "fusion", WithOption: true),
                _ => default((string Value, bool WithOption)?)
            };
            if (styleValue.HasValue)
                result["style"] = styleValue.Value;

            if (!string.IsNullOrEmpty(QtOptionsPage.StylesheetPath)) {
                result["stylesheet"] = (Value: Utils.SafeQuote(QtOptionsPage.StylesheetPath),
                    WithOption: true);
            } else if (!Detached) {
                // Hack: Apply stylesheet resizing for embedded window widgets to reasonable defaults.
                var tempPath = Path.Combine(Path.GetTempPath(), "default.qss");
                if (!File.Exists(tempPath)) {
                    using var writer = new StreamWriter(tempPath);
                    writer.WriteLine("QTreeView { min-width: 256; min-height: 256 }");
                }
                result["stylesheet"] = (Value: Utils.SafeQuote(tempPath), WithOption: true);
            }

            return result;
        }

        protected virtual ProcessStartInfo GetStartInfo(string qtToolsPath,
            Dictionary<string, (string Value, bool WithOption)> arguments, bool hideWindow)
        {
            var argsList = new List<string>();

            foreach (var option in arguments.Keys) {
                var (value, withOption) = arguments[option];
                switch (withOption) {
                case true when !string.IsNullOrWhiteSpace(value):
                    argsList.Add($"-{option} {value}");
                    break;
                case false when !string.IsNullOrWhiteSpace(value):
                    argsList.Add(value);
                    break;
                }
            }

            return new ProcessStartInfo
            {
                FileName = Path.GetFullPath(Path.Combine(qtToolsPath, ExecutableName)),
                Arguments = string.Join(" ", argsList),
                WindowStyle = hideWindow ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
            };
        }

        public virtual Process Start(
            string filePath = "",
            string qtToolsPath = null,
            bool hideWindow = true)
        {
            if (string.IsNullOrEmpty(qtToolsPath))
                qtToolsPath = GetDefaultQtToolsPath();
            var arguments = GetArguments(filePath);
            var st = GetStartInfo(qtToolsPath, arguments, hideWindow);
            try {
                var process = Process.Start(st);
                SubprocessTracker.AddProcess(process);
                return process;
            } catch (Exception exception) {
                exception.Log();
                if (!string.IsNullOrWhiteSpace(filePath) && !File.Exists(filePath))
                    Messages.Print($"The system cannot find the file: '{filePath}'");
                if (!File.Exists(st.FileName))
                    Messages.Print($"The system cannot find the file: '{st.FileName}'");
                if (arguments.TryGetValue("stylesheet", out (string Path, bool) stylesheet)) {
                    var path = Utils.Unquote(stylesheet.Path);
                    if (!string.IsNullOrWhiteSpace(path) && !File.Exists(path))
                        Messages.Print($"The system cannot find the file: '{path}'");
                }
                return null;
            }
        }

        protected virtual void OnStart(Process process)
        {
        }

        protected virtual void OnExit(Process process)
        {
        }

        protected virtual bool Detached => false;
        protected virtual bool ShowDetachNotification => true;
        protected abstract void DisableDetachNotification();

        private class EditorPane : WindowPane, IVsPersistDocData
        {
            private Editor Editor { get; }
            private string QtToolsPath { get; }

            private TableLayoutPanel EditorContainer { get; set; }
            private Panel EditorControl { get; }
            public override IWin32Window Window => EditorContainer;

            private Process EditorProcess { get; set; }
            private IntPtr EditorWindow { get; set; }
            private int EditorWindowStyle { get; set; }
            private int EditorWindowStyleExt { get; set; }
            private IntPtr EditorIcon { get; set; }

            private NotifyDetach NotifyDetach { get; set; }

            public EditorPane(Editor editor, string qtToolsPath)
            {
                Editor = editor;
                QtToolsPath = qtToolsPath;

                EditorControl = new Panel
                {
                    BackColor = SystemColors.Window,
                    Dock = DockStyle.Fill
                };
                EditorControl.Margin = Padding.Empty;
                EditorControl.Resize += EditorControl_Resize;

                EditorContainer = new TableLayoutPanel
                {
                    ColumnCount = 1,
                    RowCount = 1
                };
                EditorContainer.ColumnStyles.Add(new ColumnStyle());
                EditorContainer.RowStyles.Add(new RowStyle());
                EditorContainer.Controls.Add(EditorControl, 0, 0);
            }

            protected override void Dispose(bool disposing)
            {
                try {
                    if (disposing) {
                        EditorContainer?.Dispose();
                        EditorContainer = null;
                        GC.SuppressFinalize(this);
                    }
                } finally {
                    base.Dispose(disposing);
                }
            }

            int IVsPersistDocData.GetGuidEditorType(out Guid pClassID)
            {
                pClassID = Editor.Guid;
                return VSConstants.S_OK;
            }

            int IVsPersistDocData.LoadDocData(string pszMkDocument)
            {
                EditorProcess = Editor.Start(pszMkDocument, QtToolsPath,
                    hideWindow: !Editor.Detached);
                if (EditorProcess == null)
                    return VSConstants.E_FAIL;

                if (Editor.Detached) {
                    Editor.OnStart(EditorProcess);
                    CloseParentFrame();
                    return VSConstants.S_OK;
                }
                if (Editor.ShowDetachNotification)
                    ShowDetachBar();

                EditorProcess.WaitForInputIdle();
                EditorProcess.EnableRaisingEvents = true;
                EditorProcess.Exited += EditorProcess_Exited;

                var t = Stopwatch.StartNew();
                while (EditorWindow == IntPtr.Zero && t.ElapsedMilliseconds < 5000) {
                    var windows = new Dictionary<IntPtr, string>();
                    foreach (ProcessThread thread in EditorProcess.Threads) {
                        NativeAPI.EnumThreadWindows(
                            dwThreadId: (uint)thread.Id,
                            lParam: IntPtr.Zero,
                            lpfn: (hWnd, _) =>
                            {
                                windows.Add(hWnd, NativeAPI.GetWindowCaption(hWnd));
                                return true;
                            });
                    }

                    EditorWindow = windows
                        .Where(w => Editor.WindowFilter(w.Value))
                        .Select(w => w.Key)
                        .FirstOrDefault();

                    if (EditorWindow == IntPtr.Zero)
                        Thread.Sleep(100);
                }

                if (EditorWindow == IntPtr.Zero) {
                    EditorProcess.Kill();
                    EditorProcess = null;
                    return VSConstants.E_FAIL;
                }

                // Save editor window styles and icon
                EditorWindowStyle = NativeAPI.GetWindowLong(
                    EditorWindow, NativeAPI.GWL_STYLE);
                EditorWindowStyleExt = NativeAPI.GetWindowLong(
                    EditorWindow, NativeAPI.GWL_EXSTYLE);
                EditorIcon = NativeAPI.SendMessage(
                    EditorWindow, NativeAPI.WM_GETICON, NativeAPI.ICON_SMALL, 0);
                if (EditorIcon == IntPtr.Zero)
                    EditorIcon = (IntPtr)NativeAPI.GetClassLong(EditorWindow, NativeAPI.GCL_HICON);
                if (EditorIcon == IntPtr.Zero)
                    EditorIcon = (IntPtr)NativeAPI.GetClassLong(EditorWindow, NativeAPI.GCL_HICONSM);

                // Move editor window inside VS
                if (NativeAPI.SetParent(
                        EditorWindow, EditorControl.Handle) == IntPtr.Zero) {
                    EditorWindow = IntPtr.Zero;
                    EditorProcess.Kill();
                    EditorProcess = null;
                    return VSConstants.E_FAIL;
                }
                if (NativeAPI.SetWindowLong(
                        EditorWindow, NativeAPI.GWL_STYLE, NativeAPI.WS_VISIBLE) == 0) {
                    EditorWindow = IntPtr.Zero;
                    EditorProcess.Kill();
                    EditorProcess = null;
                    return VSConstants.E_FAIL;
                }
                if (!NativeAPI.MoveWindow(
                        EditorWindow, 0, 0, EditorControl.Width, EditorControl.Height, true)) {
                    EditorWindow = IntPtr.Zero;
                    EditorProcess.Kill();
                    EditorProcess = null;
                    return VSConstants.E_FAIL;
                }

                Editor.OnStart(EditorProcess);
                return VSConstants.S_OK;
            }

            void CloseParentFrame()
            {
                EditorProcess = null;
                EditorWindow = IntPtr.Zero;
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var parentFrame = GetService(typeof(SVsWindowFrame)) as IVsWindowFrame;
                    parentFrame?.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                });
            }

            private void EditorProcess_Exited(object sender, EventArgs e)
            {
                CloseParentFrame();
                Editor.OnExit(EditorProcess);
            }

            void EditorControl_Resize(object sender, EventArgs e)
            {
                if (EditorControl != null && EditorWindow != IntPtr.Zero) {
                    NativeAPI.MoveWindow(
                        EditorWindow, 0, 0, EditorControl.Width, EditorControl.Height, true);
                }
            }

            private void ShowDetachBar()
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    if (GetService(typeof(SVsWindowFrame)) is not IVsWindowFrame parentFrame)
                        return;

                    var result = parentFrame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost,
                        out var value);
                    if (ErrorHandler.Failed(result) || value is not IVsInfoBarHost infoBarHost)
                        return;

                    NotifyDetach?.Close();
                    NotifyDetach = new NotifyDetach(Detach, Editor.DisableDetachNotification,
                        infoBarHost);
                    NotifyDetach.Show();
                });
            }

            public void Detach()
            {
                if (EditorProcess != null) {
                    var editorWindow = EditorWindow;
                    DetachEditorWindow();
                    CloseParentFrame();
                    NativeAPI.ShowWindow(editorWindow, NativeAPI.SW_RESTORE);
                    NativeAPI.SetForegroundWindow(editorWindow);
                }
            }

            public void DetachEditorWindow()
            {
                NativeAPI.ShowWindow(EditorWindow,
                    NativeAPI.SW_HIDE);
                NativeAPI.SetParent(
                    EditorWindow, IntPtr.Zero);
                NativeAPI.SetWindowLong(
                    EditorWindow, NativeAPI.GWL_STYLE, EditorWindowStyle);
                NativeAPI.SetWindowLong(
                    EditorWindow, NativeAPI.GWL_EXSTYLE, EditorWindowStyleExt);
                NativeAPI.SendMessage(
                    EditorWindow, NativeAPI.WM_SETICON, NativeAPI.ICON_SMALL, EditorIcon);
                NativeAPI.MoveWindow(
                    EditorWindow, 0, 0, EditorControl.Width, EditorControl.Height, true);
                NativeAPI.ShowWindow(EditorWindow,
                    NativeAPI.SW_SHOWMINNOACTIVE);
            }

            int IVsPersistDocData.Close()
            {
                if (EditorProcess != null) {
                    DetachEditorWindow();
                    var editorProcess = EditorProcess;
                    EditorProcess = null;
                    var editorWindow = EditorWindow;
                    EditorWindow = IntPtr.Zero;

                    // Close editor window
                    _ = System.Threading.Tasks.Task.Run(() =>
                    {
                        NativeAPI.SendMessage(editorWindow, NativeAPI.WM_CLOSE, 0, 0);
                        if (!editorProcess.WaitForExit(500)) {
                            NativeAPI.ShowWindow(editorWindow,
                                NativeAPI.SW_RESTORE);
                            NativeAPI.SetForegroundWindow(editorWindow);
                        }
                    });
                }

                if (EditorContainer == null) {
                    if (EditorContainer != null) {
                        EditorContainer.Dispose();
                        EditorContainer = null;
                    }
                }
                return VSConstants.S_OK;
            }

            int IVsPersistDocData.RenameDocData(
                uint grfAttribs,
                IVsHierarchy pHierNew,
                uint itemidNew,
                string pszMkDocumentNew)
            {
                return VSConstants.E_NOTIMPL;
            }

            int IVsPersistDocData.IsDocDataDirty(out int pfDirty)
            {
                pfDirty = 0;
                return VSConstants.S_OK;
            }

            int IVsPersistDocData.SetUntitledDocPath(string pszDocDataPath)
            {
                return VSConstants.S_OK;
            }

            int IVsPersistDocData.SaveDocData(
                VSSAVEFLAGS dwSave,
                out string pbstrMkDocumentNew,
                out int pfSaveCanceled)
            {
                pbstrMkDocumentNew = string.Empty;
                pfSaveCanceled = 0;
                return VSConstants.E_NOTIMPL;
            }

            int IVsPersistDocData.OnRegisterDocData(
                uint docCookie,
                IVsHierarchy pHierNew,
                uint itemidNew)
            {
                return VSConstants.S_OK;
            }

            int IVsPersistDocData.IsDocDataReloadable(out int pfReloadable)
            {
                pfReloadable = 0;
                return VSConstants.S_OK;
            }

            int IVsPersistDocData.ReloadDocData(uint grfFlags)
            {
                return VSConstants.E_NOTIMPL;
            }
        }
    }
}
