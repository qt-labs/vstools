/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core.MsBuild
{
    using CommandLine;

    public sealed class QtMoc : QtTool
    {
        public const string ItemTypeName = "QtMoc";
        public const string ToolExecName = "moc.exe";

        public enum Property
        {
            ExecutionDescription,
            QTDIR,
            InputFile,
            OutputFile,
            IncludePath,
            MacFramework,
            PreprocessOnly,
            Define,
            Undefine,
            Metadata,
            CompilerFlavor,
            NoInclude,
            PathPrefix,
            ForceInclude,
            PrependInclude,
            Include,
            NoNotesWarnings,
            NoNotes,
            NoWarnings,
            IgnoreConflicts,
            OutputJson,
            DebugIncludes,
            CollectJson,
            OutputDepFile,
            DepFilePath,
            DepFileRuleName,
            RequireCompleteTypes,
            OptionsFile,
            CommandLineTemplate,
            AdditionalOptions,
            DynamicSource,
            ParallelProcess,
            AdditionalDependencies
        }

        private readonly Dictionary<Property, Option> options = new();
        private static readonly MsBuildProjectContainer QtMsBuild = new(new VcPropertyStorageProvider());

        public QtMoc()
        {
            Parser.AddOption(options[Property.IncludePath] =
                new Option("I")
                {
                    ValueName = "dir",
                    Flags = Option.Flag.ShortOptionStyle
                });

            Parser.AddOption(options[Property.MacFramework] =
                new Option("F")
                {
                    ValueName = "framework",
                    Flags = Option.Flag.ShortOptionStyle
                });

            Parser.AddOption(options[Property.PreprocessOnly] =
                new Option("E"));

            Parser.AddOption(options[Property.Define] =
                new Option("D")
                {
                    ValueName = "macro[=def]",
                    Flags = Option.Flag.ShortOptionStyle
                });

            Parser.AddOption(options[Property.Undefine] =
                new Option("U")
                {
                    ValueName = "macro",
                    Flags = Option.Flag.ShortOptionStyle
                });

            Parser.AddOption(options[Property.Metadata] =
                new Option("M")
                {
                    ValueName = "key=value",
                    Flags = Option.Flag.ShortOptionStyle
                });

            Parser.AddOption(options[Property.CompilerFlavor] =
                new Option("compiler-flavor")
                {
                    ValueName = "flavor"
                });

            Parser.AddOption(options[Property.NoInclude] =
                new Option("i"));

            Parser.AddOption(options[Property.PathPrefix] =
                new Option("p")
                {
                    ValueName = "path",
                    Flags = Option.Flag.ShortOptionStyle
                });

            Parser.AddOption(options[Property.ForceInclude] =
                new Option("f")
                {
                    ValueName = "file",
                    Flags = Option.Flag.ShortOptionStyle
                });

            Parser.AddOption(options[Property.PrependInclude] =
                new Option("b")
                {
                    ValueName = "file"
                });

            Parser.AddOption(options[Property.Include] =
                new Option("include")
                {
                    ValueName = "file"
                });

            Parser.AddOption(options[Property.NoNotesWarnings] =
                new Option("n")
                {
                    ValueName = "which",
                    Flags = Option.Flag.ShortOptionStyle
                });

            Parser.AddOption(options[Property.NoNotes] =
                new Option("no-notes"));

            Parser.AddOption(options[Property.NoWarnings] =
                new Option("no-warnings"));

            Parser.AddOption(options[Property.IgnoreConflicts] =
                new Option("ignore-option-clashes"));

            Parser.AddOption(options[Property.OutputJson] =
                new Option("output-json"));

            Parser.AddOption(options[Property.DebugIncludes] =
                new Option("debug-includes"));

            Parser.AddOption(options[Property.CollectJson] =
                new Option("collect-json"));

            Parser.AddOption(options[Property.OutputDepFile] =
                new Option("output-dep-file"));

            Parser.AddOption(options[Property.DepFilePath] =
                new Option("dep-file-path")
                {
                    ValueName = "file"
                });

            Parser.AddOption(options[Property.DepFileRuleName] =
                new Option("dep-file-rule-name")
                {
                    ValueName = "rule name"
                });

            Parser.AddOption(options[Property.RequireCompleteTypes] =
                new Option("require-complete-types"));
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

            if (Parser.IsSet(options[Property.IncludePath])) {
                properties[Property.IncludePath] =
                    string.Join(";", Parser.Values(options[Property.IncludePath]));
            }

            if (Parser.IsSet(options[Property.MacFramework])) {
                properties[Property.MacFramework] =
                    string.Join(";", Parser.Values(options[Property.MacFramework]));
            }

            if (Parser.IsSet(options[Property.PreprocessOnly]))
                properties[Property.PreprocessOnly] = "true";

            if (Parser.IsSet(options[Property.Define])) {
                properties[Property.Define] =
                    string.Join(";", Parser.Values(options[Property.Define]));
            }

            if (Parser.IsSet(options[Property.Undefine])) {
                properties[Property.Undefine] =
                    string.Join(";", Parser.Values(options[Property.Undefine]));
            }

            if (Parser.IsSet(options[Property.Metadata])) {
                properties[Property.Metadata] =
                    string.Join(";", Parser.Values(options[Property.Metadata]));
            }

            if (Parser.IsSet(options[Property.CompilerFlavor])) {
                properties[Property.CompilerFlavor] =
                    Parser.Value(options[Property.CompilerFlavor]);
            }

            if (Parser.IsSet(options[Property.NoInclude]))
                properties[Property.NoInclude] = "true";

            if (Parser.IsSet(options[Property.PathPrefix]))
                properties[Property.PathPrefix] = Parser.Value(options[Property.PathPrefix]);

            if (Parser.IsSet(options[Property.ForceInclude])) {
                properties[Property.ForceInclude] =
                    string.Join(";", Parser.Values(options[Property.ForceInclude]));
            }

            if (Parser.IsSet(options[Property.PrependInclude])) {
                properties[Property.PrependInclude] =
                    string.Join(";", Parser.Values(options[Property.PrependInclude]));
            }

            if (Parser.IsSet(options[Property.Include])) {
                properties[Property.Include] =
                    string.Join(";", Parser.Values(options[Property.Include]));
            }

            if (Parser.IsSet(options[Property.NoNotesWarnings])) {
                properties[Property.NoNotesWarnings] =
                    string.Join(";", Parser.Values(options[Property.NoNotesWarnings]));
            }

            if (Parser.IsSet(options[Property.NoNotes]))
                properties[Property.NoNotes] = "true";

            if (Parser.IsSet(options[Property.NoWarnings]))
                properties[Property.NoWarnings] = "true";

            if (Parser.IsSet(options[Property.IgnoreConflicts]))
                properties[Property.IgnoreConflicts] = "true";

            if (Parser.IsSet(options[Property.OutputJson]))
                properties[Property.OutputJson] = "true";

            if (Parser.IsSet(options[Property.DebugIncludes]))
                properties[Property.DebugIncludes] = "true";

            if (Parser.IsSet(options[Property.CollectJson]))
                properties[Property.CollectJson] = "true";

            if (Parser.IsSet(options[Property.OutputDepFile]))
                properties[Property.OutputDepFile] = "true";

            if (Parser.IsSet(options[Property.DepFilePath]))
                properties[Property.DepFilePath] = Parser.Value(options[Property.DepFilePath]);

            if (Parser.IsSet(options[Property.DepFileRuleName])) {
                properties[Property.DepFileRuleName] =
                    Parser.Value(options[Property.DepFileRuleName]);
            }

            if (Parser.IsSet(options[Property.RequireCompleteTypes]))
                properties[Property.RequireCompleteTypes] = "true";

            return true;
        }

        /// <summary>
        /// This function returns <see langword="true" /> if the MsBuild file
        /// item type is set to QtMoc; <see langword="false" /> otherwise.
        /// </summary>
        /// <param name="file"></param>
        public static bool HasMocItemType(VCFile file)
        {
            return file.ItemType == ItemTypeName;
        }

        /// <summary>
        /// This function sets the MSBuild file item type to QtMoc.
        /// </summary>
        /// <param name="file">file</param>
        public static void SetMocItemType(VCFile file)
        {
            file.ItemType = ItemTypeName;
            if (!HelperFunctions.IsSourceFile(file.FullPath))
                return;

            foreach (VCFileConfiguration config in (IVCCollection)file.FileConfigurations) {
                QtMsBuild.SetItemProperty(config, Property.DynamicSource, "input");
                QtMsBuild.SetItemPropertyByName(config, "QtMocFileName", "%(Filename).moc");
            }
        }

        /// <summary>
        /// This function removes the QtMoc MSBuild file item type.
        /// </summary>
        /// <param name="file">file</param>
        public static void RemoveMocItemType(VCFile file)
        {
            if (file.ItemType != ItemTypeName)
                return;

            if (HelperFunctions.IsHeaderFile(file.Name))
                file.ItemType = "ClInclude";
            else if (HelperFunctions.IsSourceFile(file.Name))
                file.ItemType = "ClCompile";
            else
                file.ItemType = "None";
        }
    }
}
