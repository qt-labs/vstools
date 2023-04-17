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
        /// Command menu group (command set GUID).
        /// </summary>
        private static readonly Guid ProjectContextMenuGuid = new("5732faa9-6074-4e07-b035-2816e809f50e");

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
        /// </summary>
        private enum CommandId
        {
            ImportPriFileProjectId = 0x0114,
            lUpdateOnProjectId = 0x0118,
            lReleaseOnProjectId = 0x0119,
            ProjectConvertToQtMsBuild = 0x0130,
            ProjectRefreshIntelliSense = 0x0131,
            QtProjectSettingsProjectId = 0x0122
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

            foreach (var id in Enum.GetValues(typeof(CommandId))) {
                var command = new OleMenuCommand(execHandler,
                    new CommandID(ProjectContextMenuGuid, (int)id));
                command.BeforeQueryStatus += beforeQueryStatus;
                commandService.AddCommand(command);
            }
        }

        private void execHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command)
                return;

            switch ((CommandId)command.CommandID.ID) {
            case CommandId.ImportPriFileProjectId:
                ExtLoader.ImportPriFile(HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte));
                break;
            case CommandId.lUpdateOnProjectId:
                Translation.RunlUpdate(HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte));
                break;
            case CommandId.lReleaseOnProjectId:
                Translation.RunlRelease(HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte));
                break;
            case CommandId.QtProjectSettingsProjectId:
                var pro = HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte);
                if (QtProject.GetFormatVersion(pro) >= Resources.qtMinFormatVersion_Settings) {
                    QtVsToolsPackage.Instance.Dte.ExecuteCommand("Project.Properties");
                } else if (pro != null) {
                    if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                        Notifications.UpdateProjectFormat.Show();
                } else {
                    MessageBox.Show("No Project Opened");
                }
                break;
            case CommandId.ProjectConvertToQtMsBuild: {
                    QtMsBuildConverter.ProjectToQtMsBuild(
                        HelperFunctions.GetSelectedProject(QtVsToolsPackage.Instance.Dte));
                }
                break;
            case CommandId.ProjectRefreshIntelliSense: {
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
            switch ((CommandId)command.CommandID.ID) {
            case CommandId.ImportPriFileProjectId:
                command.Visible = true;
                command.Enabled = HelperFunctions.IsVsToolsProject(HelperFunctions
                    .GetSelectedProject(QtVsToolsPackage.Instance.Dte));
                break;
            case CommandId.lUpdateOnProjectId:
            case CommandId.lReleaseOnProjectId:
                command.Visible = true;
                command.Enabled = Translation.ToolsAvailable(project);
                break;
            case CommandId.QtProjectSettingsProjectId:
                if (isQtProject)
                    status |= vsCommandStatus.vsCommandStatusEnabled;
                else
                    status |= vsCommandStatus.vsCommandStatusInvisible;
                command.Enabled = (status & vsCommandStatus.vsCommandStatusEnabled) != 0;
                command.Visible = (status & vsCommandStatus.vsCommandStatusInvisible) == 0;
                break;
            case CommandId.ProjectConvertToQtMsBuild:
                command.Visible = true;
                command.Enabled = !isQtMsBuildEnabled;
                break;
            case CommandId.ProjectRefreshIntelliSense:
                command.Visible = command.Enabled = isQtMsBuildEnabled;
                break;
            }

            if (isQtProject) {
                int projectVersion = QtProject.GetFormatVersion(project);
                switch ((CommandId)command.CommandID.ID) {
                case CommandId.ProjectConvertToQtMsBuild:
                    if (projectVersion >= Resources.qtProjectFormatVersion) {
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
