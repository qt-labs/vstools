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
    using QtMsBuild;
    using VisualStudio;

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtProjectContextMenu
    {
        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        private static QtProjectContextMenu Instance
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        public static void Initialize()
        {
            Instance = new QtProjectContextMenu();
        }

        /// <summary>
        /// Command ID.
        /// TODO: Remove, take form QtMenus.Package
        /// </summary>
        private enum CommandId
        {
            ImportPriFileProject = QtMenus.Package.ImportPriFileProject,
            lUpdateOnProject = QtMenus.Package.lUpdateOnProject,
            lReleaseOnProject = QtMenus.Package.lReleaseOnProject,
            ProjectConvertToQtMsBuild = QtMenus.Package.ProjectConvertToQtMsBuild,
            ProjectRefreshIntelliSense = QtMenus.Package.ProjectRefreshIntelliSense,
            QtProjectSettingsProject = QtMenus.Package.QtProjectSettingsProject
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QtMainMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        private QtProjectContextMenu()
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
            case QtMenus.Package.ImportPriFileProject:
                ExtLoader.ImportPriFile(HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte));
                break;
            case QtMenus.Package.lUpdateOnProject:
                Translation.RunlUpdate(HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte));
                break;
            case QtMenus.Package.lReleaseOnProject:
                Translation.RunlRelease(HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte));
                break;
            case QtMenus.Package.QtProjectSettingsProject:
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
            case QtMenus.Package.ProjectConvertToQtMsBuild: {
                    QtMsBuildConverter.ProjectToQtMsBuild(
                        HelperFunctions.GetSelectedProject(QtVsToolsPackage.Instance.Dte));
                }
                break;
            case QtMenus.Package.ProjectRefreshIntelliSense: {
                    var selectedProject = HelperFunctions.GetSelectedProject(QtVsToolsPackage.Instance.Dte);
                    var tracker = QtProjectTracker.Get(selectedProject, selectedProject.FullName);
                    QtProjectIntellisense.Refresh(tracker.Project);
                }
                break;
            }
        }

        private void beforeQueryStatus(object sender, EventArgs e)
        {
            if (sender is not OleMenuCommand command)
                return;

            var project = HelperFunctions.GetSelectedProject(QtVsToolsPackage.Instance.Dte);
            var isQtProject = HelperFunctions.IsVsToolsProject(project);
            var isQMakeProject = HelperFunctions.IsQtProject(project);
            if (!isQtProject && !isQMakeProject) {
                command.Enabled = command.Visible = false;
                return;
            }

            var isQtMsBuildEnabled = QtProject.IsQtMsBuildEnabled(project);

            var status = vsCommandStatus.vsCommandStatusSupported;
            switch (command.CommandID.ID) {
            case QtMenus.Package.ImportPriFileProject:
                command.Visible = true;
                command.Enabled = HelperFunctions.IsVsToolsProject(HelperFunctions
                    .GetSelectedProject(QtVsToolsPackage.Instance.Dte));
                break;
            case QtMenus.Package.lUpdateOnProject:
            case QtMenus.Package.lReleaseOnProject:
                command.Visible = true;
                command.Enabled = Translation.ToolsAvailable(project);
                break;
            case QtMenus.Package.QtProjectSettingsProject:
                if (isQtProject)
                    status |= vsCommandStatus.vsCommandStatusEnabled;
                else
                    status |= vsCommandStatus.vsCommandStatusInvisible;
                command.Enabled = (status & vsCommandStatus.vsCommandStatusEnabled) != 0;
                command.Visible = (status & vsCommandStatus.vsCommandStatusInvisible) == 0;
                break;
            case QtMenus.Package.ProjectConvertToQtMsBuild:
                command.Visible = true;
                command.Enabled = !isQtMsBuildEnabled;
                break;
            case QtMenus.Package.ProjectRefreshIntelliSense:
                command.Visible = command.Enabled = isQtMsBuildEnabled;
                break;
            }

            if (isQtProject) {
                var projectVersion = ProjectFormat.GetVersion(project);
                switch (command.CommandID.ID) {
                case QtMenus.Package.ProjectConvertToQtMsBuild:
                    if (projectVersion >= ProjectFormat.Version.Latest) {
                        command.Visible = command.Enabled = false;
                    } else {
                        command.Visible = command.Enabled = true;
                        if (isQtMsBuildEnabled)
                            command.Text = "Upgrade to latest Qt project format version";
                    }
                    break;
                }
            }
        }
    }
}
