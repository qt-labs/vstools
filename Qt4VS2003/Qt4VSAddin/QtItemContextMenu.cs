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

namespace Qt5VSAddin
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtItemContextMenu
    {
        #region public

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid ItemContextMenuGuid = new Guid("9f67a0bd-ee0a-47e3-b656-5efb12e3c770");

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static QtItemContextMenu Instance {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new QtItemContextMenu(package);
        }

        #endregion public

        #region private

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package m_package;

        /// <summary>
        /// Command ID.
        /// </summary>
        private const int lUpdateOnItemId = 0x0125;
        private const int lReleaseOnItemId = 0x0126;

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
        private QtItemContextMenu(Package package)
        {
            if (package == null)
                throw new ArgumentNullException("package");

            m_package = package;

            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService))
                as OleMenuCommandService;
            if (commandService == null)
                return;

            var command = new OleMenuCommand(new EventHandler(execHandler),
                new CommandID(ItemContextMenuGuid, lUpdateOnItemId));
            command.BeforeQueryStatus += new EventHandler(beforeQueryStatus);
            commandService.AddCommand(command);

            command = new OleMenuCommand(new EventHandler(execHandler),
                new CommandID(ItemContextMenuGuid, lReleaseOnItemId));
            command.BeforeQueryStatus += new EventHandler(beforeQueryStatus);
            commandService.AddCommand(command);
        }

        private void execHandler(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            switch (command.CommandID.ID) {
            case lUpdateOnItemId:
                Translation.RunlUpdate(HelperFunctions.GetSelectedFiles(Vsix.Instance.Dte),
                    HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte));
                break;
            case lReleaseOnItemId:
                Translation.RunlRelease(HelperFunctions.GetSelectedFiles(Vsix.Instance.Dte));
                break;
            default:
                break;
            }
        }

        private void beforeQueryStatus(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            command.Enabled = false;
            command.Visible = false;

            var prj = HelperFunctions.GetSelectedProject(Vsix.Instance.Dte);
            if (!HelperFunctions.IsQtProject(prj) || Vsix.Instance.Dte.SelectedItems.Count <= 0)
                return;

            foreach (SelectedItem si in Vsix.Instance.Dte.SelectedItems) {
                if (!si.Name.ToLower().EndsWith(".ts"))
                    return; // Don't display commands if one of the selected files is not a .ts file.
            }

            command.Enabled = true;
            command.Visible = true;
        }

        #endregion private
    }
}
