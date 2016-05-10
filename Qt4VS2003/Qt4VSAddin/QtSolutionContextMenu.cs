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

using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

using VsCEO = EnvDTE.vsCommandExecOption;

namespace Qt5VSAddin
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtSolutionContextMenu
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int lUpdateOnSolutionId = 0x0111;
        public const int lReleaseOnSolutionId = 0x0112;
        public const int ChangeSolutionQtVersionId = 0x0113;


        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid SolutionContextMenuGuid = new Guid("6dcda34f-4d22-4d6a-a176-5507069c5a3e");

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static QtSolutionContextMenu Instance {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new QtSolutionContextMenu(package);
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
        private QtSolutionContextMenu(Package package)
        {
            if (package == null)
                throw new ArgumentNullException("package");

            m_package = package;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService))
                as OleMenuCommandService;
            if (commandService == null)
                return;

            commandService.AddCommand(new OleMenuCommand(new EventHandler(execHandler),
                new CommandID(SolutionContextMenuGuid, lUpdateOnSolutionId)));

            commandService.AddCommand(new OleMenuCommand(new EventHandler(execHandler),
                new CommandID(SolutionContextMenuGuid, lReleaseOnSolutionId)));

            commandService.AddCommand(new OleMenuCommand(new EventHandler(execHandler),
                new CommandID(SolutionContextMenuGuid, ChangeSolutionQtVersionId)));
        }

        private void execHandler(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            object obj = null;
            bool handled = false;
            switch (command.CommandID.ID) {
            case lUpdateOnSolutionId:
                Connect.Instance.Exec(Res.lupdateSolutionFullCommand, VsCEO.vsCommandExecOptionDoDefault,
                    ref obj, ref obj, ref handled);
                break;
            case lReleaseOnSolutionId:
                Connect.Instance.Exec(Res.lreleaseSolutionFullCommand, VsCEO.vsCommandExecOptionDoDefault,
                    ref obj, ref obj, ref handled);
                break;
            case ChangeSolutionQtVersionId:
                Connect.Instance.Exec(Res.ChangeSolutionQtVersionFullCommand,
                    VsCEO.vsCommandExecOptionDoDefault, ref obj, ref obj, ref handled);
                break;
            default:
                break;
            }
        }
    }
}
