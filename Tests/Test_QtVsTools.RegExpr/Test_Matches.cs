/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.RegExpr
{
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
