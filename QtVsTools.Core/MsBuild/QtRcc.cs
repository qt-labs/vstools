/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using Microsoft.VisualStudio.VCProjectEngine;

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
            CompressionAlgorithm,
            Compression,
            NoCompression,
            NoZstd,
            CompressThreshold,
            BinaryOutput,
            Generator,
            PassNumber,
            NoNamespace,
            Verbose,
            List,
            ListMapping,
            DepFile,
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

            Parser.AddOption(options[Property.CompressionAlgorithm] =
                new Option("compress-algo", "algo"));

            Parser.AddOption(options[Property.Compression] =
                new Option("compress", "level"));

            Parser.AddOption(options[Property.NoCompression] =
                new Option("no-compress"));

            Parser.AddOption(options[Property.NoZstd] =
                new Option("no-zstd"));

            Parser.AddOption(options[Property.CompressThreshold] =
                new Option("threshold", "level"));

            Parser.AddOption(options[Property.BinaryOutput] =
                new Option("binary"));

            Parser.AddOption(options[Property.Generator] =
                new Option(new[] { "g", "generator" }, "cpp|python|python2"));

            Parser.AddOption(options[Property.PassNumber] =
                new Option("pass", "number"));

            Parser.AddOption(options[Property.NoNamespace] =
                new Option("namespace"));

            Parser.AddOption(options[Property.Verbose] =
                new Option("verbose"));

            Parser.AddOption(options[Property.List] =
                new Option("list"));

            Parser.AddOption(options[Property.ListMapping] =
                new Option("list-mapping"));

            Parser.AddOption(options[Property.DepFile] =
                new Option(new[] { "d", "depfile" }, "file"));

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

            if (Parser.IsSet(options[Property.TempFile]))
                properties[Property.TempFile] = Parser.Value(options[Property.TempFile]);

            if (Parser.IsSet(options[Property.InitFuncName]))
                properties[Property.InitFuncName] = Parser.Value(options[Property.InitFuncName]);

            if (Parser.IsSet(options[Property.Root]))
                properties[Property.Root] = Parser.Value(options[Property.Root]);

            if (Parser.IsSet(options[Property.CompressionAlgorithm])) {
                properties[Property.CompressionAlgorithm] =
                    Parser.Value(options[Property.CompressionAlgorithm]);
            }

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

            if (Parser.IsSet(options[Property.NoZstd]))
                properties[Property.NoZstd] = "true";

            if (Parser.IsSet(options[Property.CompressThreshold])) {
                properties[Property.CompressThreshold] =
                    Parser.Value(options[Property.CompressThreshold]);
            }

            if (Parser.IsSet(options[Property.BinaryOutput]))
                properties[Property.BinaryOutput] = "true";

            if (Parser.IsSet(options[Property.Generator]))
                properties[Property.Generator] = Parser.Value(options[Property.Generator]);

            if (Parser.IsSet(options[Property.PassNumber]))
                properties[Property.PassNumber] = Parser.Value(options[Property.PassNumber]);

            if (Parser.IsSet(options[Property.NoNamespace]))
                properties[Property.NoNamespace] = "true";

            if (Parser.IsSet(options[Property.Verbose]))
                properties[Property.Verbose] = "true";

            if (Parser.IsSet(options[Property.List]))
                properties[Property.List] = "true";

            if (Parser.IsSet(options[Property.ListMapping]))
                properties[Property.ListMapping] = "true";

            if (Parser.IsSet(options[Property.DepFile]))
                properties[Property.DepFile] = Parser.Value(options[Property.DepFile]);

            if (Parser.IsSet(options[Property.Project]))
                properties[Property.Project] = "true";

            if (Parser.IsSet(options[Property.FormatVersion]))
                properties[Property.FormatVersion] = Parser.Value(options[Property.FormatVersion]);

            return true;
        }

        /// <summary>
        /// This function returns <see langword="true" /> if the MsBuild file
        /// item type is set to QtRcc; <see langword="false" /> otherwise.
        /// </summary>
        /// <param name="file"></param>
        public static bool HasRccItemType(VCFile file)
        {
            return file.ItemType == ItemTypeName;
        }

        /// <summary>
        /// This function sets the MSBuild file item type to QtRcc.
        /// </summary>
        /// <param name="file">file</param>
        public static void SetRccItemType(VCFile file)
        {
            file.ItemType = ItemTypeName;
        }
    }
}
