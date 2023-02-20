/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QtVsTools.SyntaxAnalysis;

namespace QtVsTest.Macros
{
    using static RegExpr;

    class MacroLines : IEnumerable<MacroLine>
    {
        readonly List<MacroLine> Lines = new List<MacroLine>();

        public void Add(MacroLine line) { Lines.Add(line); }

        public IEnumerator<MacroLine> GetEnumerator() { return Lines.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return Lines.GetEnumerator(); }
    }

    public class MacroLine
    { }

    public enum StatementType
    {
        Unknown,

        // Define reusable macro
        //# macro <macro name>
        Macro,

        // Switch to thread
        //# thread <ui | default>
        Thread,

        // Add reference to assembly and (opt.) namespaces in that assembly
        //# ref <assembly name> [namespace] [namespace] ...
        Reference,
        Ref = Reference,

        // Add reference to namespace
        //# using <namespace>
        Using,

        // Declare global variable, shared by called/calling macros
        //# var <type> <name> [ => <initial value> ]
        Var,

        // Get Visual Studio SDK service and assign it to local variable
        //# service <var name> <service interface> [service type]
        Service,

        // Call macro
        //# call <macro>
        Call,

        // Wait until expression evaluation returns non-default value
        // Optionally assign evaluated value to variable
        //# wait [timeout] [ <var type> <var name> ] => <expr>
        Wait,

        // UI automation command
        //
        // Set context based on UI element name path
        //# ui context [ VS ] => _string_ [, _string_, ... ]
        //
        // Set context based on window handle
        //# ui context HWND => _int_
        //
        // Get reference to UI element pattern. By default, the current context is used as source.
        // A name path relative to the current context allows using a child element as source.
        //# ui pattern <_TypeName_> <_VarName_> [ => _string_ [, _string_, ... ] ]
        //
        // Get reference to UI element Invoke pattern and immediately call the Invoke() method.
        //# ui pattern Invoke [ => _string_ [, _string_, ... ] ]
        //
        // Get reference to UI element Toggle pattern and immediately call the Toggle() method.
        //# ui pattern Toggle [ => _string_ [, _string_, ... ] ]
        Ui,

        // Close Visual Studio
        //# quit
        Quit
    }

    public class Statement : MacroLine
    {
        public StatementType Type { get; set; }
        public List<string> Args { get; set; }
        public string Code { get; set; }
    }

    public class CodeLine : MacroLine
    {
        public readonly string Code;
        public CodeLine(string code)
        {
            Code = code;
        }
    }

    class MacroParser
    {
        Parser MacroTextParser;
        Token TokenMacro;

        public static MacroParser Get()
        {
            var _this = new MacroParser();
            return _this.Initialize() ? _this : null;
        }

        enum TokenId
        {
            Macro,
            Code,
            Statement,
            StatementType,
            StatementArg,
            StatementArgValue,
            StatementCode,
            StatementCodeValue,
        };

        bool Initialize()
        {
            var charEsc = Char['\\'];
            var charQuot = Char['\"'];
            var stmtBegin = new Token("//#");
            var codeBegin = new Token("=>");

            var stmtType = new Token(TokenId.StatementType,
                CharWord.Repeat(atLeast: 1));

            var quotedArgValue = new Token(TokenId.StatementArgValue, SkipWs_Disable,
                (charEsc & charQuot | CharSet[~(charQuot + CharCr + CharLf)]).Repeat(atLeast: 1));

            var quotedArg = new Token(TokenId.StatementArg,
                charQuot & quotedArgValue & charQuot.Optional())
            {
                new Rule<string>
                {
                    Create(TokenId.StatementArgValue, (string value) => value )
                }
            };

            var unquotedArg = new Token(TokenId.StatementArg,
                !LookAhead[charQuot] & CharSet[~CharSpace].Repeat(atLeast: 1));

            var stmtArg = !LookAhead[codeBegin] & (quotedArg | unquotedArg);

            var stmtCodeValue = new Token(TokenId.StatementCodeValue,
                CharSet[~(CharCr + CharLf)].Repeat());

            var stmtCode = new Token(TokenId.StatementCode,
                codeBegin & stmtCodeValue)
            {
                new Rule<string>
                {
                    Create(TokenId.StatementCodeValue, (string value) => value )
                }
            };

            var stmtLine = StartOfLine &
                new Token(TokenId.Statement, SkipWs_Disable,
                    //  '//#'    <type>     [ <arg>... ]   [  '=>'       <code>  ]
                    stmtBegin & stmtType & stmtArg.Repeat() & stmtCode.Optional())
                {
                    new Rule<Statement>
                    {
                        Capture(value => new Statement { Args = new List<string>() }),
                        Update(TokenId.StatementType, (Statement s, string typeStr) =>
                        {
                            StatementType type;
                            if (Enum.TryParse(typeStr, ignoreCase: true, result: out type))
                                s.Type = type;
                            else
                                s.Type = StatementType.Unknown;
                        }),
                        Update(TokenId.StatementArg, (Statement s, string arg) => s.Args.Add(arg)),
                        Update(TokenId.StatementCode, (Statement s, string code) => s.Code = code)
                    }
                }
                & SkipWs & (LineBreak | EndOfFile);

            var codeLine = StartOfLine &
                new Token(TokenId.Code, SkipWs_Disable,
                    !LookAhead[stmtBegin & stmtType] & CharSet[~(CharCr + CharLf)].Repeat())
                {
                    new Rule<CodeLine>
                    {
                        Capture((string value) => new CodeLine(value))
                    }
                }
                & (LineBreak | EndOfFile);

            TokenMacro = new Token(TokenId.Macro, SkipWs_Disable,
                (stmtLine | codeLine).Repeat() & EndOfFile)
            {
                new Rule<MacroLines>
                {
                    Update(TokenId.Statement, (MacroLines m, Statement s) => m.Add(s)),
                    Update(TokenId.Code,      (MacroLines m, CodeLine l)  => m.Add(l))
                }
            };

            var parser = TokenMacro.Render(defaultTokenWs: CharHorizSpace.Repeat());
            if (parser == null)
                return false;

            MacroTextParser = parser;
            return true;
        }

        public MacroLines Parse(string macroText)
        {
            return MacroTextParser.Parse(macroText)
                .GetValues<MacroLines>(TokenMacro)
                .FirstOrDefault();
        }
    }
}
