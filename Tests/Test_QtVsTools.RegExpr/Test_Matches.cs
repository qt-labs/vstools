/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.RegExpr
{
    using SyntaxAnalysis;
    using static SyntaxAnalysis.RegExpr;

    using Property = KeyValuePair<string, string>;

    [TestClass]
    public class Test_Matches
    {
        Parser propertyParser;

        [TestInitialize]
        public void GenerateParser()
        {
            var namePattern = (~CharSet['=', '\r', '\n']).Repeat(atLeast: 1);
            var nameToken = new Token("name", namePattern);

            var valuePattern = (~CharSet['\r', '\n']).Repeat(atLeast: 1);
            var valueToken = new Token("value", valuePattern);

            var propertyPattern
                = StartOfLine & nameToken & "=" & valueToken & (LineBreak | EndOfFile);

            var propertyToken = new Token("property", propertyPattern)
            {
                new Rule<Property>
                {
                    Create("name", (string s) => new Property(s, string.Empty)),
                    Transform("value", (Property p, string s) => new Property(p.Key, s))
                }
            };
            propertyParser = propertyToken.Render();
        }

        [TestMethod]
        public void MultipleMatches()
        {
            string propertiesText = @"
VSCMD_ARG_app_plat=Desktop
VSCMD_ARG_HOST_ARCH=x64
VSCMD_ARG_TGT_ARCH=x64
VSCMD_VER=16.11.17";

            IEnumerable<Property> properties = propertyParser
                .Parse(propertiesText)
                .GetValues<Property>("property");
            Assert.IsTrue(properties.Count() == 4);
        }
    }
}
