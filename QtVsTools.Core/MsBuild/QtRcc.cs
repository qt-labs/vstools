/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Text;

namespace QtVsTools.Core.MsBuild
{
    using CommandLine;

    public sealed class QtRcc : QtTool
    {
        public const string ItemTypeName = "QtRcc";
        public const string ToolExecName = "rcc.exe";

        public enum Property
        {
            ExecutionDescription,
            QTDIR,
            InputFile,
            OutputFile,
            TempFile,
            InitFuncName,
            Root,
            Compression,
            NoCompression,
            CompressThreshold,
            BinaryOutput,
            NoZstd,
            PassNumber,
            NoNamespace,
            Verbose,
            List,
            Project,
            FormatVersion,
            CommandLineTemplate,
            AdditionalOptions,
            DynamicSource,
            ParallelProcess,
            AdditionalDependencies
        }

        private readonly Dictionary<Property, Option> options = new();

        public QtRcc()
        {
            Parser.AddOption(options[Property.TempFile] =
                new Option(new[] { "t", "temp" }, "file"));

            Parser.AddOption(options[Property.InitFuncName] =
                new Option("name", "name"));

            Parser.AddOption(options[Property.Root] =
                new Option("root", "path"));

            Parser.AddOption(options[Property.Compression] =
                new Option("compress", "level"));

            Parser.AddOption(options[Property.NoCompression] =
                new Option("no-compress"));

            Parser.AddOption(options[Property.CompressThreshold] =
                new Option("threshold", "level"));

            Parser.AddOption(options[Property.BinaryOutput] =
                new Option("binary"));

            Parser.AddOption(options[Property.NoZstd] =
                new Option("no-zstd"));

            Parser.AddOption(options[Property.PassNumber] =
                new Option("pass", "number"));

            Parser.AddOption(options[Property.NoNamespace] =
                new Option("namespace"));

            Parser.AddOption(options[Property.Verbose] =
                new Option("verbose"));

            Parser.AddOption(options[Property.List] =
                new Option("list"));

            Parser.AddOption(options[Property.Project] =
                new Option("project"));

            Parser.AddOption(options[Property.FormatVersion] =
                new Option("format-version", "number"));
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

            if (Parser.IsSet(options[Property.InitFuncName]))
                properties[Property.InitFuncName] = Parser.Value(options[Property.InitFuncName]);

            if (Parser.IsSet(options[Property.Root]))
                properties[Property.Root] = Parser.Value(options[Property.Root]);

            if (Parser.IsSet(options[Property.Compression])) {
                if (!int.TryParse(Parser.Value(options[Property.Compression]), out var level))
                    return false;
                if (level is < 1 or > 9)
                    return false;
                properties[Property.Compression] = $"level{level}";
            } else {
                properties[Property.Compression] = "default";
            }

            if (Parser.IsSet(options[Property.NoCompression]))
                properties[Property.NoCompression] = "true";

            if (Parser.IsSet(options[Property.CompressThreshold])) {
                properties[Property.CompressThreshold] =
                    Parser.Value(options[Property.CompressThreshold]);
            }

            if (Parser.IsSet(options[Property.BinaryOutput]))
                properties[Property.BinaryOutput] = "true";

            if (Parser.IsSet(options[Property.NoZstd]))
                properties[Property.NoZstd] = "true";

            if (Parser.IsSet(options[Property.PassNumber]))
                properties[Property.PassNumber] = Parser.Value(options[Property.PassNumber]);

            if (Parser.IsSet(options[Property.NoNamespace]))
                properties[Property.NoNamespace] = "true";

            if (Parser.IsSet(options[Property.Verbose]))
                properties[Property.Verbose] = "true";

            if (Parser.IsSet(options[Property.List]))
                properties[Property.List] = "true";

            if (Parser.IsSet(options[Property.Project]))
                properties[Property.Project] = "true";

            if (Parser.IsSet(options[Property.FormatVersion]))
                properties[Property.FormatVersion] = Parser.Value(options[Property.FormatVersion]);

            return true;
        }

        public string GenerateCommandLine(QtMsBuildContainer container, object propertyStorage)
        {
            var cmd = new StringBuilder();
            cmd.AppendFormat(@"""{0}\bin\{1}"" ""{2}"" -o ""{3}""",
                container.GetPropertyValue(propertyStorage, Property.QTDIR),
                ToolExecName,
                container.GetPropertyValue(propertyStorage, Property.InputFile),
                container.GetPropertyValue(propertyStorage, Property.OutputFile));

            var value = container.GetPropertyValue(propertyStorage, Property.InitFuncName);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.InitFuncName], value);

            value = container.GetPropertyValue(propertyStorage, Property.Root);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Root], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.Compression);
            if (value.StartsWith("level")) {
                GenerateCommandLineOption(cmd,
                    options[Property.Compression],
                    value.Substring(5), true);
            }

            if (container.GetPropertyValue(propertyStorage, Property.NoCompression) == "true")
                GenerateCommandLineOption(cmd, options[Property.NoCompression]);

            value = container.GetPropertyValue(propertyStorage, Property.CompressThreshold);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.CompressThreshold], value);

            if (container.GetPropertyValue(propertyStorage, Property.BinaryOutput) == "true")
                GenerateCommandLineOption(cmd, options[Property.BinaryOutput]);

            if (container.GetPropertyValue(propertyStorage, Property.NoZstd) == "true")
                GenerateCommandLineOption(cmd, options[Property.NoZstd]);

            value = container.GetPropertyValue(propertyStorage, Property.PassNumber);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.PassNumber], value);

            if (container.GetPropertyValue(propertyStorage, Property.Verbose) == "true")
                GenerateCommandLineOption(cmd, options[Property.Verbose]);

            if (container.GetPropertyValue(propertyStorage, Property.List) == "true")
                GenerateCommandLineOption(cmd, options[Property.List]);

            if (container.GetPropertyValue(propertyStorage, Property.Project) == "true")
                GenerateCommandLineOption(cmd, options[Property.Project]);

            value = container.GetPropertyValue(propertyStorage, Property.FormatVersion);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.FormatVersion], value);

            return cmd.ToString();
        }
    }
}
