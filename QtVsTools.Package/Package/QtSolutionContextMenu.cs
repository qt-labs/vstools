/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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
            SolutionEnableProjectTracking = 0x1130
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

            var dte = QtVsToolsPackage.Instance.Dte;
            switch ((CommandId)command.CommandID.ID) {
            case CommandId.lUpdateOnSolutionId:
                Translation.RunlUpdate(QtVsToolsPackage.Instance.Dte.Solution);
                break;
            case CommandId.lReleaseOnSolutionId:
                Translation.RunlRelease(QtVsToolsPackage.Instance.Dte.Solution);
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
