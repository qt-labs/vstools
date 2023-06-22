/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Wizards.Util
{
    using Core;
    using VisualStudio;

    internal class FileExistsInFilterValidationRule : VCLanguageManagerValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (value is not string @string)
                return new ValidationResult(false, @"Invalid file name.");

            if (GetSelectedDteProject() is not {} project)
                return ValidationResult.ValidResult;

            var files = HelperFunctions.GetProjectFiles(project, Filter);
            if (files.Count == 0)
                return ValidationResult.ValidResult;

            var fileName = @string.ToUpperInvariant();
            return files.FirstOrDefault(x => x.ToUpperInvariant() == fileName) == null
                ? ValidationResult.ValidResult
                : new ValidationResult(false, @"File already exists.");
        }

        public FilesToList Filter { get; set; }

        private static Project GetSelectedDteProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (VsServiceProvider.GetService<SDTE, DTE>() is not {} dte)
                return null;

            try {
                if (dte.ActiveSolutionProjects is not Array projects || projects.Length == 0)
                    return null;
                return projects.GetValue(0) as Project;
            } catch (Exception exception) {
                exception.Log();
            }
            return null;
        }
    }
}
