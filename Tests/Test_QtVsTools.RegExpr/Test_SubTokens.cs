/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.RegExpr
{
    using static SyntaxAnalysis.RegExpr;

    [TestClass]
    public class Test_SubTokens
    {
        [TestMethod]
        public void TestManyToMany()
        {
            var tokenA = new Token("A", "a");
            var tokenB = new Token("B", "b" & tokenA);
            var tokenC = new Token("C", "c" & tokenB);
            var tokenX = new Token("X", (tokenA | tokenB | tokenC).Repeat());
            var parser = tokenX.Render();
            parser.Parse("abacba");
        }

        [TestMethod]
        public void TestLookAhead()
        {
            var tokenA = new Token("A", "a");
            var tokenB = new Token("B", "b" & !LookAhead[tokenA] & AnyChar);
            var tokenX = new Token("X", (tokenA | tokenB).Repeat());
            var parser = tokenX.Render();
            parser.Parse("abc");
        }
    }
}
