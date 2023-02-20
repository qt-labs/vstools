/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace QtVsTools.Core.CommandLine
{
    using IVSMacroExpander = QtMsBuild.IVSMacroExpander;
    using static Utils;

    public class Parser
    {
        readonly List<Option> commandLineOptionList = new List<Option>();
        readonly Dictionary<string, int> nameHash = new Dictionary<string, int>();
        readonly Dictionary<int, List<string>> optionValuesHash = new Dictionary<int, List<string>>();
        readonly List<string> optionNames = new List<string>();
        readonly List<string> positionalArgumentList = new List<string>();
        readonly List<string> unknownOptionNames = new List<string>();
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

        public string ErrorText => throw new NotImplementedException();

        public string HelpText => throw new NotImplementedException();

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
            if (!nameHash.TryGetValue(optionName, out int optionIndex)) {
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
            if (nameHash.TryGetValue(optionName, out int optionOffset)) {
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

        List<string> TokenizeArgs(string commandLine, IVSMacroExpander macros, string execName = "")
        {
            List<string> arguments = new List<string>();
            StringBuilder arg = new StringBuilder();
            bool foundExec = string.IsNullOrEmpty(execName);
            foreach (Match token in Lexer.Tokenize(commandLine + " ")) {
                // Additional " " ensures loop will always end with whitespace processing

                if (!foundExec) {
                    if (!token.TokenText().EndsWith(execName, IgnoreCase))
                        continue;

                    foundExec = true;
                }

                var tokenType = token.TokenType();
                if (tokenType == Token.Whitespace || tokenType == Token.Newline) {
                    // This will always run at the end of the loop

                    if (arg.Length > 0) {
                        var argData = arg.ToString();
                        arg.Clear();
                        if (argData.StartsWith("@")) {
                            var workingDir = macros.ExpandString("$(MSBuildProjectDirectory)");
                            var optFilePath = macros.ExpandString(argData.Substring(1));
                            string[] additionalArgs = File.ReadAllLines(
                                Path.Combine(workingDir, optFilePath));
                            if (additionalArgs.Length != 0) {
                                var additionalArgsString = string.Join(" ", additionalArgs
                                    .Select(x => "\"" + x.Replace("\"", "\\\"") + "\""));
                                arguments.AddRange(TokenizeArgs(additionalArgsString, macros));
                            }
                        } else {
                            arguments.Add(argData);
                        }
                    }
                    if (tokenType == Token.Newline)
                        break;

                } else {
                    arg.Append(token.TokenText());
                }
            }
            return arguments;
        }

        public bool Parse(string commandLine, IVSMacroExpander macros, string execName)
        {
            List<string> args = null;
            try {
                args = TokenizeArgs(commandLine, macros, execName);
            } catch {
                return false;
            }
            return Parse(args);
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

            if (args == null || !args.Any()) {
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
                                Trace.Assert(nameHash.TryGetValue(
                                    optionName,
                                    out int optionOffset));
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

                            if (nameHash.TryGetValue(
                                possibleShortOptionStyleName,
                                out int shortOptionIdx)) {
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
            if (nameHash.TryGetValue(optionName, out int optionOffset)) {
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
        Newline = 1,
        Unquoted = 2,
        Quoted = 3,
        Whitespace = 4
    };

    static class Lexer
    {
        static readonly Regex lexer = new Regex(
            /* Newline    */ @"(\n)" +
            /* Unquoted   */ @"|((?:(?:[^\s\""])|(?:(?<=\\)\""))+)" +
            /* Quoted     */ @"|(?:\""((?:(?:[^\""])|(?:(?<=\\)\""))+)\"")" +
            /* Whitespace */ @"|(\s+)");

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
                return token.Groups[(int)t].Value.Replace("\\\"", "\"");
            return "";
        }

        public static MatchCollection Tokenize(string commandLine)
        {
            return lexer.Matches(commandLine);
        }
    }
}
