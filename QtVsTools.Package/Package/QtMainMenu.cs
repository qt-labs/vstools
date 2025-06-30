// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.Shell;

namespace QtVsTools
{
    using Core;
    using Core.MsBuild;
    using QtVsTools.Core.Common;
    using VisualStudio;

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtMainMenu
    {
        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        private static QtMainMenu Instance
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        public static void Initialize()
        {
            Instance = new QtMainMenu();
        }

        /// <summary>
        /// Command ID.
        /// TODO: Remove, take form QtMenus.Package
        /// </summary>
        private enum CommandId
        {
            QtVersion = QtMenus.Package.QtVersion,
            ViewQtHelp = QtMenus.Package.ViewQtHelp,
            ViewGettingStarted = QtMenus.Package.ViewGettingStarted,
            LaunchDesigner = QtMenus.Package.LaunchDesigner,
            LaunchLinguist = QtMenus.Package.LaunchLinguist,
            OpenProFile = QtMenus.Package.OpenProFile,
            ImportPriFile = QtMenus.Package.ImportPriFile,
            ConvertToQtMsBuild = QtMenus.Package.ConvertToQtMsBuild,
            QtProjectSettings = QtMenus.Package.QtProjectSettings,
            QtOptions = QtMenus.Package.QtOptions,
            QtVersions = QtMenus.Package.QtVersions
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QtMainMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private QtMainMenu()
        {
            var commandService = VsServiceProvider
                .GetService<IMenuCommandService, OleMenuCommandService>();
            if (commandService == null)
                return;

            foreach (int id in Enum.GetValues(typeof(CommandId))) {
                var command = new OleMenuCommand(ExecHandler,
                    new CommandID(QtMenus.Package.Guid, id));
                command.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(command);
            }
        }

        private static void ExecHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command)
                return;

            var properties = new Dictionary<string, string>
            {
                {"Command", Enum.GetName(typeof(CommandId), command.CommandID.ID)}
            };
            Telemetry.TrackEvent(typeof(QtMainMenu) + ".ExecHandler", properties);

            switch (command.CommandID.ID) {
            case QtMenus.Package.ViewQtHelp:
                VsShellUtilities.OpenSystemBrowser("https://www.qt.io/developers");
                break;
            case QtMenus.Package.ViewGettingStarted:
                VsShellUtilities.OpenSystemBrowser("https://doc.qt.io/qtvstools/index.html");
                break;
            case QtMenus.Package.LaunchDesigner:
                try {
                    const string uiContent = "<ui version=\"4.0\">\n"
                        + " <class>Form</class>\n"
                        + " <widget class=\"QWidget\" name=\"Form\">\n"
                        + "  <property name=\"objectName\">\n"
                        + "   <string notr=\"true\">Form</string>\n"
                        + "  </property>\n"
                        + "  <property name=\"geometry\">\n"
                        + "   <rect>\n"
                        + "    <x>0</x>\n"
                        + "    <y>0</y>\n"
                        + "    <width>400</width>\n"
                        + "    <height>300</height>\n"
                        + "   </rect>\n"
                        + "  </property>\n"
                        + "  <property name=\"windowTitle\">\n"
                        + "   <string>Form</string>\n"
                        + "  </property>\n"
                        + " </widget>\n"
                        + "</ui>\n";
                    VsEditor.Open(path: CreateTempFile("form", ".ui", uiContent));
                } catch (Exception exception) {
                    exception.Log();
                    QtVsToolsPackage.Instance.QtDesigner.Start(hideWindow: false);
                }
                break;
            case QtMenus.Package.LaunchLinguist:
                try {
                    const string tsContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                        + "<!DOCTYPE TS>\n"
                        + "<TS version=\"2.1\" language=\"en\">\n"
                        + "<context>\n"
                        + " <name>Empty</name>\n"
                        + " <message>\n"
                        + "  <source>Empty</source>\n"
                        + "  <translation type=\"unfinished\"></translation>\n"
                        + " </message>\n"
                        + "</context>\n"
                        + "</TS>\n";
                    VsEditor.Open(path: CreateTempFile("lang", ".ts", tsContent));
                } catch (Exception exception) {
                    exception.Log();
                    QtVsToolsPackage.Instance.QtLinguist.Start(hideWindow: false);
                }
                break;
            case QtMenus.Package.OpenProFile:
                ProjectImporter.ImportProFile(QtVsToolsPackage.Instance.Dte);
                break;
            case QtMenus.Package.ImportPriFile:
                ProjectImporter.ImportPriFile(QtVsToolsPackage.Instance.Dte,
                    Utils.PackageInstallPath);
                break;
            case QtMenus.Package.ConvertToQtMsBuild:
                MsBuildProjectConverter.SolutionToQtMsBuild();
                break;
            case QtMenus.Package.QtProjectSettings:
                QtVsToolsPackage.Instance.Dte.ExecuteCommand("Project.Properties");
                break;
            case QtMenus.Package.QtOptions:
                QtVsToolsPackage.Instance.ShowOptionPage(typeof(Core.Options.QtOptionsPage));
                break;
            case QtMenus.Package.QtVersions:
                QtVsToolsPackage.Instance.ShowOptionPage(typeof(Core.Options.QtVersionsPage));
                break;
            }
        }

        private static void BeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command)
                return;

            var dte = QtVsToolsPackage.Instance.Dte;

            switch (command.CommandID.ID) {
            case QtMenus.Package.ViewQtHelp:
            case QtMenus.Package.ViewGettingStarted:
            case QtMenus.Package.LaunchDesigner:
            case QtMenus.Package.LaunchLinguist:
            case QtMenus.Package.OpenProFile:
            case QtMenus.Package.QtOptions:
            case QtMenus.Package.QtVersions:
                command.Visible = command.Enabled = true;
                break;
            case QtMenus.Package.QtVersion:
                command.Text = "Qt Visual Studio Tools version " + Version.USER_VERSION;
                command.Visible = true;
                command.Enabled = false;
                break;
            case QtMenus.Package.ImportPriFile:
            case QtMenus.Package.QtProjectSettings:
                command.Visible = command.Enabled
                    = HelperFunctions.GetSelectedQtProject(dte) is { };
                break;
            case QtMenus.Package.ConvertToQtMsBuild:
                command.Visible = command.Enabled = false;
                foreach (var project in HelperFunctions.ProjectsInSolution(dte)) {
                    switch (MsBuildProjectFormat.GetVersion(project)) {
                    case MsBuildProjectFormat.Version.V1:
                    case MsBuildProjectFormat.Version.V2:
                        command.Visible = command.Enabled = true;
                        return;
                    case >= MsBuildProjectFormat.Version.V3 and < MsBuildProjectFormat.Version.Latest:
                        command.Visible = command.Enabled = true;
                        command.Text = "Upgrade projects to latest Qt project format version";
                        return;
                    }
                }
                break;
            }
        }

        private static string CreateTempFile(string prefix, string extension, string fileContent)
        {
            var tempFolderPath = Path.GetTempPath();

            for (var i = 1; i <= 9999; i++) {
                var tmpPath = Path.Combine(tempFolderPath, $"{prefix}-{i:D4}{extension}");
                try {
                    // Use FileMode.CreateNew to ensure uniqueness.
                    using var fs = new FileStream(tmpPath, FileMode.CreateNew, FileAccess.Write);
                    using var writer = new StreamWriter(fs, Encoding.UTF8);
                    writer.Write(fileContent);
                    return tmpPath;
                } catch (IOException) {
                    // The file already exists, so try the next candidate.
                }
            }
            throw new Exception("No available file name could be generated within the range 1-9999.");
        }
    }
}
