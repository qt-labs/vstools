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

using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using QtVsTools.Core;

namespace QtVsTools.Legacy
{
    internal static class QtMenu
    {
        internal static void ShowFormProjectQtSettings(Project pro)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var vm = QtVersionManager.The();
            var versionInfo = vm.GetVersionInfo(pro);
            if (versionInfo == null)
                versionInfo = vm.GetVersionInfo(vm.GetDefaultVersion());
            using (var form = new FormProjectQtSettings(versionInfo.qtMajor)) {
                form.SetProject(pro);
                form.StartPosition = FormStartPosition.CenterParent;
                var ww = new MainWinWrapper(QtVsToolsPackage.Instance.Dte);
                form.ShowDialog(ww);
            }
        }

        internal static void ShowFormChangeProjectQtVersion()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var pro = HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte);
            if (!HelperFunctions.IsQtProject(pro))
                return;

            using (var formChangeQtVersion = new FormChangeQtVersion()) {
                formChangeQtVersion.UpdateContent(Legacy.ChangeFor.Project);
                var ww = new MainWinWrapper(QtVsToolsPackage.Instance.Dte);
                if (formChangeQtVersion.ShowDialog(ww) == DialogResult.OK) {
                    var qtVersion = formChangeQtVersion.GetSelectedQtVersion();
                    HelperFunctions.SetDebuggingEnvironment(pro, "PATH=" + QtVersionManager
                        .The().GetInstallPath(qtVersion) + @"\bin;$(PATH)", true);
                }
            }
        }

        internal static void ShowFormChangeSolutionQtVersion()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string newQtVersion = null;
            using (var formChangeQtVersion = new FormChangeQtVersion()) {
                formChangeQtVersion.UpdateContent(Legacy.ChangeFor.Solution);
                if (formChangeQtVersion.ShowDialog() != DialogResult.OK)
                    return;
                newQtVersion = formChangeQtVersion.GetSelectedQtVersion();
            }
            if (newQtVersion == null)
                return;

            string platform = null;
            var dte = QtVsToolsPackage.Instance.Dte;
            try {
                platform = (dte.Solution.SolutionBuild.ActiveConfiguration as SolutionConfiguration2)
                    .PlatformName;
            } catch { }
            if (string.IsNullOrEmpty(platform))
                return;

            var vm = QtVersionManager.The();
            foreach (var project in HelperFunctions.ProjectsInSolution(dte)) {
                if (HelperFunctions.IsVsToolsProject(project)) {
                    var OldQtVersion = vm.GetProjectQtVersion(project, platform);
                    if (OldQtVersion == null)
                        OldQtVersion = vm.GetDefaultVersion();

                    var created = false;
                    var qtProject = QtProject.Create(project);
                    if (qtProject.PromptChangeQtVersion(OldQtVersion, newQtVersion))
                        qtProject.ChangeQtVersion(OldQtVersion, newQtVersion, ref created);
                }
            }
            Core.Legacy.QtVersionManager.SaveSolutionQtVersion(dte.Solution, newQtVersion);
        }
    }
}
