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
using Microsoft.VisualStudio.Shell;

namespace QtVsTools
{
    using Core;
    using QtMsBuild;
    using VisualStudio;

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtSolutionContextMenu
    {
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        private static readonly Guid SolutionContextMenuGuid = new Guid("6dcda34f-4d22-4d6a-a176-5507069c5a3e");

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        private static QtSolutionContextMenu Instance
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        public static void Initialize()
        {
            Instance = new QtSolutionContextMenu();
        }

        /// <summary>
        /// Command ID.
        /// </summary>
        private enum CommandId
        {
            lUpdateOnSolutionId = 0x0111,
            lReleaseOnSolutionId = 0x0112,
            SolutionConvertToQtMsBuild = 0x0130,
            SolutionEnableProjectTracking = 0x1130,
            ChangeSolutionQtVersionId = 0x0113
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QtMainMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        private QtSolutionContextMenu()
        {
            var commandService = VsServiceProvider
                .GetService<IMenuCommandService, OleMenuCommandService>();
            if (commandService == null)
                return;

            foreach (var id in Enum.GetValues(typeof(CommandId))) {
                var command = new OleMenuCommand(execHandler,
                    new CommandID(SolutionContextMenuGuid, (int)id));
                command.BeforeQueryStatus += beforeQueryStatus;
                commandService.AddCommand(command);
            }
        }

        private void beforeQueryStatus(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            switch (command.CommandID.ID) {
            case (int)CommandId.ChangeSolutionQtVersionId:
                var projects = HelperFunctions.ProjectsInSolution(QtVsToolsLegacyPackage.Instance.Dte);
                foreach (var project in projects) {
                    if (!HelperFunctions.IsVsToolsProject(project)
                        && HelperFunctions.IsQtProject(project)) {
                        command.Enabled = command.Visible = true;
                        return;
                    }
                }
                command.Enabled = command.Visible = false;
                break;
            default:
                command.Enabled = command.Visible = true;
                break;
            }
        }

        private void execHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            var dte = QtVsToolsLegacyPackage.Instance.Dte;
            switch ((CommandId)command.CommandID.ID) {
            case CommandId.lUpdateOnSolutionId:
                Translation.RunlUpdate(QtVsToolsLegacyPackage.Instance.Dte.Solution);
                break;
            case CommandId.lReleaseOnSolutionId:
                Translation.RunlRelease(QtVsToolsLegacyPackage.Instance.Dte.Solution);
                break;
            case CommandId.ChangeSolutionQtVersionId:
                Legacy.QtMenu.ShowFormChangeSolutionQtVersion();
                break;
            case CommandId.SolutionConvertToQtMsBuild:
                QtMsBuildConverter.SolutionToQtMsBuild();
                break;
            case CommandId.SolutionEnableProjectTracking: {
                    foreach (var project in HelperFunctions.ProjectsInSolution(dte)) {
                        if (HelperFunctions.IsVsToolsProject(project))
                            QtProjectTracker.Get(project, project.FullName);
                    }
                }
                break;
            }
        }
    }
}
