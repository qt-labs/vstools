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

using System.Collections.Generic;
using System.Globalization;

namespace QtVsTools
{
    public class TranslationItem : CultureInfo
    {
        public TranslationItem(int culture)
            : base(culture)
        {
        }

        public override string ToString()
        {
            if (NativeName != DisplayName)
                return DisplayName;

            var culture = GetCultureInfo(Vsix.Instance.Dte.LocaleID);
            if (culture.TwoLetterISOLanguageName == TwoLetterISOLanguageName)
                return DisplayName;

            return EnglishName;
        }

        public static TranslationItem SystemLanguage()
        {
            return new TranslationItem(CurrentCulture.LCID);
        }

        public static TranslationItem[] GetTranslationItems()
        {
            var cultures = GetCultures(CultureTypes.SpecificCultures
                                       & ~CultureTypes.UserCustomCulture & ~CultureTypes.ReplacementCultures);
            var transItems = new List<TranslationItem>();
            for (var i = 0; i < cultures.Length; i++) {
                // Locales without a LCID are given LCID 0x1000 (http://msdn.microsoft.com/en-us/library/dn363603.aspx)
                // Trying to create a TranslationItem for these will cause an exception to be thrown.
                var lcid = cultures[i].LCID;
                if (lcid != 0x1000)
                    transItems.Add(new TranslationItem(lcid));
            }
            return transItems.ToArray();
        }
    }
}
