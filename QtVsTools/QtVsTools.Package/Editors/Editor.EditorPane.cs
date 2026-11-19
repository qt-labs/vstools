// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Package.Editors
{
    using Core;

    public abstract partial class Editor
    {
        internal class EditorPane : WindowPane, IVsPersistDocData
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

                EditorProcess.EnableRaisingEvents = true;

                if (Editor.Detached) {
                    Editor.OnStart(EditorProcess);
                    CloseParentFrame();
                    return VSConstants.S_OK;
                }

                if (Editor.ShowDetachNotification)
                    ShowDetachBar();

                EditorProcess.WaitForInputIdle();
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

            int IVsPersistDocData.RenameDocData(uint grfAttribs,
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

            int IVsPersistDocData.SaveDocData(VSSAVEFLAGS dwSave,
                out string pbstrMkDocumentNew,
                out int pfSaveCanceled)
            {
                pbstrMkDocumentNew = string.Empty;
                pfSaveCanceled = 0;
                return VSConstants.E_NOTIMPL;
            }

            int IVsPersistDocData.OnRegisterDocData(uint docCookie,
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
