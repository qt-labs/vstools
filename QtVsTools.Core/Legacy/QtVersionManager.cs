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

using Microsoft.VisualStudio.Shell;

namespace QtVsTools.Core.Legacy
{
    public static class QtVersionManager
    {
        public static string GetSolutionQtVersion(EnvDTE.Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution == null)
                return null;

            if (solution.Globals.get_VariableExists("Qt5Version")) {
                var version = (string)solution.Globals["Qt5Version"];
                return Core.QtVersionManager.The().VerifyIfQtVersionExists(version) ? version : null;
            }

            return null;
        }

        public static bool SaveSolutionQtVersion(EnvDTE.Solution solution, string version)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!Core.QtVersionManager.The().IsVersionAvailable(version) && version != "$(DefaultQtVersion)")
                return false;

            solution.Globals["Qt5Version"] = version;
            if (!solution.Globals.get_VariablePersists("Qt5Version"))
                solution.Globals.set_VariablePersists("Qt5Version", true);
            return true;
        }
    }
}
