/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.ComponentModel.Design;
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
                var command = new OleMenuCommand(execHandler,
                    new CommandID(QtMenus.Package.Guid, id));
                command.BeforeQueryStatus += beforeQueryStatus;
                commandService.AddCommand(command);
            }
        }

        private void execHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command)
                return;

            switch (command.CommandID.ID) {
            case QtMenus.Package.ViewQtHelp:
                VsShellUtilities.OpenSystemBrowser("https://www.qt.io/developers");
                break;
            case QtMenus.Package.ViewGettingStarted:
                VsShellUtilities.OpenSystemBrowser("https://doc.qt.io/qtvstools/index.html");
                break;
            case QtMenus.Package.LaunchDesigner:
                QtVsToolsPackage.Instance.QtDesigner.Start(hideWindow: false);
                break;
            case QtMenus.Package.LaunchLinguist:
                QtVsToolsPackage.Instance.QtLinguist.Start(hideWindow: false);
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

        private void beforeQueryStatus(object sender, EventArgs e)
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
                    = HelperFunctions.GetSelectedQtProject(dte) is {};
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
    }
}
