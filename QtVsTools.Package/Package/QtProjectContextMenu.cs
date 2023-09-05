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
        private static readonly Guid ProjectContextMenuGuid = new Guid("5732faa9-6074-4e07-b035-2816e809f50e");

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
            ExportPriFileProjectId = 0x0115,
            ExportProFileProjectId = 0x0116,
            lUpdateOnProjectId = 0x0118,
            lReleaseOnProjectId = 0x0119,
            ProjectConvertToQtMsBuild = 0x0130,
            ProjectRefreshIntelliSense = 0x0131,
            QtProjectSettingsProjectId = 0x0122,
            ChangeProjectQtVersionProjectId = 0x0123
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

            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            switch ((CommandId)command.CommandID.ID) {
            case CommandId.ImportPriFileProjectId:
                ExtLoader.ImportPriFile(HelperFunctions.GetSelectedQtProject(QtVsToolsLegacyPackage.Instance.Dte));
                break;
            case CommandId.ExportPriFileProjectId:
                ExtLoader.ExportPriFile();
                break;
            case CommandId.ExportProFileProjectId:
                ExtLoader.ExportProFile();
                break;
            case CommandId.lUpdateOnProjectId:
                Translation.RunlUpdate(HelperFunctions.GetSelectedQtProject(QtVsToolsLegacyPackage.Instance.Dte));
                break;
            case CommandId.lReleaseOnProjectId:
                Translation.RunlRelease(HelperFunctions.GetSelectedQtProject(QtVsToolsLegacyPackage.Instance.Dte));
                break;
            case CommandId.QtProjectSettingsProjectId: {
                    var pro = HelperFunctions.GetSelectedQtProject(QtVsToolsLegacyPackage.Instance.Dte);
                    int projectVersion = QtProject.GetFormatVersion(pro);
                    if (projectVersion >= Resources.qtMinFormatVersion_Settings) {
                        QtVsToolsLegacyPackage.Instance.Dte.ExecuteCommand("Project.Properties");
                    } else if (pro != null) {
                        Legacy.QtMenu.ShowFormProjectQtSettings(pro);
                    } else {
                        MessageBox.Show(SR.GetString("NoProjectOpened"));
                    }
                }
                break;
            case CommandId.ChangeProjectQtVersionProjectId:
                Legacy.QtMenu.ShowFormChangeProjectQtVersion();
                break;
            case CommandId.ProjectConvertToQtMsBuild: {
                    QtMsBuildConverter.ProjectToQtMsBuild(
                        HelperFunctions.GetSelectedProject(QtVsToolsLegacyPackage.Instance.Dte));
                }
                break;
            case CommandId.ProjectRefreshIntelliSense: {
                    var selectedProject = HelperFunctions.GetSelectedProject(QtVsToolsLegacyPackage.Instance.Dte);
                    var tracker = QtProjectTracker.Get(selectedProject, selectedProject.FullName);
                    QtProjectIntellisense.Refresh(tracker.Project);
                }
                break;
            }
        }

        private void beforeQueryStatus(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            var project = HelperFunctions.GetSelectedProject(QtVsToolsLegacyPackage.Instance.Dte);
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
            case CommandId.ExportPriFileProjectId:
            case CommandId.ExportProFileProjectId:
                command.Visible = true;
                command.Enabled = HelperFunctions.IsVsToolsProject(HelperFunctions
                    .GetSelectedProject(QtVsToolsLegacyPackage.Instance.Dte));
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
                command.Enabled = ((status & vsCommandStatus.vsCommandStatusEnabled) != 0);
                command.Visible = ((status & vsCommandStatus.vsCommandStatusInvisible) == 0);
                break;
            case CommandId.ChangeProjectQtVersionProjectId:
                if (isQMakeProject)
                    status |= vsCommandStatus.vsCommandStatusEnabled;
                else
                    status |= vsCommandStatus.vsCommandStatusInvisible;
                command.Enabled = ((status & vsCommandStatus.vsCommandStatusEnabled) != 0);
                command.Visible = ((status & vsCommandStatus.vsCommandStatusInvisible) == 0);
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
                case CommandId.ChangeProjectQtVersionProjectId:
                    if (projectVersion >= Resources.qtMinFormatVersion_Settings)
                        command.Visible = command.Enabled = false;
                    break;
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
