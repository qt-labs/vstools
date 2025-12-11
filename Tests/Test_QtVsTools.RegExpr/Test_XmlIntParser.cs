// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.RegExpr
{
    using static SyntaxAnalysis.RegExpr;

    [TestClass]
    public class Test_XmlIntParser
    {
        private const string IdNum = "NUM";
        private const string IdExpr = "EXPR";
        private const string IdExprLPar = "EXPR_LPAR";
        private const string IdTagValue = "TAG_VALUE";
        private const string IdTagBegin = "TAG_BEGIN";
        private const string IdTagName = "TAG_NAME";
        private const string IdTag = "TAG";

        private static Parser GetParser()
        {
            // XML chars
            var charLt = Char['<'];
            var charGt = Char['>'];
            var charSlash = Char['/'];

            // int expr chars
            var charPlus = Char['+'];
            var charMinus = Char['-'];
            var charMult = Char['*'];
            var charDiv = Char['/'];
            var charLPar = Char['('];
            var charRPar = Char[')'];
            var charDigit = Char['0', '9'];
            var charAddtOper = CharSet[charPlus + charMinus];
            var charMultOper = CharSet[charMult + charDiv];
            var charOper = CharSet[charAddtOper + charMultOper];

            // int operator priorities
            const int priorityInfixAddt = 10;
            const int priorityInfixMult = 20;
            const int priorityPrefixAddt = 30;

            // token: number
            var exprNum = new Token(IdNum,
                charDigit.Repeat() & !LookAhead[SkipWs & (charDigit | charLPar)])
            {
                new Rule<int>
                {
                    Capture(int.Parse)
                }
            };

            // token: plus (infix or prefix)
            var exprPlus = new Token(IdExpr,
                charPlus & !LookAhead[SkipWs & (charOper | charRPar | charLt)])
            {
                new PrefixRule<int, int>(
                    priority: priorityPrefixAddt,
                    select: t => (t.IsFirst || t.LookBehind().First().Is(IdExprLPar))
                            && t.LookAhead().First().Is(IdNum, IdExprLPar))
                {
                    Create((int x) => +x)
                },

                new InfixRule<int, int, int>(priority: priorityInfixAddt)
                {
                    Create((int x, int y) => x + y)
                }
            };

            // token: minus (infix or prefix)
            var exprMinus = new Token(IdExpr,
                charMinus & !LookAhead[SkipWs & (charOper | charRPar | charLt)])
            {
                new PrefixRule<int, int>(
                    priority: priorityPrefixAddt,
                    select: t => (t.IsFirst || t.LookBehind().First().Is(IdExprLPar))
                            && t.LookAhead().First().Is(IdNum, IdExprLPar))
                {
                    Create((int x) => -x)
                },

                new InfixRule<int, int, int>(priority: priorityInfixAddt)
                {
                    Create((int x, int y) => x - y)
                }
            };

            // token: multiplication
            var exprMult = new Token(IdExpr,
                charMult & !LookAhead[SkipWs & (charOper | charRPar | charLt)])
            {
                new InfixRule<int, int, int>(priority: priorityInfixMult)
                {
                    Create((int x, int y) => x * y)
                }
            };

            // token: division
            var exprDiv = new Token(IdExpr,
                charDiv & !LookAhead[SkipWs & (charOper | charRPar | charLt)])
            {
                new InfixRule<int, int, int>(priority: priorityInfixMult)
                {
                    Create((int x, int y) => x / y)
                }
            };

            // token: left parenthesis
            var exprLPar = new Token(IdExprLPar,
                charLPar & !LookAhead[SkipWs & (charRPar | charLt)])
            {
                new LeftDelimiterRule<string>
                {
                    Capture(value => value)
                }
            };

            // token: right parenthesis
            var exprRPar = new Token(IdExpr,
                charRPar & !LookAhead[SkipWs & (charDigit | charLPar)])
            {
                new RightDelimiterRule<string, int, int>
                {
                    Create((string lPar, int n) => n)
                }
            };

            // int expression
            var numExpr = (exprNum
                        | exprPlus
                        | exprMinus
                        | exprMult
                        | exprDiv
                        | exprLPar
                        | exprRPar).Repeat();

            // token: tag value containing int expression
            var tagValue = new Token(IdTagValue, SkipWs_Disable,
                LookAhead[SkipWs & CharSet[~(CharSpace + charLt)]] & numExpr & LookAhead[charLt])
            {
                new Rule<string>
                {
                    Create(IdNum, (int expr) => "=" + expr),
                    Create(IdExpr, (int expr) => "=" + expr)
                }
            };

            // token: tag open (only tag name, no attribs)
            var tagBegin = new Token(IdTagBegin,
                charLt & new Token(IdTagName, CharWord.Repeat()) & charGt)
            {
                new LeftDelimiterRule<string>
                {
                    Create(IdTagName, (string tagName) => tagName)
                }
            };

            // token: tag close
            var tagEnd = new Token(IdTag,
                charLt & charSlash & new Token(IdTagName, CharWord.Repeat()) & LookAhead[charGt])
            {
                new RightDelimiterRule<string, string, string>
                {
                    Create(IdTagName, (string name) => name),
                    Error(
                        (string tag, string tagName) => tagName != tag,
                        (tag, tagName) => $"Expected {tagName}, found {tag}"),
                    Create(
                        (string tag, string value) => value.StartsWith("="),
                        (tag, value) => tag + value),
                    Create(
                        (string tag, string value) => !value.StartsWith("="),
                        (tag, value) => tag + ":{" + value + "}")
                }
            };

            // token: tag sequence
            var tagConcat = new Token(IdTag, charGt & LookAhead[SkipWs & charLt & ~charSlash])
            {
                new InfixRule<string, string, string>(
                    pre: t => t.LeftOperand.Is(IdTag) && t.RightOperand.Is(IdTag))
                {
                    Create((string leftTag, string rightTag) => leftTag + "," + rightTag)
                }
            };

            // XML containing int expressions
            var xmlInt = StartOfLine
                & (tagBegin | tagValue | tagEnd & (tagConcat | charGt)).Repeat()
                & SkipWs & EndOfFile;

            // generate RegExpr parser
            return xmlInt.Render(CharSpace.Repeat());
        }

        private readonly Parser parser = GetParser();

        [TestMethod]
        public void TestConst()
        {
            const string testInput = "<x>42</x>";
            const string testOutput = "x=42";
            Debug.Assert(parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }

        [TestMethod]
        public void TestConstError()
        {
            var testInput = "<x>foo</x>";
            Assert.ThrowsExactly<ParseErrorException>(() => parser.Parse(testInput));
        }

        [TestMethod]
        public void TestInfix()
        {
            const string testInput = "<x>2 - 1</x>";
            const string testOutput = "x=1";
            Debug.Assert(parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }

        [TestMethod]
        public void TestInfixError()
        {
            const string testInput = "<x>2 - </x>";
            Assert.ThrowsExactly<ParseErrorException>(() => parser.Parse(testInput));
        }

        [TestMethod]
        public void TestPrefix()
        {
            const string testInput = "<x>-2 + 1</x>";
            const string testOutput = "x=-1";
            Debug.Assert(parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }
        [TestMethod]
        public void TestPrefixError()
        {
            var testInput = "<x>- + 1</x>";
            Assert.ThrowsExactly<ParseErrorException>(() => parser.Parse(testInput));
        }

        [TestMethod]
        public void TestPrecedence()
        {
            const string testInput = "<x>2 + 3 * 4</x>";
            const string testOutput = "x=14";
            Debug.Assert(parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }

        [TestMethod]
        public void TestParentheses()
        {
            const string testInput = "<x>(2 + 3) * 4</x>";
            const string testOutput = "x=20";
            Debug.Assert(parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }

        [TestMethod]
        public void TestParenthesesLeftError()
        {
            const string testInput = "<x>2 + 3) * 4</x>";
            Assert.ThrowsExactly<ParseErrorException>(() => parser.Parse(testInput));
        }

        [TestMethod]
        public void TestParenthesesRightError()
        {
            const string testInput = "<x>(2 + 3 * 4</x>";
            Assert.ThrowsExactly<ParseErrorException>(() => parser.Parse(testInput));
        }

        [TestMethod]
        public void TestParenthesesNested()
        {
            const string testInput = "<x>(-((2 + 3) * 4) / 5) * 3</x>";
            const string testOutput = "x=-12";
            Debug.Assert(parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }

        [TestMethod]
        public void TestNestedTags()
        {
            const string testInput = "<a><x>(-((2 + 3) * 4) / 5) * 3</x><y>(2 + 3) * 4</y></a>";
            const string testOutput = "a:{x=-12,y=20}";
            Debug.Assert(parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }

        [TestMethod]
        public void TestNestedTagsError()
        {
            const string testInput = "<a><x>1</x><y>2<z><w>";
            Assert.ThrowsExactly<ParseErrorException>(() => parser.Parse(testInput));
        }

        [TestMethod]
        public void TestMultiLines()
        {
            const string testInput = "<a>" + "\r\n" +
                "  <x>2 + 3 * 4</x>" + "\r\n" +
                "  <y>(2 + 3) * 4</y>" + "\r\n" +
                "</a>";
            const string testOutput = "a:{x=14,y=20}";
            Debug.Assert(parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }
    }
}
