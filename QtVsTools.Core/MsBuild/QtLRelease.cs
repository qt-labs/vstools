// ************************************************************************************************
// Copyright (C) 2023 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
// ************************************************************************************************

using System.Collections.Generic;

namespace QtVsTools.Core.MsBuild
{
    using CommandLine;

    public sealed class QtLRelease : QtTool
    {
        public const string ItemTypeName = "QtTranslation";
        public const string ToolExecName = "lrelease.exe";

        public enum Property
        {
            ReleaseDescription,
            InputFile,
            BuildAction,
            QmOutputDir,
            QmOutputFile,
            IdBased,
            Compress,
            NoUnfinished,
            RemoveIdentical,
            UntranslatedPrefix,
            Project,
            ReleaseSilent
        }

        private readonly Dictionary<Property, Option> options = new();

        public QtLRelease()
            : base(defaultInputOutput: false)
        {
            Parser.AddOption(options[Property.QmOutputFile] =
                new Option("qm", "qm-file"));

            Parser.AddOption(options[Property.IdBased] =
                new Option("idbased"));

            Parser.AddOption(options[Property.Compress] =
                new Option("compress"));

            Parser.AddOption(options[Property.NoUnfinished] =
                new Option("nounfinished"));

            Parser.AddOption(options[Property.RemoveIdentical] =
                new Option("removeidentical"));

            Parser.AddOption(options[Property.Project] =
                new Option("project", "filename"));

            Parser.AddOption(options[Property.UntranslatedPrefix] =
                new Option("markuntranslated", "prefix"));

            Parser.AddOption(options[Property.ReleaseSilent] =
                new Option("silent"));
        }

        public bool ParseCommandLine(string commandLine, IVsMacroExpander macros,
            out Dictionary<Property, string> properties)
        {
            properties = new Dictionary<Property, string>();

            if (!ParseCommandLine(commandLine, macros, ToolExecName, out _ /* ignore qtDir */,
                out var inputPath, out var outputPath)) {
                return false;
            }

            if (!string.IsNullOrEmpty(inputPath))
                properties[Property.InputFile] = inputPath;

            if (!string.IsNullOrEmpty(outputPath))
                properties[Property.QmOutputDir] = outputPath;

            if (Parser.IsSet(options[Property.QmOutputFile]))
                properties[Property.QmOutputFile] = Parser.Value(options[Property.QmOutputFile]);

            if (Parser.IsSet(options[Property.IdBased]))
                properties[Property.IdBased] = "true";

            if (Parser.IsSet(options[Property.Compress]))
                properties[Property.Compress] = "true";

            if (Parser.IsSet(options[Property.NoUnfinished]))
                properties[Property.NoUnfinished] = "true";

            if (Parser.IsSet(options[Property.RemoveIdentical]))
                properties[Property.RemoveIdentical] = "true";

            if (Parser.IsSet(options[Property.Project]))
                properties[Property.Project] = Parser.Value(options[Property.Project]);

            if (Parser.IsSet(options[Property.UntranslatedPrefix])) {
                properties[Property.UntranslatedPrefix] =
                    Parser.Value(options[Property.UntranslatedPrefix]);
            }

            if (Parser.IsSet(options[Property.ReleaseSilent]))
                properties[Property.ReleaseSilent] = "true";

            return true;
        }
    }
}
