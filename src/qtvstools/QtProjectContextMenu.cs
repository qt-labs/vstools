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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using QtProjectLib;
using QtVsTools.VisualStudio;
using System;
using System.ComponentModel.Design;
using System.Windows.Forms;

namespace QtVsTools
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtProjectContextMenu
    {
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid ProjectContextMenuGuid = new Guid("5732faa9-6074-4e07-b035-2816e809f50e");

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static QtProjectContextMenu Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new QtProjectContextMenu(package);
        }

        /// <summary>
        /// Command ID.
        /// </summary>
        private enum CommandId
        {
            ImportPriFileProjectId = 0x0114,
            ExportPriFileProjectId = 0x0115,
            ExportProFileProjectId = 0x0116,
            CreateNewTsFileProjectId = 0x0117,
            lUpdateOnProjectId = 0x0118,
            lReleaseOnProjectId = 0x0119,
            ProjectConvertToQtMsBuild = 0x0130,
            ConvertToQtProjectId = 0x0120,
            ConvertToQmakeProjectId = 0x0121,
            QtProjectSettingsProjectId = 0x0122,
            ChangeProjectQtVersionProjectId = 0x0123,
            ProjectAddNewQtClassProjectId = 0x200
        }

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package m_package;

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get { return m_package; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QtMainMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private QtProjectContextMenu(Package package)
        {
            if (package == null)
                throw new ArgumentNullException("package");

            m_package = package;

            var commandService = VsServiceProvider
                .GetService<IMenuCommandService, OleMenuCommandService>();
            if (commandService == null)
                return;

            foreach (var id in Enum.GetValues(typeof(CommandId))) {
                var command = new OleMenuCommand(execHandler,
                    new CommandID(ProjectContextMenuGuid, (int) id));
                command.BeforeQueryStatus += beforeQueryStatus;
                commandService.AddCommand(command);
            }
        }

        private void execHandler(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            switch ((CommandId) command.CommandID.ID) {
            case CommandId.ImportPriFileProjectId:
                ExtLoader.ImportPriFile(HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte));
                break;
            case CommandId.ExportPriFileProjectId:
                ExtLoader.ExportPriFile();
                break;
            case CommandId.ExportProFileProjectId:
                ExtLoader.ExportProFile();
                break;
            case CommandId.CreateNewTsFileProjectId:
                Translation.CreateNewTranslationFile(HelperFunctions.GetSelectedQtProject(Vsix
                    .Instance.Dte));
                break;
            case CommandId.lUpdateOnProjectId:
                Translation.RunlUpdate(HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte));
                break;
            case CommandId.lReleaseOnProjectId:
                Translation.RunlRelease(HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte));
                break;
            case CommandId.ConvertToQtProjectId:
            case CommandId.ConvertToQmakeProjectId:
                {
                    var caption = SR.GetString("ConvertTitle");
                    var text = SR.GetString("ConvertConfirmation");
                    if (MessageBox.Show(text, caption, MessageBoxButtons.YesNo) == DialogResult.Yes) {
                        HelperFunctions.ToggleProjectKind(HelperFunctions.GetSelectedProject(Vsix
                            .Instance.Dte));
                    }
                }
                break;
            case CommandId.QtProjectSettingsProjectId:
                {
                    var pro = HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte);
                    if (pro != null) {
                        using (var formProjectQtSettings = new FormProjectQtSettings()) {
                            formProjectQtSettings.SetProject(pro);
                            formProjectQtSettings.StartPosition = FormStartPosition.CenterParent;
                            var ww = new MainWinWrapper(Vsix.Instance.Dte);
                            formProjectQtSettings.ShowDialog(ww);
                        }
                    } else {
                        MessageBox.Show(SR.GetString("NoProjectOpened"));
                    }
                }
                break;
            case CommandId.ChangeProjectQtVersionProjectId:
                {
                    var pro = HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte);
                    if (HelperFunctions.IsQMakeProject(pro)) {
                        using (var formChangeQtVersion = new FormChangeQtVersion()) {
                            formChangeQtVersion.UpdateContent(ChangeFor.Project);
                            var ww = new MainWinWrapper(Vsix.Instance.Dte);
                            if (formChangeQtVersion.ShowDialog(ww) == DialogResult.OK) {
                                var qtVersion = formChangeQtVersion.GetSelectedQtVersion();
                                HelperFunctions.SetDebuggingEnvironment(pro, "PATH=" + QtVersionManager
                                    .The().GetInstallPath(qtVersion) + @"\bin;$(PATH)", true);
                            }
                        }
                    }
                }
                break;
            case CommandId.ProjectConvertToQtMsBuild:
                {
                    QtMsBuildConverter.ProjectToQtMsBuild(
                        HelperFunctions.GetSelectedProject(Vsix.Instance.Dte));
                }
                break;
            case CommandId.ProjectAddNewQtClassProjectId:
                {
                    try {
                        var project = HelperFunctions.GetSelectedProject(Vsix.Instance.Dte);
                        if (!HelperFunctions.IsQtProject(project))
                            return;

                        var vcProject = project.Object as VCProject;
                        if (vcProject == null)
                            return;

                        var loop = true;
                        do {
                            var classWizard = new Wizards.ClassWizard.AddClassWizard();
                            loop = classWizard.Run(Vsix.Instance.Dte, vcProject.Name,
                                vcProject.ProjectDirectory) == Wizards.WizardResult.Exception;
                        } while (loop);
                    } catch {
                        // Deliberately ignore any kind of exception but close the dialog.
                    }
                }
                break;
            }
        }

        private void beforeQueryStatus(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            var project = HelperFunctions.GetSelectedProject(Vsix.Instance.Dte);
            var isQtProject = HelperFunctions.IsQtProject(project);
            var isQMakeProject = HelperFunctions.IsQMakeProject(project);
            var isQtMsBuildEnabled = QtProject.IsQtMsBuildEnabled(project);

            if (!isQtProject && !isQMakeProject) {
                command.Enabled = command.Visible = false;
                return;
            }

            switch ((CommandId) command.CommandID.ID) {
            // TODO: Fix these functionality and re-enable the menu items
            case CommandId.ConvertToQtProjectId:
            case CommandId.ConvertToQmakeProjectId:
                {
                    command.Visible = false;
                }
                break;
            case CommandId.ImportPriFileProjectId:
            case CommandId.ExportPriFileProjectId:
            case CommandId.ExportProFileProjectId:
            case CommandId.CreateNewTsFileProjectId:
            case CommandId.lUpdateOnProjectId:
            case CommandId.lReleaseOnProjectId:
                command.Visible = true;
                command.Enabled = HelperFunctions.IsQtProject(HelperFunctions
                    .GetSelectedProject(Vsix.Instance.Dte));
                break;
            //case CommandId.ConvertToQmakeProjectId:
            case CommandId.QtProjectSettingsProjectId:
            case CommandId.ProjectAddNewQtClassProjectId:
                {
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
            case CommandId.ChangeProjectQtVersionProjectId:
                {
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
            case CommandId.ProjectConvertToQtMsBuild:
                {
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
            }

            if (project != null && isQtProject) {
                int projectVersion = QtProject.GetFormatVersion(project);
                switch ((CommandId)command.CommandID.ID) {
                    case CommandId.QtProjectSettingsProjectId:
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
