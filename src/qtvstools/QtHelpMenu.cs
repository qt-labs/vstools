/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using QtProjectLib;
using QtVsTools.VisualStudio;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace QtVsTools
{
    internal sealed class QtHelpMenu
    {
        public static QtHelpMenu Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new QtHelpMenu(package);
        }

        const int F1QtHelpId = 0x0100;
        const int ViewQtHelpId = 0x0101;
        const int OnlineDocumentationId = 0x0102;
        const int OfflineDocumentationId = 0x0103;

        readonly Package package;
        static readonly Guid HelpMenuGroupGuid = new Guid("fc6244f9-ec84-4370-a59c-b009b2eafd1b");

        QtHelpMenu(Package pkg)
        {
            if (pkg == null)
                throw new ArgumentNullException("package");
            package = pkg;

            var commandService = VsServiceProvider
                .GetService<IMenuCommandService, OleMenuCommandService>();
            if (commandService == null)
                return;

            var menuCommandID = new CommandID(HelpMenuGroupGuid, F1QtHelpId);
            commandService.AddCommand(new MenuCommand(F1QtHelpCallback, menuCommandID));

            menuCommandID = new CommandID(HelpMenuGroupGuid, ViewQtHelpId);
            commandService.AddCommand(new MenuCommand(ViewQtHelpCallback, menuCommandID));

            var command = new OleMenuCommand(ExecHandler, new CommandID(HelpMenuGroupGuid,
                OfflineDocumentationId));
            command.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(command);

            command = new OleMenuCommand(ExecHandler, new CommandID(HelpMenuGroupGuid,
                OnlineDocumentationId));
            command.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(command);
        }

        IServiceProvider ServiceProvider
        {
            get { return package; }
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

        void ViewQtHelpCallback(object sender, EventArgs args)
        {
            VsShellUtilities.OpenSystemBrowser("https://www.qt.io/developers");
        }

        async void F1QtHelpCallback(object sender, EventArgs args)
        {
            try {
                var dte = VsServiceProvider.GetService<SDTE, DTE>();
                var objTextDocument = dte.ActiveDocument.Object() as TextDocument;

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
                    return; // suppress single character, operators etc...

                var qtVersion = "$(DefaultQtVersion)";
                var project = HelperFunctions.GetSelectedQtProject(dte);
                if (project == null) {
                    project = HelperFunctions.GetSelectedProject(dte);
                    if (project != null && HelperFunctions.IsQMakeProject(project)) {
                        var qmakeQtDir = HelperFunctions.GetQtDirFromQMakeProject(project);
                        qtVersion = QtVersionManager.The().GetQtVersionFromInstallDir(qmakeQtDir);
                    }
                } else {
                    qtVersion = QtVersionManager.The().GetProjectQtVersion(project);
                }

                var docPath = QtVersionManager.The().GetVersionInfo(qtVersion).QtInstallDocs;
                if (string.IsNullOrEmpty(docPath) || !Directory.Exists(docPath))
                    return;

                var qchFiles = Directory.GetFiles(docPath, "*?.qch");
                if (qchFiles.Length == 0)
                    return;

                var settingsManager = VsShellSettings.Manager;
                var store = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
                var offline =
                    store.GetBoolean(Statics.HelpPreferencePath, Statics.HelpPreferenceKey, true);

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
                            using (var reader = await command.ExecuteReaderAsync()) {
                                while (reader.Read()) {
                                    var title = GetString(reader, 0);
                                    if (string.IsNullOrWhiteSpace(title))
                                        title = keyword + ':' + GetString(reader, 3);
                                    var path = string.Empty;
                                    if (offline) {
                                        path = "file:///" + Path.Combine(docPath,
                                            GetString(reader, 2), GetString(reader, 3));
                                    } else {
                                        path = "https://" + Path.Combine("doc.qt.io", "qt-5",
                                            GetString(reader, 3));
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
                    if (!offline) {
                        uri = new UriBuilder("https://doc.qt.io/qt-5/search-results.html")
                        {
                            Query = "q=" + keyword
                        }.ToString();
                    }
                    break;
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
                        return;
                    uri = dialog.Link;
                    break;
                }

                if (string.IsNullOrEmpty(uri)) { // offline mode without a single search hit
                    VsShellUtilities.ShowMessageBox(Instance.ServiceProvider,
                        "Your search - " + keyword + " - did not match any documents.",
                        string.Empty, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                } else {
                    if (uri.StartsWith("file:///", StringComparison.Ordinal)
                        && !File.Exists(uri.Substring("file:///".Length))) {
                        VsShellUtilities.ShowMessageBox(Instance.ServiceProvider,
                            "Your search - " + keyword + " - did match a document, but it could "
                            + "not be found on disk. To use the online help, select: "
                            + "Help | Set Qt Help Preference | Use Online Documentation",
                            string.Empty, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    } else {
                        VsShellUtilities.OpenSystemBrowser(HelperFunctions.ChangePathFormat(uri));
                    }
                }
            } catch { }
        }

        void ExecHandler(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            var settingsManager = VsShellSettings.Manager;
            var store = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            store.CreateCollection(Statics.HelpPreferencePath);

            var value = command.CommandID.ID == OfflineDocumentationId;
            store.SetBoolean(Statics.HelpPreferencePath, Statics.HelpPreferenceKey, value);
        }

        void BeforeQueryStatus(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            var settingsManager = VsShellSettings.Manager;
            var store = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            switch (command.CommandID.ID) {
            case OnlineDocumentationId:
                command.Checked = !store.GetBoolean(Statics.HelpPreferencePath,
                    Statics.HelpPreferenceKey, false);
                break;
            case OfflineDocumentationId:
                command.Checked = store.GetBoolean(Statics.HelpPreferencePath,
                    Statics.HelpPreferenceKey, true);
                break;
            }
        }
    }
}
