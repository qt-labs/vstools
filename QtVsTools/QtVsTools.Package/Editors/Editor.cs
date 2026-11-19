// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Package.Editors
{
    using Core;
    using Core.CMake;
    using Core.MsBuild;
    using Core.Options;
    using QtVsTools.Core.Common;
    using VisualStudio;

    public abstract partial class Editor : IVsEditorFactory
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
                if (CMakeProject.ActiveProject is { } cMakeProject) {
                    var path = cMakeProject["cmake", "CMAKE_PREFIX_PATH"];
                    return string.IsNullOrWhiteSpace(path) ? null : Path.Combine(path, "bin");
                }

                if (Context == null)
                    return null;
                if (MsBuildProject.GetOrAdd(VsShell.GetProject(Context)) is not { } project)
                    return null;
                var qtToolsPath = project.GetPropertyValue("QtToolsPath");
                return string.IsNullOrWhiteSpace(qtToolsPath) ? null : qtToolsPath;
            });
        }

        protected static string GetDefaultQtToolsPath()
        {
            var defaultVersion = QtVersionManager.GetDefaultVersion();
            var defaultVersionInfo = VersionInformation.GetOrAddByName(defaultVersion);
            return string.IsNullOrEmpty(defaultVersionInfo?.QtDir)
                ? null
                : Path.Combine(defaultVersionInfo.QtDir, "bin");
        }

        [EnvironmentPermission(SecurityAction.Demand, Unrestricted = true)]
        public virtual int CreateEditorInstance(uint grfCreateDoc,
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
            pbstrPhysicalView = null; // initialize out parameter

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

        public virtual Process Start(string filePath = "",
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
        { }

        protected virtual void OnExit(Process process)
        { }

        protected virtual bool Detached => false;
        protected virtual bool ShowDetachNotification => true;

        protected abstract void DisableDetachNotification();
    }
}
