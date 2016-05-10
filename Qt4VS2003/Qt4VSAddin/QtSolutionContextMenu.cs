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
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Windows.Forms;

namespace Qt5VSAddin
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtSolutionContextMenu
    {
        #region public

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

        #endregion public

        #region private

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package m_package;

        /// <summary>
        /// Command ID.
        /// </summary>
        private const int lUpdateOnSolutionId = 0x0111;
        private const int lReleaseOnSolutionId = 0x0112;
        private const int ChangeSolutionQtVersionId = 0x0113;

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

            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService))
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

            switch (command.CommandID.ID) {
            case lUpdateOnSolutionId:
                Translation.RunlUpdate(Vsix.Instance.Dte.Solution);
                break;
            case lReleaseOnSolutionId:
                Translation.RunlRelease(Vsix.Instance.Dte.Solution);
                break;
            case ChangeSolutionQtVersionId:
                var formChangeQtVersion = new FormChangeQtVersion();
                formChangeQtVersion.UpdateContent(ChangeFor.Solution);
                if (formChangeQtVersion.ShowDialog() != DialogResult.OK)
                    return;

                var newQtVersion = formChangeQtVersion.GetSelectedQtVersion();
                if (newQtVersion == null)
                    return;

                string currentPlatform = null;
                try {
                    var config2 = Vsix.Instance.Dte.Solution.SolutionBuild
                        .ActiveConfiguration as SolutionConfiguration2;
                    currentPlatform = config2.PlatformName;
                } catch { }
                if (string.IsNullOrEmpty(currentPlatform))
                    return;

                var dte = Vsix.Instance.Dte;
                foreach (Project project in HelperFunctions.ProjectsInSolution(dte)) {
                    if (HelperFunctions.IsQtProject(project)) {
                        var OldQtVersion = QtVersionManager.The().GetProjectQtVersion(project,
                            currentPlatform);
                        if (OldQtVersion == null)
                            OldQtVersion = QtVersionManager.The().GetDefaultVersion();

                        bool created = false;
                        var qtProject = QtProject.Create(project);
                        qtProject.ChangeQtVersion(OldQtVersion, newQtVersion, ref created);
                    }
                }
                QtVersionManager.The().SaveSolutionQtVersion(dte.Solution, newQtVersion);
                break;
            }
        }

        #endregion private
    }
}
