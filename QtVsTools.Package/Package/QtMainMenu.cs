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
    using VisualStudio;

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtMainMenu
    {
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        private static readonly Guid MainMenuGuid = new Guid("58f83fff-d39d-4c66-810b-2702e1f04e73");

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
        /// </summary>
        private enum CommandId
        {
            QtVersionId = 0x0500,
            ViewQtHelpId = 0x0501,
            ViewGettingStartedId = 0x0503,
            LaunchDesignerId = 0x0100,
            LaunchLinguistId = 0x0101,
            OpenProFileId = 0x0102,
            ImportPriFileId = 0x0103,
            ConvertToQtMsBuild = 0x0130,
            QtProjectSettingsId = 0x0109,
            QtOptionsId = 0x0110,
            QtVersionsId = 0x0111
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

            foreach (var id in Enum.GetValues(typeof(CommandId))) {
                var command = new OleMenuCommand(execHandler,
                    new CommandID(MainMenuGuid, (int)id));
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
            case CommandId.ViewQtHelpId:
                VsShellUtilities.OpenSystemBrowser("https://www.qt.io/developers");
                break;
            case CommandId.ViewGettingStartedId:
                VsShellUtilities.OpenSystemBrowser("https://doc.qt.io/qtvstools/qtvstools-getting-started.html");
                break;
            case CommandId.LaunchDesignerId:
                QtVsToolsPackage.Instance.QtDesigner.Start(hideWindow: false);
                break;
            case CommandId.LaunchLinguistId:
                QtVsToolsPackage.Instance.QtLinguist.Start(hideWindow: false);
                break;
            case CommandId.OpenProFileId:
                ExtLoader.ImportProFile();
                break;
            case CommandId.ImportPriFileId:
                ExtLoader.ImportPriFile(HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte));
                break;
            case CommandId.ConvertToQtMsBuild:
                QtMsBuildConverter.SolutionToQtMsBuild();
                break;
            case CommandId.QtProjectSettingsId:
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
            case CommandId.QtOptionsId:
                QtVsToolsPackage.Instance.ShowOptionPage(typeof(Options.QtOptionsPage));
                break;
            case CommandId.QtVersionsId:
                QtVsToolsPackage.Instance.ShowOptionPage(typeof(Options.QtVersionsPage));
                break;
            }
        }

        private void beforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            var project = HelperFunctions.GetSelectedProject(QtVsToolsPackage.Instance.Dte);

            switch ((CommandId)command.CommandID.ID) {
            case CommandId.ViewQtHelpId:
            case CommandId.ViewGettingStartedId:
                command.Visible = command.Enabled = true;
                break;
            case CommandId.QtVersionId:
                command.Text = "Qt Visual Studio Tools version " + Version.USER_VERSION;
                command.Visible = true;
                command.Enabled = false;
                break;
            case CommandId.LaunchDesignerId:
            case CommandId.LaunchLinguistId:
            case CommandId.OpenProFileId:
            case CommandId.QtOptionsId:
            case CommandId.QtVersionsId:
                command.Visible = true;
                command.Enabled = true;
                break;
            case CommandId.ImportPriFileId:
                command.Visible = true;
                command.Enabled = HelperFunctions.IsVsToolsProject(project);
                break;
            case CommandId.QtProjectSettingsId: {
                    var status = vsCommandStatus.vsCommandStatusSupported;
                    if (project != null) {
                        if (HelperFunctions.IsVsToolsProject(project))
                            status |= vsCommandStatus.vsCommandStatusEnabled;
                        else if (HelperFunctions.IsQtProject(project))
                            status |= vsCommandStatus.vsCommandStatusInvisible;
                    }
                    command.Enabled = ((status & vsCommandStatus.vsCommandStatusEnabled) != 0);
                    command.Visible = ((status & vsCommandStatus.vsCommandStatusInvisible) == 0);
                }
                break;
            case CommandId.ConvertToQtMsBuild: {
                    command.Visible = true;
                    command.Enabled = (QtVsToolsPackage.Instance.Dte.Solution != null
                        && QtVsToolsPackage.Instance.Dte.Solution.Projects != null
                        && QtVsToolsPackage.Instance.Dte.Solution.Projects.Count > 0);
                }
                break;
            }
        }
    }
}
