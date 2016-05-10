/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Add-in.
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

using Digia.Qt5ProjectLib;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Windows.Forms;

namespace Qt5VSAddin
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
        public static QtProjectContextMenu Instance {
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
        private enum CommandId : int
        {
            ImportPriFileProjectId = 0x0114,
            ExportPriFileProjectId = 0x0115,
            ExportProFileProjectId = 0x0116,
            CreateNewTsFileProjectId = 0x0117,
            lUpdateOnProjectId = 0x0118,
            lReleaseOnProjectId = 0x0119,
            ConvertToQtProjectId = 0x0120,
            ConvertToQmakeProjectId = 0x0121,
            QtProjectSettingsProjectId = 0x0122,
            ChangeProjectQtVersionProjectId = 0x0123
        }

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package m_package;

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider {
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

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService))
                as OleMenuCommandService;
            if (commandService == null)
                return;

            foreach (var id in Enum.GetValues(typeof(CommandId))) {
                var command = new OleMenuCommand(new EventHandler(execHandler),
                    new CommandID(ProjectContextMenuGuid, (int)id));
                command.BeforeQueryStatus += new EventHandler(beforeQueryStatus);
                commandService.AddCommand(command);
            }
        }

        private void execHandler(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            switch ((CommandId)command.CommandID.ID) {
            case CommandId.ImportPriFileProjectId:
                ExtLoader.ImportPriFile(HelperFunctions.GetSelectedQtProject(Connect.Instance.Dte));
                break;
            case CommandId.ExportPriFileProjectId:
                ExtLoader.ExportPriFile();
                break;
            case CommandId.ExportProFileProjectId:
                ExtLoader.ExportProFile();
                break;
            case CommandId.CreateNewTsFileProjectId:
                Translation.CreateNewTranslationFile(HelperFunctions.GetSelectedQtProject(Connect
                    .Instance.Dte));
                break;
            case CommandId.lUpdateOnProjectId:
                Translation.RunlUpdate(HelperFunctions.GetSelectedQtProject(Connect.Instance.Dte));
                break;
            case CommandId.lReleaseOnProjectId:
                Translation.RunlRelease(HelperFunctions.GetSelectedQtProject(Connect.Instance.Dte));
                break;
            case CommandId.ConvertToQtProjectId:
            case CommandId.ConvertToQmakeProjectId: {
                var caption = SR.GetString("ConvertTitle");
                var text = SR.GetString("ConvertConfirmation");
                if (MessageBox.Show(text, caption, MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    HelperFunctions.ToggleProjectKind(HelperFunctions.GetSelectedProject(Connect
                        .Instance.Dte));
                }
            } break;
            case CommandId.QtProjectSettingsProjectId: {
                var pro = HelperFunctions.GetSelectedQtProject(Connect.Instance.Dte);
                if (pro != null) {
                    var formProjectQtSettings = new FormProjectQtSettings();
                    formProjectQtSettings.SetProject(pro);
                    formProjectQtSettings.StartPosition = FormStartPosition.CenterParent;
                    var ww = new MainWinWrapper(Connect.Instance.Dte);
                    formProjectQtSettings.ShowDialog(ww);
                } else {
                    MessageBox.Show(SR.GetString("NoProjectOpened"));
                }
            }   break;
            case CommandId.ChangeProjectQtVersionProjectId: {
                var pro = HelperFunctions.GetSelectedQtProject(Connect.Instance.Dte);
                if (HelperFunctions.IsQMakeProject(pro)) {
                    var formChangeQtVersion = new FormChangeQtVersion();
                    formChangeQtVersion.UpdateContent(ChangeFor.Project);
                    var ww = new MainWinWrapper(Connect.Instance.Dte);
                    if (formChangeQtVersion.ShowDialog(ww) == DialogResult.OK) {
                        string qtVersion = formChangeQtVersion.GetSelectedQtVersion();
                        HelperFunctions.SetDebuggingEnvironment(pro, "PATH=" + QtVersionManager
                            .The().GetInstallPath(qtVersion) + @"\bin;$(PATH)", true);
                    }
                }
            }   break;
            default:
                break;
            }
        }

        private void beforeQueryStatus(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            switch ((CommandId)command.CommandID.ID) {
            case CommandId.ImportPriFileProjectId:
            case CommandId.ExportPriFileProjectId:
            case CommandId.ExportProFileProjectId:
            case CommandId.CreateNewTsFileProjectId:
            case CommandId.lUpdateOnProjectId:
            case CommandId.lReleaseOnProjectId:
                command.Visible = true;
                command.Enabled = HelperFunctions.IsQtProject(HelperFunctions
                    .GetSelectedProject(Connect.Instance.Dte));
                break;
            case CommandId.ConvertToQmakeProjectId:
            case CommandId.QtProjectSettingsProjectId: {
                var status = vsCommandStatus.vsCommandStatusSupported;
                var project = HelperFunctions.GetSelectedProject(Connect.Instance.Dte);
                if (project != null) {
                    if (HelperFunctions.IsQtProject(project))
                        status |= vsCommandStatus.vsCommandStatusEnabled;
                    else if ((project != null) && HelperFunctions.IsQMakeProject(project))
                        status |= vsCommandStatus.vsCommandStatusInvisible;
                }
                command.Enabled = ((status & vsCommandStatus.vsCommandStatusEnabled) != 0);
                command.Visible = ((status & vsCommandStatus.vsCommandStatusInvisible) == 0);
            } break;
            case CommandId.ConvertToQtProjectId:
            case CommandId.ChangeProjectQtVersionProjectId: {
                var status = vsCommandStatus.vsCommandStatusSupported;
                var project = HelperFunctions.GetSelectedProject(Connect.Instance.Dte);
                if ((project == null) || HelperFunctions.IsQtProject(project))
                    status |= vsCommandStatus.vsCommandStatusInvisible;
                else if (HelperFunctions.IsQMakeProject(project))
                    status |= vsCommandStatus.vsCommandStatusEnabled;
                else
                    status |= vsCommandStatus.vsCommandStatusInvisible;
                command.Enabled = ((status & vsCommandStatus.vsCommandStatusEnabled) != 0);
                command.Visible = ((status & vsCommandStatus.vsCommandStatusInvisible) == 0);
            } break;
            default:
                break;
            }
        }
    }
}
