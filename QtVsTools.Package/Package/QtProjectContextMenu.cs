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
using Microsoft.VisualStudio.Shell;
using QtVsTools.Core;
using QtVsTools.VisualStudio;
using System;
using System.ComponentModel.Design;
using System.Windows.Forms;

namespace QtVsTools
{
    using QtMsBuild;

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
            ConvertToQtProjectId = 0x0120,
            ConvertToQmakeProjectId = 0x0121,
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
                ExtLoader.ImportPriFile(HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte));
                break;
            case CommandId.ExportPriFileProjectId:
                ExtLoader.ExportPriFile();
                break;
            case CommandId.ExportProFileProjectId:
                ExtLoader.ExportProFile();
                break;
            case CommandId.lUpdateOnProjectId:
                Translation.RunlUpdate(HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte));
                break;
            case CommandId.lReleaseOnProjectId:
                Translation.RunlRelease(HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte));
                break;
            case CommandId.ConvertToQtProjectId:
            case CommandId.ConvertToQmakeProjectId: {
                    var caption = SR.GetString("ConvertTitle");
                    var text = SR.GetString("ConvertConfirmation");
                    if (MessageBox.Show(text, caption, MessageBoxButtons.YesNo) == DialogResult.Yes) {
                        HelperFunctions.ToggleProjectKind(HelperFunctions.GetSelectedProject(QtVsToolsPackage
                            .Instance.Dte));
                    }
                }
                break;
            case CommandId.QtProjectSettingsProjectId: {
                    var pro = HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte);
                    int projectVersion = QtProject.GetFormatVersion(pro);
                    if (projectVersion >= Resources.qtMinFormatVersion_Settings) {
                        QtVsToolsPackage.Instance.Dte.ExecuteCommand("Project.Properties");
                    } else if (pro != null) {
                        using (var formProjectQtSettings = new FormProjectQtSettings()) {
                            formProjectQtSettings.SetProject(pro);
                            formProjectQtSettings.StartPosition = FormStartPosition.CenterParent;
                            var ww = new MainWinWrapper(QtVsToolsPackage.Instance.Dte);
                            formProjectQtSettings.ShowDialog(ww);
                        }
                    } else {
                        MessageBox.Show(SR.GetString("NoProjectOpened"));
                    }
                }
                break;
            case CommandId.ChangeProjectQtVersionProjectId: {
                    var pro = HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte);
                    if (HelperFunctions.IsQMakeProject(pro)) {
                        using (var formChangeQtVersion = new FormChangeQtVersion()) {
                            formChangeQtVersion.UpdateContent(ChangeFor.Project);
                            var ww = new MainWinWrapper(QtVsToolsPackage.Instance.Dte);
                            if (formChangeQtVersion.ShowDialog(ww) == DialogResult.OK) {
                                var qtVersion = formChangeQtVersion.GetSelectedQtVersion();
                                HelperFunctions.SetDebuggingEnvironment(pro, "PATH=" + QtVersionManager
                                    .The().GetInstallPath(qtVersion) + @"\bin;$(PATH)", true);
                            }
                        }
                    }
                }
                break;
            case CommandId.ProjectConvertToQtMsBuild: {
                    QtMsBuildConverter.ProjectToQtMsBuild(
                        HelperFunctions.GetSelectedProject(QtVsToolsPackage.Instance.Dte));
                }
                break;
            case CommandId.ProjectRefreshIntelliSense: {
                    var selectedProject = HelperFunctions.GetSelectedProject(QtVsToolsPackage.Instance.Dte);
                    var tracker = QtProjectTracker.Get(selectedProject);
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

            var project = HelperFunctions.GetSelectedProject(QtVsToolsPackage.Instance.Dte);
            var isQtProject = HelperFunctions.IsQtProject(project);
            var isQMakeProject = HelperFunctions.IsQMakeProject(project);
            var isQtMsBuildEnabled = QtProject.IsQtMsBuildEnabled(project);

            if (!isQtProject && !isQMakeProject) {
                command.Enabled = command.Visible = false;
                return;
            }

            switch ((CommandId)command.CommandID.ID) {
            // TODO: Fix these functionality and re-enable the menu items
            case CommandId.ConvertToQtProjectId:
            case CommandId.ConvertToQmakeProjectId: {
                    command.Visible = false;
                }
                break;
            case CommandId.ImportPriFileProjectId:
            case CommandId.ExportPriFileProjectId:
            case CommandId.ExportProFileProjectId:
                command.Visible = true;
                command.Enabled = HelperFunctions.IsQtProject(HelperFunctions
                    .GetSelectedProject(QtVsToolsPackage.Instance.Dte));
                break;
            case CommandId.lUpdateOnProjectId:
            case CommandId.lReleaseOnProjectId:
                command.Visible = true;
                command.Enabled = Translation.ToolsAvailable(project);
                break;
            //case CommandId.ConvertToQmakeProjectId:
            case CommandId.QtProjectSettingsProjectId: {
                    var status = vsCommandStatus.vsCommandStatusSupported;
                    if (project != null) {
                        if (isQtProject)
                            status |= vsCommandStatus.vsCommandStatusEnabled;
                        else if (isQMakeProject)
                            status |= vsCommandStatus.vsCommandStatusInvisible;
                    }
                    command.Enabled = ((status & vsCommandStatus.vsCommandStatusEnabled) != 0);
                    command.Visible = ((status & vsCommandStatus.vsCommandStatusInvisible) == 0);
                }
                break;
            //case CommandId.ConvertToQtProjectId:
            case CommandId.ChangeProjectQtVersionProjectId: {
                    var status = vsCommandStatus.vsCommandStatusSupported;
                    if ((project == null) || isQtProject)
                        status |= vsCommandStatus.vsCommandStatusInvisible;
                    else if (isQMakeProject)
                        status |= vsCommandStatus.vsCommandStatusEnabled;
                    else
                        status |= vsCommandStatus.vsCommandStatusInvisible;
                    command.Enabled = ((status & vsCommandStatus.vsCommandStatusEnabled) != 0);
                    command.Visible = ((status & vsCommandStatus.vsCommandStatusInvisible) == 0);
                }
                break;
            case CommandId.ProjectConvertToQtMsBuild: {
                    if (project == null || (!isQtProject && !isQMakeProject)) {
                        command.Visible = false;
                        command.Enabled = false;
                    } else if (isQtMsBuildEnabled) {
                        command.Visible = true;
                        command.Enabled = false;
                    } else {
                        command.Visible = true;
                        command.Enabled = true;
                    }
                }
                break;
            case CommandId.ProjectRefreshIntelliSense: {
                    command.Visible = command.Enabled = isQtMsBuildEnabled;
                }
                break;
            }

            if (project != null && isQtProject) {
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
