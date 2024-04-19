/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace QtVsTools.Package.MsBuild
{
    using Core.MsBuild;
    using QtVsTools.Core.Common;
    using VisualStudio;

    [Guid(GuidString)]
    public partial class ConversionReportViewer : IVsEditorFactory
    {
        private const string GuidString = "57F4A78F-2C57-4903-8A8E-A48FF5D0B2A8";
        public static Guid Guid { get; } = new(GuidString);

        private ConversionReportViewerWindow ViewerWindow { get; set; }

        private void CreateEditorInstance()
        {
            ViewerWindow = new ConversionReportViewerWindow();
        }

        public int Close()
        {
            return VSConstants.S_OK;
        }
    }

    public partial class ConversionReportViewerWindow : WindowPane, IVsPersistDocData
    {
        private RichTextBox TextBox { get; } = new();
        private JObject Metadata { get; set; }
        private Dictionary<string, string> TempBefore { get; } = new();
        private Dictionary<string, string> TempAfter { get; } = new();

        public ConversionReportViewerWindow()
        {
            TextBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            TextBox.IsReadOnly = true;
            TextBox.IsReadOnlyCaretVisible = false;
            Content = TextBox;
        }

        public int LoadDocData(string path)
        {
            if (ConversionReport.Load(path) is not { } report)
                return VSConstants.E_FAIL;
            if (report.Document is not { } document)
                return VSConstants.E_FAIL;
            if (document.Tag is not string { Length: > 0 } metadata)
                return VSConstants.E_FAIL;
            TextBox.Document = document;
            try {
                Metadata = JObject.Parse(metadata);
                if (Metadata["files"] is not JObject files)
                    return VSConstants.E_FAIL;
                foreach (var file in files.Properties()) {
                    File.WriteAllText(TempBefore[file.Name]
                        = $@"{Path.GetTempPath()}\{Path.GetRandomFileName()}.xml",
                        Utils.FromZipBase64(file.Value["before"].Value<string>()));
                    File.WriteAllText(TempAfter[file.Name]
                        = $@"{Path.GetTempPath()}\{Path.GetRandomFileName()}.xml",
                        Utils.FromZipBase64(file.Value["after"].Value<string>()));
                }
            } catch (Exception) {
                return VSConstants.E_FAIL;
            }

            foreach (var link in FindDocumentHyperlinks(TextBox.Document)) {
                var enableLink = !link.NavigateUri.Query.Contains("current")
                    || File.Exists(link.NavigateUri.LocalPath);
                link.Cursor = enableLink ? Cursors.Hand : Cursors.No;
                link.Foreground = new SolidColorBrush(enableLink ? Colors.Blue : Colors.Gray);
                if (enableLink)
                    link.MouseDown += Link_MouseDown;
            }

            return VSConstants.S_OK;
        }

        private string LinkPath(Hyperlink link, string moniker)
        {
            return moniker switch
            {
                "before" => TempBefore[link.NavigateUri.LocalPath],
                "after" => TempAfter[link.NavigateUri.LocalPath],
                _ => link.NavigateUri.LocalPath,
            };
        }

        private void Link_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not Hyperlink link)
                return;
            if (link.NavigateUri?.Query is not { Length: > 0 } query)
                return;
            var queryArgs = query.Split(new[] { '?', '&' }, StringSplitOptions.RemoveEmptyEntries);
            if (queryArgs.ElementAtOrDefault(0) is not { Length: > 0 } request)
                return;
            switch (request) {
            case "before":
            case "after":
                VsEditor.Open(LinkPath(link, request), VsEditor.OpenWith.CodeEditor);
                break;
            case "diff":
                if (queryArgs.ElementAtOrDefault(1) is not { Length: > 0 } leftMoniker)
                    break;
                if (queryArgs.ElementAtOrDefault(2) is not { Length: > 0 } rightMoniker)
                    break;
                if (LinkPath(link, leftMoniker) is not { Length: > 0 } leftPath)
                    break;
                if (LinkPath(link, rightMoniker) is not { Length: > 0 } rightPath)
                    break;
                if (!File.Exists(leftPath))
                    break;
                if (!File.Exists(rightPath))
                    break;
                VsEditor.Diff(leftPath, rightPath);
                break;
            }
        }

        public int Close()
        {
            foreach (var tempFile in TempBefore.Values.Union(TempAfter.Values).ToList())
                Utils.DeleteFile(tempFile);

            TempBefore.Clear();
            TempAfter.Clear();
            return VSConstants.S_OK;
        }

        private List<Hyperlink> FindDocumentHyperlinks(FlowDocument doc)
        {
            var nodes = new Stack<DependencyObject>();
            var links = new List<Hyperlink>();
            nodes.Push(doc);
            while (nodes.Any()) {
                var node = nodes.Pop();
                foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
                    nodes.Push(child);
                if (node is Hyperlink link)
                    links.Add(link);
            }
            return links;
        }
    }

    #region ### BOILERPLATE ########################################################################

    public partial class ConversionReportViewer : IVsEditorFactory
    {
        public int SetSite(IServiceProvider psp)
        {
            return VSConstants.S_OK;
        }

        [EnvironmentPermission(SecurityAction.Demand, Unrestricted = true)]
        public int CreateEditorInstance(uint grfCreateDoc, string pszMkDocument,
            string pszPhysicalView, IVsHierarchy pvHier, uint itemid, IntPtr punkDocDataExisting,
            out IntPtr ppunkDocView, out IntPtr ppunkDocData, out string pbstrEditorCaption,
            out Guid pguidCmdUI, out int pgrfCDW)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pguidCmdUI = Guid;
            pgrfCDW = 0;
            pbstrEditorCaption = null;

            if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0) {
                return VSConstants.E_INVALIDARG;
            }
            if (punkDocDataExisting != IntPtr.Zero) {
                return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
            }

            CreateEditorInstance();
            ppunkDocView = Marshal.GetIUnknownForObject(ViewerWindow);
            ppunkDocData = Marshal.GetIUnknownForObject(ViewerWindow);
            pbstrEditorCaption = "";
            pgrfCDW = (int)(_VSRDTFLAGS.RDT_CantSave | _VSRDTFLAGS.RDT_DontAutoOpen);

            return VSConstants.S_OK;
        }

        public int MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
        {
            pbstrPhysicalView = null;
            if (VSConstants.LOGVIEWID_Primary == rguidLogicalView)
                return VSConstants.S_OK;
            return VSConstants.E_NOTIMPL;
        }
    }

    public partial class ConversionReportViewerWindow : WindowPane, IVsPersistDocData
    {
        public int GetGuidEditorType(out Guid pClassID)
        {
            pClassID = ConversionReportViewer.Guid;
            return VSConstants.S_OK;
        }

        public int IsDocDataDirty(out int pfDirty)
        {
            pfDirty = 0;
            return VSConstants.S_OK;
        }

        public int SetUntitledDocPath(string pszDocDataPath)
        {
            return VSConstants.S_OK;
        }

        public int SaveDocData(VSSAVEFLAGS dwSave, out string pbstrMkDocumentNew,
            out int pfSaveCanceled)
        {
            pbstrMkDocumentNew = string.Empty;
            pfSaveCanceled = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int OnRegisterDocData(uint docCookie, IVsHierarchy pHierNew, uint itemidNew)
        {
            return VSConstants.S_OK;
        }

        public int RenameDocData(uint grfAttribs, IVsHierarchy pHierNew, uint itemidNew,
            string pszMkDocumentNew)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int IsDocDataReloadable(out int pfReloadable)
        {
            pfReloadable = 0;
            return VSConstants.S_OK;
        }

        public int ReloadDocData(uint grfFlags)
        {
            return VSConstants.E_NOTIMPL;
        }
    }
    #endregion ### BOILERPLATE #####################################################################
}
