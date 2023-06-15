/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Text;

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
            OptionsFile,
            CommandLineTemplate,
            AdditionalOptions,
            DynamicSource,
            ParallelProcess,
            AdditionalDependencies
        }

        private readonly Dictionary<Property, Option> options = new();

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
                    string.Join(";", Parser.Values(options[Property.CompilerFlavor]));
            }

            if (Parser.IsSet(options[Property.NoInclude]))
                properties[Property.NoInclude] = "true";

            if (Parser.IsSet(options[Property.PathPrefix])) {
                properties[Property.PathPrefix] =
                    string.Join(";", Parser.Values(options[Property.PathPrefix]));
            }

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

            var value = container.GetPropertyValue(propertyStorage, Property.IncludePath);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.IncludePath], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.MacFramework);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.MacFramework], value);

            if (container.GetPropertyValue(propertyStorage, Property.PreprocessOnly) == "true")
                GenerateCommandLineOption(cmd, options[Property.PreprocessOnly]);

            value = container.GetPropertyValue(propertyStorage, Property.Define);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Define], value);

            value = container.GetPropertyValue(propertyStorage, Property.Undefine);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Undefine], value);

            value = container.GetPropertyValue(propertyStorage, Property.Metadata);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Metadata], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.CompilerFlavor);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.CompilerFlavor], value);

            if (container.GetPropertyValue(propertyStorage, Property.NoInclude) == "true")
                GenerateCommandLineOption(cmd, options[Property.NoInclude]);

            value = container.GetPropertyValue(propertyStorage, Property.PathPrefix);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.PathPrefix], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.ForceInclude);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.ForceInclude], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.PrependInclude);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.PrependInclude], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.Include);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Include], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.NoNotesWarnings);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.NoNotesWarnings], value, true);

            if (container.GetPropertyValue(propertyStorage, Property.NoNotes) == "true")
                GenerateCommandLineOption(cmd, options[Property.NoNotes]);

            if (container.GetPropertyValue(propertyStorage, Property.NoWarnings) == "true")
                GenerateCommandLineOption(cmd, options[Property.NoWarnings]);

            if (container.GetPropertyValue(propertyStorage, Property.IgnoreConflicts) == "true")
                GenerateCommandLineOption(cmd, options[Property.IgnoreConflicts]);

            return cmd.ToString();
        }
    }
}
