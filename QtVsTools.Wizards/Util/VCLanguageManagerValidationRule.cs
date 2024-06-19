/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Windows.Controls;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCCodeModel;

namespace QtVsTools.Wizards.Util
{
    using VisualStudio;

    internal abstract class VCLanguageManagerValidationRule : ValidationRule
    {
        protected VCLanguageManagerValidationRule()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ValidatesOnTargetUpdated = true;

            if (VsServiceProvider.GetService<DTE>() is {} dte)
                Vclm = dte.GetObject("VCLanguageManager") as VCLanguageManager;
        }

        public string FileExt { get; set; }
        protected VCLanguageManager Vclm { get; }
    }
}
