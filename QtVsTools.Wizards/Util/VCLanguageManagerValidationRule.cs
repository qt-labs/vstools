/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCCodeModel;
using EnvDTE;

namespace QtVsTools.Wizards.Util
{
    using VisualStudio;

    internal abstract class VCLanguageManagerValidationRule : ValidationRule
    {
        protected VCLanguageManagerValidationRule()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ValidatesOnTargetUpdated = true;

            var dte = VsServiceProvider.GetService<DTE>();
            Vclm = dte.GetObject("VCLanguageManager") as VCLanguageManager;
        }

        public string FileExt { get; set; }
        public VCLanguageManager Vclm { get; }
    }
}
