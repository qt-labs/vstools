/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;

namespace QtVsTools.Wizards.Util
{
    using Core;
    using Core.MsBuild;
    using VisualStudio;

    internal class FileExistsinFilterValidationRule : VCLanguageManagerValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string @string) {
                var dte = VsServiceProvider.GetService<SDTE, DTE>();
                if (dte == null)
                    return ValidationResult.ValidResult;

                var project = HelperFunctions.GetSelectedProject(dte);
                if (project == null)
                    return ValidationResult.ValidResult;

                var files = HelperFunctions.GetProjectFiles(project, Filter);
                if (files.Count == 0)
                    return ValidationResult.ValidResult;

                var fileName = @string.ToUpperInvariant();
                if (files.FirstOrDefault(x => x.ToUpperInvariant() == fileName) != null)
                    return new ValidationResult(false, @"File already exists.");
                return ValidationResult.ValidResult;
            }
            return new ValidationResult(false, @"Invalid file name.");
        }

        public FilesToList Filter { get; set; }
    }
}
