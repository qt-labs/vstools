/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.RegExpr
{
    using static SyntaxAnalysis.RegExpr;

    [TestClass]
    public class Test_XmlIntParser
    {
        public const string IdNum = "NUM";
        public const string IdExpr = "EXPR";
        public const string IdExprLPar = "EXPR_LPAR";
        public const string IdTagValue = "TAG_VALUE";
        public const string IdTagBegin = "TAG_BEGIN";
        public const string IdTagName = "TAG_NAME";
        public const string IdTag = "TAG";

        public static Parser GetParser()
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
            int priorityInfixAddt = 10;
            int priorityInfixMult = 20;
            int priorityPrefixAddt = 30;

            // token: number
            var exprNum = new Token(IdNum,
                charDigit.Repeat() & !LookAhead[SkipWs & (charDigit | charLPar)])
            {
                new Rule<int>
                {
                    Capture(value => int.Parse(value))
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
                new LeftDelimiterRule<string>()
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
                    Create(IdNum, (int expr) => "=" + expr.ToString()),
                    Create(IdExpr, (int expr) => "=" + expr.ToString())
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
                        (tag, tagName) => string.Format("Expected {0}, found {1}", tagName, tag)),
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

        readonly Parser Parser = GetParser();

        [TestMethod]
        public void TestConst()
        {
            string testInput = "<x>42</x>";
            string testOutput = "x=42";
            Debug.Assert(Parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }

        [TestMethod]
        [ExpectedException(typeof(ParseErrorException))]
        public void TestConstError()
        {
            string testInput = "<x>foo</x>";
            Parser.Parse(testInput);
        }

        [TestMethod]
        public void TestInfix()
        {
            string testInput = "<x>2 - 1</x>";
            string testOutput = "x=1";
            Debug.Assert(Parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }

        [TestMethod]
        [ExpectedException(typeof(ParseErrorException))]
        public void TestInfixError()
        {
            string testInput = "<x>2 - </x>";
            Parser.Parse(testInput);
        }

        [TestMethod]
        public void TestPrefix()
        {
            string testInput = "<x>-2 + 1</x>";
            string testOutput = "x=-1";
            Debug.Assert(Parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }
        [TestMethod]
        [ExpectedException(typeof(ParseErrorException))]
        public void TestPrefixError()
        {
            string testInput = "<x>- + 1</x>";
            Parser.Parse(testInput);
        }

        [TestMethod]
        public void TestPrecedence()
        {
            string testInput = "<x>2 + 3 * 4</x>";
            string testOutput = "x=14";
            Debug.Assert(Parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }

        [TestMethod]
        public void TestParentheses()
        {
            string testInput = "<x>(2 + 3) * 4</x>";
            string testOutput = "x=20";
            Debug.Assert(Parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }

        [TestMethod]
        [ExpectedException(typeof(ParseErrorException))]
        public void TestParenthesesLeftError()
        {
            string testInput = "<x>2 + 3) * 4</x>";
            Parser.Parse(testInput);
        }

        [TestMethod]
        [ExpectedException(typeof(ParseErrorException))]
        public void TestParenthesesRightError()
        {
            string testInput = "<x>(2 + 3 * 4</x>";
            Parser.Parse(testInput);
        }

        [TestMethod]
        public void TestParenthesesNested()
        {
            string testInput = "<x>(-((2 + 3) * 4) / 5) * 3</x>";
            string testOutput = "x=-12";
            Debug.Assert(Parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }

        [TestMethod]
        public void TestNestedTags()
        {
            string testInput = "<a><x>(-((2 + 3) * 4) / 5) * 3</x><y>(2 + 3) * 4</y></a>";
            string testOutput = "a:{x=-12,y=20}";
            Debug.Assert(Parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }

        [TestMethod]
        [ExpectedException(typeof(ParseErrorException))]
        public void TestNestedTagsError()
        {
            string testInput = "<a><x>1</x><y>2<z><w>";
            Parser.Parse(testInput);
        }

        [TestMethod]
        public void TestMultiLines()
        {
            string testInput =
                "<a>" + "\r\n" +
                "  <x>2 + 3 * 4</x>" + "\r\n" +
                "  <y>(2 + 3) * 4</y>" + "\r\n" +
                "</a>";
            string testOutput = "a:{x=14,y=20}";
            Debug.Assert(Parser.Parse(testInput).GetValues<string>(IdTag).First() == testOutput);
        }
    }
}
