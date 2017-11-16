/****************************************************************************
**
** Copyright (C) 2017 The Qt Company Ltd.
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace QtProjectLib.CommandLine
{
    public class Parser
    {

        List<Option> commandLineOptionList = new List<Option>();
        Dictionary<string, int> nameHash = new Dictionary<string, int>();
        Dictionary<int, List<string>> optionValuesHash = new Dictionary<int, List<string>>();
        List<string> optionNames = new List<string>();
        List<string> positionalArgumentList = new List<string>();
        List<string> unknownOptionNames = new List<string>();
        bool needsParsing = true;

        public enum SingleDashWordOptionMode
        {
            ParseAsCompactedShortOptions = 0,
            ParseAsLongOptions = 1
        }
        SingleDashWordOptionMode singleDashWordOptionMode = 0;

        public enum OptionsAfterPositionalArgumentsMode
        {
            ParseAsOptions = 0,
            ParseAsPositionalArguments = 1
        }
        OptionsAfterPositionalArgumentsMode optionsAfterPositionalArgumentsMode = 0;

        public Parser()
        {
        }

        public string ApplicationDescription
        {
            get;
            set;
        }

        public string ErrorText
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string HelpText
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<string> PositionalArguments
        {
            get
            {
                CheckParsed("PositionalArguments");
                return positionalArgumentList;
            }
        }

        public IEnumerable<string> OptionNames
        {
            get
            {
                CheckParsed("OptionNames");
                return optionNames;
            }
        }

        public IEnumerable<string> UnknownOptionNames
        {
            get
            {
                CheckParsed("UnknownOptionNames");
                return unknownOptionNames;
            }
        }

        IEnumerable<string> Aliases(string optionName)
        {
            int optionIndex;
            if (!nameHash.TryGetValue(optionName, out optionIndex)) {
                return new List<string>();
            }
            return commandLineOptionList[optionIndex].Names;
        }

        public void SetSingleDashWordOptionMode(SingleDashWordOptionMode singleDashWordOptionMode)
        {
            this.singleDashWordOptionMode = singleDashWordOptionMode;
        }

        public void SetOptionsAfterPositionalArgumentsMode(
            OptionsAfterPositionalArgumentsMode parsingMode)
        {
            this.optionsAfterPositionalArgumentsMode = parsingMode;
        }

        public bool AddOption(Option option)
        {
            if (option.Names.Any()) {
                foreach (var name in option.Names) {
                    if (nameHash.ContainsKey(name))
                        return false;
                }

                commandLineOptionList.Add(option);

                int offset = commandLineOptionList.Count() - 1;
                foreach (var name in option.Names)
                    nameHash.Add(name, offset);

                return true;
            }

            return false;
        }

        public bool AddOptions(IEnumerable<Option> options)
        {
            bool result = true;
            foreach (var option in options)
                result &= AddOption(option);

            return result;
        }

        public Option AddVersionOption()
        {
            Option opt = new Option(new[] { "v", "version" });
            AddOption(opt);
            return opt;
        }

        public Option AddHelpOption()
        {
            Option opt = new Option(new[] { "?", "h", "help" });
            AddOption(opt);
            return opt;
        }

        public void AddPositionalArgument(string name, string description, string syntax)
        {
            throw new NotImplementedException();
        }

        public void ClearPositionalArguments()
        {
            throw new NotImplementedException();
        }

        bool RegisterFoundOption(string optionName)
        {
            if (nameHash.ContainsKey(optionName)) {
                optionNames.Add(optionName);
                return true;
            } else {
                unknownOptionNames.Add(optionName);
                return false;
            }
        }

        bool ParseOptionValue(string optionName, string argument,
             IEnumerator<string> argumentEnumerator, ref bool atEnd)
        {
            const char assignChar = '=';
            int optionOffset;
            if (nameHash.TryGetValue(optionName, out optionOffset)) {
                int assignPos = argument.IndexOf(assignChar);
                bool withValue = !string.IsNullOrEmpty(
                    commandLineOptionList[optionOffset].ValueName);
                if (withValue) {
                    if (assignPos == -1) {
                        if (atEnd = (!argumentEnumerator.MoveNext())) {
                            return false;
                        }
                        if (!optionValuesHash.ContainsKey(optionOffset))
                            optionValuesHash.Add(optionOffset, new List<string>());
                        optionValuesHash[optionOffset].Add(argumentEnumerator.Current);
                    } else {
                        if (!optionValuesHash.ContainsKey(optionOffset))
                            optionValuesHash.Add(optionOffset, new List<string>());
                        optionValuesHash[optionOffset].Add(argument.Substring(assignPos + 1));
                    }
                } else {
                    if (assignPos != -1) {
                        return false;
                    }
                }
            }
            return true;
        }

        void CheckParsed(string method)
        {
            if (needsParsing)
                Trace.TraceWarning("CommandLineParser: Parse() before {0}", method);
        }

        public bool Parse(string commandLine)
        {
            List<string> arguments = new List<string>();
            StringBuilder arg = new StringBuilder();
            foreach (Match token in Lexer.Tokenize(commandLine)) {
                if (token.TokenType() == Token.Whitespace) {
                    if (arg.Length > 0) {
                        arguments.Add(arg.ToString());
                        arg.Clear();
                    }
                } else {
                    arg.Append(token.TokenText());
                }
            }
            if (arg.Length > 0)
                arguments.Add(arg.ToString());
            return Parse(arguments);
        }

        public bool Parse(IEnumerable<string> args)
        {
            needsParsing = false;

            bool error = false;

            const string doubleDashString = "--";
            const char dashChar = '-';
            const char assignChar = '=';

            bool forcePositional = false;
            positionalArgumentList.Clear();
            optionNames.Clear();
            unknownOptionNames.Clear();
            optionValuesHash.Clear();

            if (!args.Any()) {
                return false;
            }

            var argumentIterator = args.GetEnumerator();
            bool atEnd = false;

            while (!atEnd && argumentIterator.MoveNext()) {
                var argument = argumentIterator.Current;
                if (forcePositional) {
                    positionalArgumentList.Add(argument);
                } else if (argument.StartsWith(doubleDashString)) {
                    if (argument.Length > 2) {
                        var optionName = argument.Substring(2).Split(new char[] { assignChar })[0];
                        if (RegisterFoundOption(optionName)) {
                            if (!ParseOptionValue(
                                optionName,
                                argument,
                                argumentIterator,
                                ref atEnd)) {
                                error = true;
                            }

                        } else {
                            error = true;
                        }
                    } else {
                        forcePositional = true;
                    }
                } else if (argument.StartsWith(dashChar.ToString())) {
                    if (argument.Length == 1) { // single dash ("stdin")
                        positionalArgumentList.Add(argument);
                        continue;
                    }
                    string optionName = "";
                    switch (singleDashWordOptionMode) {
                        case SingleDashWordOptionMode.ParseAsCompactedShortOptions:
                            bool valueFound = false;
                            for (int pos = 1; pos < argument.Length; ++pos) {
                                optionName = argument.Substring(pos, 1);
                                if (!RegisterFoundOption(optionName)) {
                                    error = true;
                                } else {
                                    int optionOffset;
                                    Trace.Assert(nameHash.TryGetValue(
                                        optionName,
                                        out optionOffset));
                                    bool withValue = !string.IsNullOrEmpty(
                                        commandLineOptionList[optionOffset].ValueName);
                                    if (withValue) {
                                        if (pos + 1 < argument.Length) {
                                            if (argument[pos + 1] == assignChar)
                                                ++pos;
                                            if (!optionValuesHash.ContainsKey(optionOffset)) {
                                                optionValuesHash.Add(
                                                    optionOffset,
                                                    new List<string>());
                                            }
                                            optionValuesHash[optionOffset].Add(
                                                argument.Substring(pos + 1));
                                            valueFound = true;
                                        }
                                        break;
                                    }
                                    if (pos + 1 < argument.Length
                                        && argument[pos + 1] == assignChar) {
                                        break;
                                    }
                                }
                            }
                            if (!valueFound
                                && !ParseOptionValue(
                                    optionName,
                                    argument,
                                    argumentIterator,
                                    ref atEnd)) {
                                error = true;
                            }

                            break;
                        case SingleDashWordOptionMode.ParseAsLongOptions:
                            if (argument.Length > 2) {
                                string possibleShortOptionStyleName = argument.Substring(1, 1);

                                int shortOptionIdx;
                                if (nameHash.TryGetValue(
                                    possibleShortOptionStyleName,
                                    out shortOptionIdx)) {
                                    var arg = commandLineOptionList[shortOptionIdx];
                                    if ((arg.Flags & Option.Flag.ShortOptionStyle) != 0) {
                                        RegisterFoundOption(possibleShortOptionStyleName);
                                        if (!optionValuesHash.ContainsKey(shortOptionIdx)) {
                                            optionValuesHash.Add(
                                                shortOptionIdx,
                                                new List<string>());
                                        }
                                        optionValuesHash[shortOptionIdx].Add(
                                            argument.Substring(2));
                                        break;
                                    }
                                }
                            }
                            optionName = argument.Substring(1).Split(new char[] { assignChar })[0];
                            if (RegisterFoundOption(optionName)) {
                                if (!ParseOptionValue(
                                    optionName,
                                    argument,
                                    argumentIterator,
                                    ref atEnd)) {
                                    error = true;
                                }

                            } else {
                                error = true;
                            }
                            break;
                    }
                } else {
                    positionalArgumentList.Add(argument);
                    if (optionsAfterPositionalArgumentsMode
                        == OptionsAfterPositionalArgumentsMode.ParseAsPositionalArguments) {
                        forcePositional = true;
                    }
                }
            }
            return !error;
        }

        public bool IsSet(string name)
        {
            CheckParsed("IsSet");
            if (optionNames.Contains(name))
                return true;
            var aliases = Aliases(name);
            foreach (var optionName in optionNames) {
                if (aliases.Contains(optionName))
                    return true;
            }
            return false;
        }

        public string Value(string optionName)
        {
            CheckParsed("Value");
            var valueList = Values(optionName);
            if (valueList.Any())
                return valueList.Last();
            return "";
        }

        public IEnumerable<string> Values(string optionName)
        {
            CheckParsed("Values");
            int optionOffset;
            if (nameHash.TryGetValue(optionName, out optionOffset)) {
                var values = optionValuesHash[optionOffset];
                return values;
            }

            Trace.TraceWarning("QCommandLineParser: option not defined: \"{0}\"", optionName);
            return new List<string>();
        }

        public bool IsSet(Option option)
        {
            return option.Names.Any() && IsSet(option.Names.First());
        }

        public string Value(Option option)
        {
            return Value(option.Names.FirstOrDefault());
        }

        public IEnumerable<string> Values(Option option)
        {
            return Values(option.Names.FirstOrDefault());
        }
    }

    public class Option
    {
        [Flags]
        public enum Flag
        {
            HiddenFromHelp = 0x1,
            ShortOptionStyle = 0x2
        }

        public Option(string name, string valueName = "")
        {
            Names = new[] { name };
            ValueName = valueName;
            Flags = 0;
        }

        public Option(IEnumerable<string> names, string valueName = "")
        {
            Names = names;
            ValueName = valueName;
            Flags = 0;
        }

        public Option(Option other)
        {
            Names = other.Names;
            ValueName = other.ValueName;
            Flags = other.Flags;
        }

        public IEnumerable<string> Names
        {
            get;
            private set;
        }

        public string ValueName
        {
            get;
            set;
        }

        public Flag Flags
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Names.Last();
        }

    }

    enum Token
    {
        Unknown = 0,
        Unquoted = 1,
        Quoted = 2,
        Whitespace = 3
    };

    static class Lexer
    {
        static Regex lexer = new Regex(@"([^\s\""]+)|(?:\""([^\""]+)\"")|(\s+)");

        public static Token TokenType(this Match token)
        {
            for (int i = 1; i < token.Groups.Count; i++) {
                if (!string.IsNullOrEmpty(token.Groups[i].Value))
                    return (Token)i;
            }
            return Token.Unknown;
        }

        public static string TokenText(this Match token)
        {
            Token t = TokenType(token);
            if (t != Token.Unknown)
                return token.Groups[(int)t].Value;
            return "";
        }

        public static MatchCollection Tokenize(string commandLine)
        {
            return lexer.Matches(commandLine);
        }
    }
}
