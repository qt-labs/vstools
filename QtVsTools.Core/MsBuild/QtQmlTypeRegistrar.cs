// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Collections.Generic;
using System.Linq;

namespace QtVsTools.Core.MsBuild
{
    using CommandLine;

    public sealed class QtQmlTypeRegistrar : QtTool
    {
        public const string ItemTypeName = "QtQmlTypeRegistrar";
        public const string ToolExecName = "qmltyperegistrar.exe";

        public enum Property
        {
            ExecutionDescription,
            QTDIR,
            InputFile,
            OutputFile,
            GenerateQmlTypes,
            ImportName,
            MajorVersion,
            MinorVersion,
            ForeignTypes,
            PastMajorVersion,
            PrivateIncludes,
            Namespace,
            JsRoot,
            Extract,
            MergeQtConf,
            FollowForeignVersioning
        }

        private readonly Dictionary<Property, Option> options = new();

        public QtQmlTypeRegistrar()
        {
            Parser.AddOption(options[Property.GenerateQmlTypes] =
                new Option("generate-qmltypes", "file"));

            Parser.AddOption(options[Property.ImportName] =
                new Option("import-name", "name"));

            Parser.AddOption(options[Property.MajorVersion] =
                new Option("major-version", "version"));

            Parser.AddOption(options[Property.MinorVersion] =
                new Option("minor-version", "version"));

            Parser.AddOption(options[Property.ForeignTypes] =
                new Option("foreign-types", "file"));

            Parser.AddOption(options[Property.PastMajorVersion] =
                new Option("past-major-version", "version"));

            Parser.AddOption(options[Property.PrivateIncludes] =
                new Option("private-includes"));

            Parser.AddOption(options[Property.Namespace] =
                new Option("namespace", "name"));

            Parser.AddOption(options[Property.JsRoot] =
                new Option("jsroot"));

            Parser.AddOption(options[Property.Extract] =
                new Option("extract"));

            Parser.AddOption(options[Property.MergeQtConf] =
                new Option("merge-qt-conf", "file"));

            Parser.AddOption(options[Property.FollowForeignVersioning] =
                new Option("follow-foreign-versioning"));
        }

        public bool ParseCommandLine(string commandLine, IVsMacroExpander macros,
            out Dictionary<Property, string> properties)
        {
            properties = new Dictionary<Property, string>();

            if (!ParseCommandLine(commandLine, macros, ToolExecName, out var qtDir,
                out var inputPath, out var outputPath)) {
                return false;
            }

            if (!string.IsNullOrEmpty(qtDir))
                properties[Property.QTDIR] = qtDir;

            if (!string.IsNullOrEmpty(inputPath))
                properties[Property.InputFile] = inputPath;

            if (!string.IsNullOrEmpty(outputPath))
                properties[Property.OutputFile] = outputPath;

            if (Parser.IsSet(options[Property.GenerateQmlTypes])) {
                properties[Property.GenerateQmlTypes] =
                    Parser.Value(options[Property.GenerateQmlTypes]);
            }

            if (Parser.IsSet(options[Property.ImportName]))
                properties[Property.ImportName] = Parser.Value(options[Property.ImportName]);

            if (Parser.IsSet(options[Property.MajorVersion]))
                properties[Property.MajorVersion] = Parser.Value(options[Property.MajorVersion]);

            if (Parser.IsSet(options[Property.MinorVersion]))
                properties[Property.MinorVersion] = Parser.Value(options[Property.MinorVersion]);

            if (Parser.IsSet(options[Property.ForeignTypes])) {
                var values = Parser.Values(options[Property.ForeignTypes])
                    .SelectMany(value => value.Split(';', ','))
                    .Select(value => value.Trim())
                    .Where(value => !string.IsNullOrEmpty(value));
                properties[Property.ForeignTypes] = string.Join(";", values);
            }

            if (Parser.IsSet(options[Property.PastMajorVersion])) {
                properties[Property.PastMajorVersion] =
                    string.Join(";", Parser.Values(options[Property.PastMajorVersion]));
            }

            if (Parser.IsSet(options[Property.PrivateIncludes]))
                properties[Property.PrivateIncludes] = "true";

            if (Parser.IsSet(options[Property.Namespace]))
                properties[Property.Namespace] = Parser.Value(options[Property.Namespace]);

            if (Parser.IsSet(options[Property.JsRoot]))
                properties[Property.JsRoot] = "true";

            if (Parser.IsSet(options[Property.Extract]))
                properties[Property.Extract] = "true";

            if (Parser.IsSet(options[Property.MergeQtConf]))
                properties[Property.MergeQtConf] = Parser.Value(options[Property.MergeQtConf]);

            if (Parser.IsSet(options[Property.FollowForeignVersioning]))
                properties[Property.FollowForeignVersioning] = "true";

            return true;
        }
    }
}
