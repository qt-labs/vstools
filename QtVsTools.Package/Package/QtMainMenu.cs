/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace QtVsTools
{
    using Core;
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
                VsShellUtilities.OpenSystemBrowser("https://doc.qt.io/qtvstools/qtvstools-getting-started.html");
                break;
            case QtMenus.Package.LaunchDesigner:
                QtVsToolsPackage.Instance.QtDesigner.Start(hideWindow: false);
                break;
            case QtMenus.Package.LaunchLinguist:
                QtVsToolsPackage.Instance.QtLinguist.Start(hideWindow: false);
                break;
            case QtMenus.Package.OpenProFile:
                ExtLoader.ImportProFile();
                break;
            case QtMenus.Package.ImportPriFile:
                ExtLoader.ImportPriFile(HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte));
                break;
            case QtMenus.Package.ConvertToQtMsBuild:
                QtMsBuildConverter.SolutionToQtMsBuild();
                break;
            case QtMenus.Package.QtProjectSettings:
                var pro = HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte);
                if (ProjectFormat.GetVersion(pro) >= ProjectFormat.Version.V3) {
                    QtVsToolsPackage.Instance.Dte.ExecuteCommand("Project.Properties");
                } else if (pro != null) {
                    if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                        Notifications.UpdateProjectFormat.Show();
                } else {
                    MessageBox.Show("No Project Opened");
                }
                break;
            case QtMenus.Package.QtOptions:
                QtVsToolsPackage.Instance.ShowOptionPage(typeof(Options.QtOptionsPage));
                break;
            case QtMenus.Package.QtVersions:
                QtVsToolsPackage.Instance.ShowOptionPage(typeof(Options.QtVersionsPage));
                break;
            }
        }

        private void beforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command)
                return;

            var project = HelperFunctions.GetSelectedProject(QtVsToolsPackage.Instance.Dte);

            switch (command.CommandID.ID) {
            case QtMenus.Package.ViewQtHelp:
            case QtMenus.Package.ViewGettingStarted:
                command.Visible = command.Enabled = true;
                break;
            case QtMenus.Package.QtVersion:
                command.Text = "Qt Visual Studio Tools version " + Version.USER_VERSION;
                command.Visible = true;
                command.Enabled = false;
                break;
            case QtMenus.Package.LaunchDesigner:
            case QtMenus.Package.LaunchLinguist:
            case QtMenus.Package.OpenProFile:
            case QtMenus.Package.QtOptions:
            case QtMenus.Package.QtVersions:
                command.Visible = true;
                command.Enabled = true;
                break;
            case QtMenus.Package.ImportPriFile:
                command.Visible = true;
                command.Enabled = HelperFunctions.IsVsToolsProject(project);
                break;
            case QtMenus.Package.QtProjectSettings: {
                    var status = vsCommandStatus.vsCommandStatusSupported;
                    if (project != null) {
                        if (HelperFunctions.IsVsToolsProject(project))
                            status |= vsCommandStatus.vsCommandStatusEnabled;
                        else if (HelperFunctions.IsQtProject(project))
                            status |= vsCommandStatus.vsCommandStatusInvisible;
                    }
                    command.Enabled = (status & vsCommandStatus.vsCommandStatusEnabled) != 0;
                    command.Visible = (status & vsCommandStatus.vsCommandStatusInvisible) == 0;
                }
                break;
            case QtMenus.Package.ConvertToQtMsBuild: {
                    command.Visible = true;
                    command.Enabled = QtVsToolsPackage.Instance.Dte.Solution is { Projects: { Count: > 0 } };
                }
                break;
            }
        }
    }
}
