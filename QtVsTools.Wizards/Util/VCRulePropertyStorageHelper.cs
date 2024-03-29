/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using EnvDTE;

namespace QtVsTools.Wizards.Util
{
    using Core;

    static class VCRulePropertyStorageHelper
    {
        public static void SetQtModules(DTE dte, List<string> modules)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var newModules = modules.ToHashSet();
            if (modules.Count == 0)
                return;

            var qtProject = HelperFunctions.GetSelectedQtProject(dte);
            if (qtProject?.VcProject?.Configurations is not IVCCollection collection)
                return;

            // TODO: There is already code providing such functionality, though it seems overly
            // complicated to use compared to this simple for loop (see VCPropertyStorageProvider).
            foreach (VCConfiguration config in collection) {
                if (config.Rules.Item("QtRule10_Settings") is not IVCRulePropertyStorage props)
                    continue;
                var updatedModules = props.GetUnevaluatedPropertyValue("QtModules")
                    .Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries)
                    .ToHashSet()
                    .Union(newModules);
                props.SetPropertyValue("QtModules", string.Join(";", updatedModules));
            }
        }
    }
}
