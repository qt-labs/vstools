/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

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
        #region Copied from qqmljsgrammar_p.h: enum QQmlJSGrammar::VariousConstants
        EOF_SYMBOL = 0,
        REDUCE_HERE = 131,
        T_AND = 1,
        T_AND_AND = 2,
        T_AND_EQ = 3,
        T_ARROW = 96,
        T_AS = 116,
        T_AT = 90,
        T_AUTOMATIC_SEMICOLON = 64,
        T_BREAK = 4,
        T_CASE = 5,
        T_CATCH = 6,
        T_CLASS = 102,
        T_COLON = 7,
        T_COMMA = 8,
        T_COMMENT = 94,
        T_COMPATIBILITY_SEMICOLON = 95,
        T_COMPONENT = 108,
        T_CONST = 88,
        T_CONTINUE = 9,
        T_DEBUGGER = 91,
        T_DEFAULT = 10,
        T_DELETE = 11,
        T_DIVIDE_ = 12,
        T_DIVIDE_EQ = 13,
        T_DO = 14,
        T_DOT = 15,
        T_ELLIPSIS = 99,
        T_ELSE = 16,
        T_ENUM = 98,
        T_EQ = 17,
        T_EQ_EQ = 18,
        T_EQ_EQ_EQ = 19,
        T_ERROR = 120,
        T_EXPORT = 105,
        T_EXTENDS = 103,
        T_FALSE = 87,
        T_FEED_JS_EXPRESSION = 124,
        T_FEED_JS_MODULE = 126,
        T_FEED_JS_SCRIPT = 125,
        T_FEED_JS_STATEMENT = 123,
        T_FEED_UI_OBJECT_MEMBER = 122,
        T_FEED_UI_PROGRAM = 121,
        T_FINALLY = 20,
        T_FOR = 21,
        T_FORCE_BLOCK = 128,
        T_FORCE_DECLARATION = 127,
        T_FOR_LOOKAHEAD_OK = 129,
        T_FROM = 106,
        T_FUNCTION = 23,
        T_FUNCTION_STAR = 22,
        T_GE = 24,
        T_GET = 118,
        T_GT = 25,
        T_GT_GT = 26,
        T_GT_GT_EQ = 27,
        T_GT_GT_GT = 28,
        T_GT_GT_GT_EQ = 29,
        T_IDENTIFIER = 30,
        T_IF = 31,
        T_IMPORT = 114,
        T_IN = 32,
        T_INSTANCEOF = 33,
        T_LBRACE = 34,
        T_LBRACKET = 35,
        T_LE = 36,
        T_LET = 89,
        T_LPAREN = 37,
        T_LT = 38,
        T_LT_LT = 39,
        T_LT_LT_EQ = 40,
        T_MINUS = 41,
        T_MINUS_EQ = 42,
        T_MINUS_MINUS = 43,
        T_MULTILINE_STRING_LITERAL = 93,
        T_NEW = 44,
        T_NOT = 45,
        T_NOT_EQ = 46,
        T_NOT_EQ_EQ = 47,
        T_NO_SUBSTITUTION_TEMPLATE = 109,
        T_NULL = 85,
        T_NUMERIC_LITERAL = 48,
        T_OF = 117,
        T_ON = 130,
        T_OR = 49,
        T_OR_EQ = 51,
        T_OR_OR = 52,
        T_PLUS = 53,
        T_PLUS_EQ = 54,
        T_PLUS_PLUS = 55,
        T_PRAGMA = 115,
        T_PROPERTY = 70,
        T_PUBLIC = 113,
        T_QUESTION = 56,
        T_QUESTION_QUESTION = 97,
        T_RBRACE = 57,
        T_RBRACKET = 58,
        T_READONLY = 72,
        T_REMAINDER = 59,
        T_REMAINDER_EQ = 60,
        T_REQUIRED = 107,
        T_RESERVED_WORD = 92,
        T_RETURN = 61,
        T_RPAREN = 62,
        T_SEMICOLON = 63,
        T_SET = 119,
        T_SIGNAL = 71,
        T_STAR = 65,
        T_STAR_EQ = 68,
        T_STAR_STAR = 66,
        T_STAR_STAR_EQ = 67,
        T_STATIC = 104,
        T_STRING_LITERAL = 69,
        T_SUPER = 101,
        T_SWITCH = 73,
        T_TEMPLATE_HEAD = 110,
        T_TEMPLATE_MIDDLE = 111,
        T_TEMPLATE_TAIL = 112,
        T_THEN = 132,
        T_THIS = 74,
        T_THROW = 75,
        T_TILDE = 76,
        T_TRUE = 86,
        T_TRY = 77,
        T_TYPEOF = 78,
        T_VAR = 79,
        T_VERSION_NUMBER = 50,
        T_VOID = 80,
        T_WHILE = 81,
        T_WITH = 82,
        T_WITHOUTAS = 133,
        T_XOR = 83,
        T_XOR_EQ = 84,
        T_YIELD = 100,

        ACCEPT_STATE = 1098,
        RULE_COUNT = 616,
        STATE_COUNT = 1099,
        TERMINAL_COUNT = 134,
        NON_TERMINAL_COUNT = 238,

        GOTO_INDEX_OFFSET = 1099,
        GOTO_INFO_OFFSET = 7238,
        GOTO_CHECK_OFFSET = 7238
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
                Length = length
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
            case TokenKind.T_REQUIRED:
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
