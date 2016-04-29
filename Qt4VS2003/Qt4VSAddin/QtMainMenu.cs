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
    internal sealed class QtMainMenu
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int LaunchDesignerId = 0x0100;
        public const int LaunchLinguistId = 0x0101;
        public const int OpenProFileId = 0x0102;
        public const int ImportPriFileId = 0x0103;
        public const int ExportProFileId = 0x0104;
        public const int CreateProFileId = 0x0105;
        public const int CreateNewTsFileId = 0x0107;
        public const int ConvertToQtId = 0x0124;
        public const int ConvertToQmakeId = 0x0108;
        public const int QtProjectSettingsId = 0x0109;
        public const int ChangeProjectQtVersionId = 0x0126;
        public const int QtOptionsId = 0x0110;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid MainMenuGuid = new Guid("58f83fff-d39d-4c66-810b-2702e1f04e73");

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static QtMainMenu Instance {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new QtMainMenu(package);
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
        private QtMainMenu(Package package)
        {
            if (package == null)
                throw new ArgumentNullException("package");

            m_package = package;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService))
                as OleMenuCommandService;
            if (commandService == null)
                return;

            commandIds = new Dictionary<int, string> {
                { LaunchDesignerId, Res.LaunchDesignerFullCommand },
                { LaunchLinguistId, Res.LaunchLinguistFullCommand },
                { OpenProFileId, Res.ImportProFileFullCommand },
                { ImportPriFileId, Res.ImportPriFileFullCommand },
                { ExportProFileId, Res.ExportPriFileFullCommand },
                { CreateProFileId, Res.ExportProFileFullCommand },
                { CreateNewTsFileId, Res.CreateNewTranslationFileFullCommand },
                { ConvertToQtId, Res.ConvertToQtFullCommand },
                { ConvertToQmakeId, Res.ConvertToQMakeFullCommand },
                { QtProjectSettingsId, Res.ProjectQtSettingsFullCommand },
                { ChangeProjectQtVersionId, Res.ChangeProjectQtVersionFullCommand },
                { QtOptionsId, Res.VSQtOptionsFullCommand }
            };

            foreach (var id in commandIds.Keys) {
                var command = new OleMenuCommand(new EventHandler(execHandler),
                    new CommandID(MainMenuGuid, id));
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
            Connect.Instance().Exec(resCommand, VsCEO.vsCommandExecOptionDoDefault, ref obj, ref obj,
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
            Connect.Instance().QueryStatus(resCommand, VsCSTW.vsCommandStatusTextWantedNone, ref status,
                ref obj);
            command.Enabled = ((status & EnvDTE.vsCommandStatus.vsCommandStatusEnabled) != 0);
            command.Visible = ((status & EnvDTE.vsCommandStatus.vsCommandStatusInvisible) == 0);
        }
    }
}
