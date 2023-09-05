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

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace QtVsTools
{
    using Core;
    using VisualStudio;

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtItemContextMenu
    {
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        private static readonly Guid ItemContextMenuGuid = new Guid("9f67a0bd-ee0a-47e3-b656-5efb12e3c770");

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        private static QtItemContextMenu Instance
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        public static void Initialize()
        {
            Instance = new QtItemContextMenu();
        }

        /// <summary>
        /// Command ID.
        /// </summary>
        private const int lUpdateOnItemId = 0x0125;
        private const int lReleaseOnItemId = 0x0126;

        /// <summary>
        /// Initializes a new instance of the <see cref="QtMainMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        private QtItemContextMenu()
        {
            var commandService = VsServiceProvider
                .GetService<IMenuCommandService, OleMenuCommandService>();
            if (commandService == null)
                return;

            var command = new OleMenuCommand(execHandler,
                new CommandID(ItemContextMenuGuid, lUpdateOnItemId));
            command.BeforeQueryStatus += beforeQueryStatus;
            commandService.AddCommand(command);

            command = new OleMenuCommand(execHandler,
                new CommandID(ItemContextMenuGuid, lReleaseOnItemId));
            command.BeforeQueryStatus += beforeQueryStatus;
            commandService.AddCommand(command);
        }

        private void execHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            switch (command.CommandID.ID) {
            case lUpdateOnItemId:
                Translation.RunlUpdate(HelperFunctions.GetSelectedFiles(QtVsToolsLegacyPackage.Instance.Dte));
                break;
            case lReleaseOnItemId:
                Translation.RunlRelease(HelperFunctions.GetSelectedFiles(QtVsToolsLegacyPackage.Instance.Dte));
                break;
            }
        }

        private void beforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            command.Enabled = false;
            command.Visible = false;

            var prj = HelperFunctions.GetSelectedProject(QtVsToolsLegacyPackage.Instance.Dte);
            if (!HelperFunctions.IsVsToolsProject(prj) || QtVsToolsLegacyPackage.Instance.Dte.SelectedItems.Count <= 0)
                return;

            foreach (SelectedItem si in QtVsToolsLegacyPackage.Instance.Dte.SelectedItems) {
                if (!HelperFunctions.IsTranslationFile(si.Name))
                    return; // Don't display commands if one of the selected files is not a .ts file.
            }

            command.Enabled = Translation.ToolsAvailable(prj);
            command.Visible = true;
        }
    }
}
