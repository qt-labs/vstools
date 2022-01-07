/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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
            if (value is string) {
                var identifier = value as string;
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
