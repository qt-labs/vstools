/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace QtVsTools.Wizards.Util
{
    internal class FileNameValidationRule : VCLanguageManagerValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string { Length: > 0 } filename) {
                if (!string.IsNullOrEmpty(Path.GetDirectoryName(filename)))
                    filename = Path.GetFileName(filename);

                if (FileExt is ".ui" or ".qrc") {
                    filename = filename.ToLower().Replace(".h", ".x");
                    filename = filename.Replace(FileExt, ".h");
                }

                if (Vclm.ValidateFileName(filename)
                    && !Path.GetInvalidFileNameChars().Any(filename.Contains)) {
                    return ValidationResult.ValidResult;
                }
            }
            return new ValidationResult(false, "Invalid file name.");
        }
    }
}
