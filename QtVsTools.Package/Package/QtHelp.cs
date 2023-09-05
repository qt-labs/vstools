/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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
using System.ComponentModel.Design;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools
{
    using Core;
    using VisualStudio;

    public class QtHelp
    {
        public enum SourcePreference { Online, Offline }

        private static QtHelp Instance
        {
            get;
            set;
        }

        public static void Initialize()
        {
            Instance = new QtHelp();
        }

        const int F1QtHelpId = 0x0502;

        private static readonly Guid MainMenuGuid = new Guid("58f83fff-d39d-4c66-810b-2702e1f04e73");

        private QtHelp()
        {
            var commandService = VsServiceProvider
                .GetService<IMenuCommandService, OleMenuCommandService>();
            if (commandService == null)
                return;

            var menuCommandID = new CommandID(MainMenuGuid, F1QtHelpId);
            commandService.AddCommand(new MenuCommand(F1QtHelpEventHandler, menuCommandID));
        }
        static bool IsSuperfluousCharacter(string text)
        {
            switch (text) {
            case " ":
            case ";":
            case ".":
            case "<":
            case ">":
            case "{":
            case "}":
            case "(":
            case ")":
            case ":":
            case ",":
            case "/":
            case "\\":
            case "^":
            case "%":
            case "+":
            case "-":
            case "*":
            case "\t":
            case "&":
            case "\"":
            case "!":
            case "[":
            case "]":
            case "|":
            case "'":
            case "~":
            case "#":
            case "=":
                return true; // nothing we are interested in
            }
            return false;
        }

        static string GetString(DbDataReader reader, int index)
        {
            if (!reader.IsDBNull(index))
                return reader.GetString(index);
            return string.Empty;
        }

        void F1QtHelpEventHandler(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!ShowEditorContextHelp()) {
                Messages.Print("No help match was found. You can still try to search online at "
                    + "https://doc.qt.io" + ".", false, true);
            }
        }

        public static bool ShowEditorContextHelp()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try {
                var dte = VsServiceProvider.GetService<SDTE, DTE>();
                var objTextDocument = dte?.ActiveDocument?.Object() as TextDocument;
                if (objTextDocument == null)
                    return false;

                var keyword = string.Empty;
                var selection = objTextDocument.Selection;
                if (selection.IsEmpty) { // no selection inside the document
                    var line = selection.ActivePoint.Line; // current line
                    var offset = selection.ActivePoint.LineCharOffset; // current char offset

                    selection.CharLeft(true); // try the character before the cursor
                    if (!selection.IsEmpty) {
                        keyword = selection.Text; // something in front of the cursor
                        selection.CharRight(true); // reset to origin
                        if (!IsSuperfluousCharacter(keyword)) {
                            // move the selection to the start of the word
                            selection.WordLeft(true);
                            selection.MoveToPoint(selection.TopPoint);
                        }
                    }
                    selection.WordRight(true);  // select the word
                    keyword = selection.Text;  // get the selected text
                    selection.MoveToLineAndOffset(line, offset); // reset
                } else {
                    keyword = selection.Text;
                }

                keyword = keyword.Trim();
                if (keyword.Length <= 1 || IsSuperfluousCharacter(keyword))
                    return false; // suppress single character, operators etc...

                var qtVersion = "$(DefaultQtVersion)";
                var project = HelperFunctions.GetSelectedQtProject(dte);
                if (project == null) {
                    project = HelperFunctions.GetSelectedProject(dte);
                    if (project != null && HelperFunctions.IsQtProject(project)) {
                        var qmakeQtDir = HelperFunctions.GetQtDirFromQMakeProject(project);
                        qtVersion = QtVersionManager.The().GetQtVersionFromInstallDir(qmakeQtDir);
                    }
                } else {
                    qtVersion = QtVersionManager.The().GetProjectQtVersion(project);
                }

                var info = QtVersionManager.The().GetVersionInfo(qtVersion);
                var docPath = info?.QtInstallDocs;
                if (string.IsNullOrEmpty(docPath) || !Directory.Exists(docPath))
                    return false;

                var qchFiles = Directory.GetFiles(docPath, "*?.qch");
                if (qchFiles.Length == 0)
                    return TryShowGenericSearchResultsOnline(keyword, info.qtMajor);

                var offline = QtVsToolsLegacyPackage.Instance.Options.HelpPreference == SourcePreference.Offline;

                var linksForKeyword = string.Format("SELECT d.Title, f.Name, e.Name, "
                    + "d.Name, a.Anchor FROM IndexTable a, FileNameTable d, FolderTable e, "
                    + "NamespaceTable f WHERE a.FileId=d.FileId AND d.FolderId=e.Id AND "
                    + "a.NamespaceId=f.Id AND a.Name='{0}'", keyword);

                var links = new Dictionary<string, string>();
                var builder = new SQLiteConnectionStringBuilder
                {
                    ReadOnly = true
                };
                foreach (var file in qchFiles) {
                    builder.DataSource = file;
                    using (var connection = new SQLiteConnection(builder.ToString())) {
                        connection.Open();
                        using (var command = new SQLiteCommand(linksForKeyword, connection)) {
                            var reader = QtVsToolsLegacyPackage.Instance.JoinableTaskFactory
                                .Run(async () => await command.ExecuteReaderAsync());
                            using (reader) {
                                while (reader.Read()) {
                                    var title = GetString(reader, 0);
                                    if (string.IsNullOrWhiteSpace(title))
                                        title = keyword + ':' + GetString(reader, 3);
                                    var path = string.Empty;
                                    if (offline) {
                                        path = "file:///" + Path.Combine(docPath,
                                            GetString(reader, 2), GetString(reader, 3));
                                    } else {
                                        path = "https://" + Path.Combine("doc.qt.io",
                                            $"qt-{info.qtMajor}", GetString(reader, 3));
                                    }
                                    if (!string.IsNullOrWhiteSpace(GetString(reader, 4)))
                                        path += "#" + GetString(reader, 4);
                                    links.Add(title, path);
                                }
                            }
                        }
                    }
                }

                var uri = string.Empty;
                switch (links.Values.Count) {
                case 0:
                    return TryShowGenericSearchResultsOnline(keyword, info.qtMajor);
                case 1:
                    uri = links.First().Value;
                    break;
                default:
                    var dialog = new QtHelpLinkChooser
                    {
                        Links = links,
                        Keyword = keyword,
                        ShowInTaskbar = false
                    };
                    if (!dialog.ShowModal().GetValueOrDefault())
                        return false;
                    uri = dialog.Link;
                    break;
                }

                uri = HelperFunctions.FromNativeSeparators(uri);
                var helpUri = new Uri(uri);
                if (helpUri.IsFile && !File.Exists(helpUri.LocalPath)) {
                    VsShellUtilities.ShowMessageBox(QtVsToolsLegacyPackage.Instance,
                        "Your search - " + keyword + " - did match a document, but it could "
                        + "not be found on disk. To use the online help, select: "
                        + "Tools | Options | Qt | Preferred source | Online",
                        string.Empty, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                } else {
                    VsShellUtilities.OpenSystemBrowser(uri);
                }
            } catch (Exception exception) {
                exception.Log();
            }
            return true;
        }

        private static bool TryShowGenericSearchResultsOnline(string keyword, uint version)
        {
            if (QtVsToolsLegacyPackage.Instance.Options.HelpPreference != SourcePreference.Online)
                return false;

            VsShellUtilities.OpenSystemBrowser(HelperFunctions.FromNativeSeparators(
                new UriBuilder($"https://doc.qt.io/qt-{version}/search-results.html")
                {
                    Query = "q=" + keyword
                }.ToString())
            );
            return true;
        }
    }
}
