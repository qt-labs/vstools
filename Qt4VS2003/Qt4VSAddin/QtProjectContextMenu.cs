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
using System.Collections.Generic;
using System.ComponentModel.Design;

using VsCEO = EnvDTE.vsCommandExecOption;
using VsCSTW = EnvDTE.vsCommandStatusTextWanted;

namespace Qt5VSAddin
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtProjectContextMenu
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int ImportPriFileProjectId = 0x0114;
        public const int ExportProFileProjectId = 0x0115;
        public const int CreateProFileProjectId = 0x0116;
        public const int CreateNewTsFileProjectId = 0x0117;
        public const int lUpdateOnProjectId = 0x0118;
        public const int lReleaseOnProjectId = 0x0119;
        public const int ConvertToQtProjectId = 0x0120;
        public const int ConvertToQmakeProjectId = 0x0121;
        public const int QtProjectSettingsProjectId = 0x0122;
        public const int ChangeProjectQtVersionProjectId = 0x0123;

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
        /// Command ID dictionary maps new menu ids to old addin menu ids.
        /// </summary>
        private readonly Dictionary<int, string> commandIds;

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

            commandIds = new Dictionary<int, string> {
                { ImportPriFileProjectId, Res.ImportPriFileFullCommand },
                { ExportProFileProjectId, Res.ExportPriFileFullCommand },
                { CreateProFileProjectId, Res.ExportProFileFullCommand },
                { CreateNewTsFileProjectId, Res.CreateNewTranslationFileFullCommand },
                { lUpdateOnProjectId, Res.lupdateProjectFullCommand },
                { lReleaseOnProjectId, Res.lreleaseProjectFullCommand },
                { ConvertToQtProjectId, Res.ConvertToQtFullCommand },
                { ConvertToQmakeProjectId, Res.ConvertToQMakeFullCommand },
                { QtProjectSettingsProjectId, Res.ProjectQtSettingsFullCommand },
                { ChangeProjectQtVersionProjectId, Res.ChangeProjectQtVersionFullCommand }
            };

            foreach (var id in commandIds.Keys) {
                var command = new OleMenuCommand(new EventHandler(execHandler),
                    new CommandID(ProjectContextMenuGuid, id));
                command.BeforeQueryStatus += new EventHandler(beforeQueryStatus);
                commandService.AddCommand(command);
            }
        }

        private void execHandler(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            string resCommand = null;
            if (!commandIds.TryGetValue(command.CommandID.ID, out resCommand))
                return;

            object obj = null;
            bool handled = false;
            Connect.Instance.Exec(resCommand, VsCEO.vsCommandExecOptionDoDefault, ref obj, ref obj,
                ref handled);
        }

        private void beforeQueryStatus(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            string resCommand = null;
            if (!commandIds.TryGetValue(command.CommandID.ID, out resCommand))
                return;

            object obj = null;
            EnvDTE.vsCommandStatus status = EnvDTE.vsCommandStatus.vsCommandStatusUnsupported;
            Connect.Instance.QueryStatus(resCommand, VsCSTW.vsCommandStatusTextWantedNone, ref status,
                ref obj);
            command.Enabled = ((status & EnvDTE.vsCommandStatus.vsCommandStatusEnabled) != 0);
            command.Visible = ((status & EnvDTE.vsCommandStatus.vsCommandStatusInvisible) == 0);
        }
    }
}
