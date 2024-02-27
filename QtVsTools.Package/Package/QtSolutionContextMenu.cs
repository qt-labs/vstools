/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace QtVsTools
{
    using Core;
    using Core.MsBuild;
    using VisualStudio;

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtSolutionContextMenu
    {
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
        /// TODO: Remove, take form QtMenus.Package
        /// </summary>
        private enum CommandId
        {
            lUpdateOnSolution = QtMenus.Package.lUpdateOnSolution,
            lReleaseOnSolution = QtMenus.Package.lReleaseOnSolution,
            SolutionConvertToQtMsBuild = QtMenus.Package.SolutionConvertToQtMsBuild,
            SolutionEnableProjectTracking = QtMenus.Package.SolutionEnableProjectTracking
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

            foreach (int id in Enum.GetValues(typeof(CommandId))) {
                var command = new OleMenuCommand(execHandler,
                    new CommandID(QtMenus.Package.Guid, id));
                commandService.AddCommand(command);
            }
        }

        private void execHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command)
                return;

            var dte = QtVsToolsPackage.Instance.Dte;
            switch (command.CommandID.ID) {
            case QtMenus.Package.lUpdateOnSolution:
                Translation.RunlUpdate(QtVsToolsPackage.Instance.Dte.Solution);
                break;
            case QtMenus.Package.lReleaseOnSolution:
                Translation.RunlRelease(QtVsToolsPackage.Instance.Dte.Solution);
                break;
            case QtMenus.Package.SolutionConvertToQtMsBuild:
                MsBuildProjectConverter.SolutionToQtMsBuild();
                break;
            case QtMenus.Package.SolutionEnableProjectTracking:
                foreach (var vcProject in HelperFunctions.ProjectsInSolution(dte))
                    MsBuildProject.GetOrAdd(vcProject);
                break;
            }
        }
    }
}
