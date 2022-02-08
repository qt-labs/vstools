/****************************************************************************
**
** Copyright (C) 2018 The Qt Company Ltd.
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

/// This file contains definitions of basic QML syntax elements.

namespace QtVsTools.Qml.Syntax
{
    /// <summary>
    /// Reference to a location in the source code parsed by the QML parser.
    /// Maps one-to-one with the Span concept in the Visual Studio SDK.
    /// </summary>
    public struct SourceLocation
    {
        public int Offset;
        public int Length;
    }

    /// <summary>
    /// Token constants from the Qt Declarative parser.
    /// </summary>
    public enum TokenKind
    {
        #region Copied from qqmljsgrammar_p.h
        EOF_SYMBOL = 0,
        REDUCE_HERE = 125,
        T_AND = 1,
        T_AND_AND = 2,
        T_AND_EQ = 3,
        T_ARROW = 93,
        T_AS = 110,
        T_AUTOMATIC_SEMICOLON = 62,
        T_BREAK = 4,
        T_CASE = 5,
        T_CATCH = 6,
        T_CLASS = 98,
        T_COLON = 7,
        T_COMMA = 8,
        T_COMMENT = 91,
        T_COMPATIBILITY_SEMICOLON = 92,
        T_CONST = 86,
        T_CONTINUE = 9,
        T_DEBUGGER = 88,
        T_DEFAULT = 10,
        T_DELETE = 11,
        T_DIVIDE_ = 12,
        T_DIVIDE_EQ = 13,
        T_DO = 14,
        T_DOT = 15,
        T_ELLIPSIS = 95,
        T_ELSE = 16,
        T_ENUM = 94,
        T_EQ = 17,
        T_EQ_EQ = 18,
        T_EQ_EQ_EQ = 19,
        T_ERROR = 114,
        T_EXPORT = 101,
        T_EXTENDS = 99,
        T_FALSE = 85,
        T_FEED_JS_EXPRESSION = 118,
        T_FEED_JS_MODULE = 120,
        T_FEED_JS_SCRIPT = 119,
        T_FEED_JS_STATEMENT = 117,
        T_FEED_UI_OBJECT_MEMBER = 116,
        T_FEED_UI_PROGRAM = 115,
        T_FINALLY = 20,
        T_FOR = 21,
        T_FORCE_BLOCK = 122,
        T_FORCE_DECLARATION = 121,
        T_FOR_LOOKAHEAD_OK = 123,
        T_FROM = 102,
        T_FUNCTION = 22,
        T_GE = 23,
        T_GET = 112,
        T_GT = 24,
        T_GT_GT = 25,
        T_GT_GT_EQ = 26,
        T_GT_GT_GT = 27,
        T_GT_GT_GT_EQ = 28,
        T_IDENTIFIER = 29,
        T_IF = 30,
        T_IMPORT = 108,
        T_IN = 31,
        T_INSTANCEOF = 32,
        T_LBRACE = 33,
        T_LBRACKET = 34,
        T_LE = 35,
        T_LET = 87,
        T_LPAREN = 36,
        T_LT = 37,
        T_LT_LT = 38,
        T_LT_LT_EQ = 39,
        T_MINUS = 40,
        T_MINUS_EQ = 41,
        T_MINUS_MINUS = 42,
        T_MULTILINE_STRING_LITERAL = 90,
        T_NEW = 43,
        T_NOT = 44,
        T_NOT_EQ = 45,
        T_NOT_EQ_EQ = 46,
        T_NO_SUBSTITUTION_TEMPLATE = 103,
        T_NULL = 83,
        T_NUMERIC_LITERAL = 47,
        T_OF = 111,
        T_ON = 124,
        T_OR = 48,
        T_OR_EQ = 49,
        T_OR_OR = 50,
        T_PLUS = 51,
        T_PLUS_EQ = 52,
        T_PLUS_PLUS = 53,
        T_PRAGMA = 109,
        T_PROPERTY = 68,
        T_PUBLIC = 107,
        T_QUESTION = 54,
        T_RBRACE = 55,
        T_RBRACKET = 56,
        T_READONLY = 70,
        T_REMAINDER = 57,
        T_REMAINDER_EQ = 58,
        T_RESERVED_WORD = 89,
        T_RETURN = 59,
        T_RPAREN = 60,
        T_SEMICOLON = 61,
        T_SET = 113,
        T_SIGNAL = 69,
        T_STAR = 63,
        T_STAR_EQ = 66,
        T_STAR_STAR = 64,
        T_STAR_STAR_EQ = 65,
        T_STATIC = 100,
        T_STRING_LITERAL = 67,
        T_SUPER = 97,
        T_SWITCH = 71,
        T_TEMPLATE_HEAD = 104,
        T_TEMPLATE_MIDDLE = 105,
        T_TEMPLATE_TAIL = 106,
        T_THIS = 72,
        T_THROW = 73,
        T_TILDE = 74,
        T_TRUE = 84,
        T_TRY = 75,
        T_TYPEOF = 76,
        T_VAR = 77,
        T_VOID = 78,
        T_WHILE = 79,
        T_WITH = 80,
        T_XOR = 81,
        T_XOR_EQ = 82,
        T_YIELD = 96,

        ACCEPT_STATE = 1008,
        RULE_COUNT = 586,
        STATE_COUNT = 1009,
        TERMINAL_COUNT = 126,
        NON_TERMINAL_COUNT = 213,

        GOTO_INDEX_OFFSET = 1009,
        GOTO_INFO_OFFSET = 6012,
        GOTO_CHECK_OFFSET = 6012
        #endregion
    }

    public abstract class SyntaxElement { }

    /// <summary>
    /// Represents a token identified by the QML lexer
    /// </summary>
    public class Token : SyntaxElement
    {
        private TokenKind Kind { get; set; }
        public SourceLocation Location { get; private set; }
        protected Token() { }
        public static Token Create(TokenKind kind, int offset, int length)
        {
            return Create(kind, new SourceLocation
            {
                Offset = offset,
                Length = length,
            });
        }

        public static Token Create(TokenKind kind, SourceLocation location)
        {
            Token token;
            switch (kind) {
            #region case KEYWORD:
            case TokenKind.T_AS:
            case TokenKind.T_BREAK:
            case TokenKind.T_CASE:
            case TokenKind.T_CATCH:
            case TokenKind.T_CONST:
            case TokenKind.T_CONTINUE:
            case TokenKind.T_DEFAULT:
            case TokenKind.T_DELETE:
            case TokenKind.T_DO:
            case TokenKind.T_ELSE:
            case TokenKind.T_ENUM:
            case TokenKind.T_FALSE:
            case TokenKind.T_FINALLY:
            case TokenKind.T_FOR:
            case TokenKind.T_FUNCTION:
            case TokenKind.T_IF:
            case TokenKind.T_IMPORT:
            case TokenKind.T_IN:
            case TokenKind.T_INSTANCEOF:
            case TokenKind.T_LET:
            case TokenKind.T_NEW:
            case TokenKind.T_NULL:
            case TokenKind.T_ON:
            case TokenKind.T_PRAGMA:
            case TokenKind.T_PROPERTY:
            case TokenKind.T_PUBLIC:
            case TokenKind.T_READONLY:
            case TokenKind.T_RESERVED_WORD:
            case TokenKind.T_RETURN:
            case TokenKind.T_SET:
            case TokenKind.T_SIGNAL:
            case TokenKind.T_SWITCH:
            case TokenKind.T_THIS:
            case TokenKind.T_THROW:
            case TokenKind.T_TRUE:
            case TokenKind.T_TRY:
            case TokenKind.T_TYPEOF:
            case TokenKind.T_VAR:
            case TokenKind.T_VOID:
            case TokenKind.T_WHILE:
            case TokenKind.T_WITH:
                #endregion
                token = new KeywordToken();
                break;
            case TokenKind.T_NUMERIC_LITERAL:
                token = new NumberToken();
                break;
            case TokenKind.T_MULTILINE_STRING_LITERAL:
            case TokenKind.T_STRING_LITERAL:
                token = new StringToken();
                break;
            case TokenKind.T_COMMENT:
                token = new CommentToken();
                break;
            default:
                token = new Token();
                break;
            }
            token.Kind = kind;
            token.Location = location;
            return token;
        }
    }

    public class KeywordToken : Token { }

    public class NumberToken : Token { }

    public class StringToken : Token { }

    public class CommentToken : Token { }
}
