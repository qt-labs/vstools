/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace QtVsTools.Wizards.Util
{
    internal class ClassNameValidationRule : VCLanguageManagerValidationRule
    {
        public ClassNameValidationRule()
        {
            SupportNamespaces = false;
            AllowEmptyIdentifier = false;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string identifier) {
                if (AllowEmptyIdentifier && string.IsNullOrEmpty(identifier))
                    return ValidationResult.ValidResult;

                if (SupportNamespaces) {
                    var index = identifier.LastIndexOf(@"::", System.StringComparison.Ordinal);
                    if (index >= 0)
                        identifier = identifier.Substring(index + 2);
                }

                if (Regex.IsMatch(identifier, pattern) && !Vclm.IsReservedName(identifier))
                    return ValidationResult.ValidResult;
            }
            return new ValidationResult(false, @"Invalid identifier.");
        }

        public bool SupportNamespaces { get; set; }
        public bool AllowEmptyIdentifier { get; set; }

        const string pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";
    }
}
