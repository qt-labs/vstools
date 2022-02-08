/****************************************************************************
**
** Copyright (C) 2020 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using QtVsTools.Core;
using QtVsTools.VisualStudio;

namespace QtVsTools.Editors
{
    using static Core.HelperFunctions;

    public abstract class Editor : IVsEditorFactory
    {
        public abstract Guid Guid { get; }
        public abstract string ExecutableName { get; }

        public virtual Func<string, bool> WindowFilter => (caption => true);

        protected virtual string GetTitle(Process editorProcess)
        {
            return editorProcess.StartInfo.FileName;
        }

        protected virtual string GetToolsPath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetQtToolsPath() ?? GetDefaultQtToolsPath();
        }

        protected IVsHierarchy Context { get; private set; }
        protected uint ItemId { get; private set; }

        string GetQtToolsPath()
        {
            var project = VsShell.GetProject(Context);
            if (project == null)
                return null;

            ThreadHelper.ThrowIfNotOnUIThread();

            var vcProject = project.Object as VCProject;
            if (vcProject == null)
                return null;

            var vcConfigs = vcProject.Configurations as IVCCollection;
            if (vcConfigs == null)
                return null;

            var activeConfig = project.ConfigurationManager?.ActiveConfiguration;
            if (activeConfig == null)
                return null;

            var activeConfigId = string.Format("{0}|{1}",
                activeConfig.ConfigurationName, activeConfig.PlatformName);
            var vcConfig = vcConfigs.Item(activeConfigId) as VCConfiguration;
            if (vcConfig == null)
                return null;

            var qtToolsPath = vcConfig.GetEvaluatedPropertyValue("QtToolsPath");
            if (string.IsNullOrEmpty(qtToolsPath))
                return null;

            return qtToolsPath;
        }

        string GetDefaultQtToolsPath()
        {
            var versionMgr = QtVersionManager.The();
            if (versionMgr == null)
                return null;

            var defaultVersion = versionMgr.GetDefaultVersion();
            var defaultVersionInfo = versionMgr.GetVersionInfo(defaultVersion);
            if (defaultVersionInfo == null || string.IsNullOrEmpty(defaultVersionInfo.qtDir))
                return null;

            return Path.Combine(defaultVersionInfo.qtDir, "bin");
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
            // Initialize to null
            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pguidCmdUI = Guid;
            pgrfCDW = 0;
            pbstrEditorCaption = null;

            // Validate inputs
            if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0) {
                return VSConstants.E_INVALIDARG;
            }
            if (punkDocDataExisting != IntPtr.Zero) {
                return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
            }

            Context = pvHier;
            ItemId = itemid;

            ThreadHelper.ThrowIfNotOnUIThread();

            var toolsPath = GetToolsPath();
            if (string.IsNullOrEmpty(toolsPath))
                return VSConstants.VS_E_INCOMPATIBLEDOCDATA;

            // Create the Document (editor)
            EditorPane newEditor = new EditorPane(this, toolsPath);
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
            if (VSConstants.LOGVIEWID_Primary == rguidLogicalView) {
                // primary view uses NULL as pbstrPhysicalView
                return VSConstants.S_OK;
            } else {
                // you must return E_NOTIMPL for any unrecognized rguidLogicalView values
                return VSConstants.E_NOTIMPL;
            }
        }

        protected virtual ProcessStartInfo GetStartInfo(
            string filePath,
            string qtToolsPath,
            bool hideWindow)
        {
            return new ProcessStartInfo
            {
                FileName = Path.GetFullPath(Path.Combine(qtToolsPath, ExecutableName)),
                Arguments = SafePath(filePath),
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
            var st = GetStartInfo(filePath, qtToolsPath, hideWindow);
            try {
                return Process.Start(st);
            } catch (Exception e) {
                Messages.Print("\r\n" + e.Message);
                if (!File.Exists(st.Arguments))
                    Messages.Print("The system cannot find the file: " + st.Arguments);
                if (!File.Exists(st.FileName))
                    Messages.Print("The system cannot find the file: " + st.FileName);
                Messages.Print("\r\nStacktrace:\r\n" + e.StackTrace);
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

        private class EditorPane : WindowPane, IVsPersistDocData
        {
            public Editor Editor { get; private set; }
            public string QtToolsPath { get; private set; }

            public TableLayoutPanel EditorContainer { get; private set; }
            public Label EditorTitle { get; private set; }
            public LinkLabel EditorDetachButton { get; private set; }
            public Panel EditorControl { get; private set; }
            public override IWin32Window Window => EditorContainer;

            public Process EditorProcess { get; private set; }
            public IntPtr EditorWindow { get; private set; }
            public int EditorWindowStyle { get; private set; }
            public int EditorWindowStyleExt { get; private set; }
            public IntPtr EditorIcon { get; private set; }

            public EditorPane(Editor editor, string qtToolsPath)
            {
                Editor = editor;
                QtToolsPath = qtToolsPath;

                var titleBar = new FlowLayoutPanel
                {
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(201, 221, 201),
                };
                titleBar.Controls.Add(EditorTitle = new Label
                {
                    Text = Editor.ExecutableName,
                    ForeColor = Color.FromArgb(9, 16, 43),
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point),
                    AutoSize = true,
                    Margin = new Padding(8),
                });
                titleBar.Controls.Add(EditorDetachButton = new LinkLabel
                {
                    Text = "Detach",
                    Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point),
                    AutoSize = true,
                    Margin = new Padding(0, 8, 8, 8),
                });
                EditorDetachButton.Click += EditorDetachButton_Click;

                EditorControl = new Panel
                {
                    BackColor = SystemColors.Window,
                    Dock = DockStyle.Fill
                };
                EditorControl.Resize += EditorControl_Resize;

                EditorContainer = new TableLayoutPanel
                {
                    ColumnCount = 1,
                    RowCount = 2,
                };
                EditorContainer.ColumnStyles.Add(new ColumnStyle());
                EditorContainer.RowStyles.Add(new RowStyle());
                EditorContainer.RowStyles.Add(new RowStyle());
                EditorContainer.Controls.Add(titleBar, 0, 0);
                EditorContainer.Controls.Add(EditorControl, 0, 1);
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

                ThreadHelper.ThrowIfNotOnUIThread();

                if (Editor.Detached) {
                    Editor.OnStart(EditorProcess);
                    CloseParentFrame();
                    return VSConstants.S_OK;
                }

                EditorTitle.Text = Editor.GetTitle(EditorProcess);

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
                            lpfn: (hWnd, lParam) =>
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
                ThreadHelper.ThrowIfNotOnUIThread();

                EditorProcess = null;
                EditorWindow = IntPtr.Zero;
                var parentFrame = GetService(typeof(SVsWindowFrame)) as IVsWindowFrame;
                parentFrame?.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
            }

            private void EditorProcess_Exited(object sender, EventArgs e)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

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

            private void EditorDetachButton_Click(object sender, EventArgs e)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

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
