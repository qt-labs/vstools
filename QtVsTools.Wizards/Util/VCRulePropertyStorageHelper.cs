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

using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Collections.Generic;
using QtVsTools.Core;
using System.Linq;

namespace QtVsTools.Wizards.Util
{
    static class VCRulePropertyStorageHelper
    {
        public static void SetQtModules(DTE dte, List<string> modules)
        {
            var newModules = modules.ToHashSet();
            if (modules.Count == 0)
                return;

            var project = HelperFunctions.GetSelectedQtProject(dte);
            if (project == null)
                return;

            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var vcproject = project.Object as VCProject;
            if (vcproject == null)
                return;

            // TODO: There is already code providing such functionality, though it seems overly
            // complicated to use compared to this simple for loop (see VCPropertyStorageProvider).
            foreach (VCConfiguration config in vcproject.Configurations as IVCCollection) {
                var props = config.Rules.Item("QtRule10_Settings") as IVCRulePropertyStorage;
                var updatedModules = props.GetUnevaluatedPropertyValue("QtModules")
                    .Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries)
                    .ToHashSet()
                    .Union(newModules);
                props.SetPropertyValue("QtModules", string.Join(";", updatedModules));
            }
        }
    }
}
