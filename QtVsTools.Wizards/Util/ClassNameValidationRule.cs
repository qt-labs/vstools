/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
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

                if (Vclm != null && !Vclm.IsReservedName(identifier)
                    && Regex.IsMatch(identifier, pattern)) {
                    return ValidationResult.ValidResult;
                }
            }
            return new ValidationResult(false, @"Invalid identifier.");
        }

        public bool SupportNamespaces { get; set; }
        public bool AllowEmptyIdentifier { get; set; }

        const string pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";

        public static string SafeName(string safeName, string @default)
        {
            safeName = Regex.Replace(safeName, "[^a-zA-Z0-9_]", string.Empty);
            safeName = Regex.Replace(safeName, @"^[\d-]*\s*", string.Empty);
            var result = new ClassNameValidationRule().Validate(safeName, null);
            if (result != ValidationResult.ValidResult)
                safeName = @default;
            return safeName;
        }
    }
}
