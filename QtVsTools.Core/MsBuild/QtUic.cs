/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core.MsBuild
{
    using CommandLine;

    public sealed class QtUic : QtTool
    {
        public const string ItemTypeName = "QtUic";
        public const string ToolExecName = "uic.exe";

        public enum Property
        {
            ExecutionDescription,
            QTDIR,
            InputFile,
            OutputFile,
            DisplayDependencies,
            NoProtection,
            NoImplicitIncludes,
            Postfix,
            Translate,
            Include,
            Generator,
            IdBased,
            CommandLineTemplate,
            AdditionalOptions,
            ParallelProcess,
            AdditionalDependencies
        }

        private readonly Dictionary<Property, Option> options = new();

        public QtUic()
        {
            Parser.AddOption(options[Property.DisplayDependencies] =
                new Option(new[] { "d", "dependencies" }));

            Parser.AddOption(options[Property.NoProtection] =
                new Option(new[] { "p", "no-protection" }));

            Parser.AddOption(options[Property.NoImplicitIncludes] =
                new Option(new[] { "n", "no-implicit-includes" }));

            Parser.AddOption(options[Property.Postfix] =
                new Option("postfix", "postfix"));

            Parser.AddOption(options[Property.Translate] =
                new Option(new[] { "tr", "translate" }, "function"));

            Parser.AddOption(options[Property.Include] =
                new Option("include", "include-file"));

            Parser.AddOption(options[Property.Generator] =
                new Option(new[] { "g", "generator" }, "java|cpp"));

            Parser.AddOption(options[Property.IdBased] =
                new Option("idbased"));
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

            if (Parser.IsSet(options[Property.DisplayDependencies]))
                properties[Property.DisplayDependencies] = "true";

            if (Parser.IsSet(options[Property.NoProtection]))
                properties[Property.NoProtection] = "true";

            if (Parser.IsSet(options[Property.NoImplicitIncludes]))
                properties[Property.NoImplicitIncludes] = "true";

            if (Parser.IsSet(options[Property.Postfix]))
                properties[Property.Postfix] = Parser.Value(options[Property.Postfix]);

            if (Parser.IsSet(options[Property.Translate]))
                properties[Property.Translate] = Parser.Value(options[Property.Translate]);

            if (Parser.IsSet(options[Property.Include]))
                properties[Property.Include] = Parser.Value(options[Property.Include]);

            if (Parser.IsSet(options[Property.Generator]))
                properties[Property.Generator] = Parser.Value(options[Property.Generator]);

            if (Parser.IsSet(options[Property.IdBased]))
                properties[Property.IdBased] = "true";

            return true;
        }

        /// <summary>
        /// This function returns <see langword="true" /> if the MsBuild file
        /// item type is set to QtUic; <see langword="false" /> otherwise.
        /// </summary>
        /// <param name="file"></param>
        public static bool HasUicItemType(VCFile file)
        {
            return file.ItemType == ItemTypeName;
        }

        /// <summary>
        /// This function sets the MSBuild file item type to QtUic.
        /// </summary>
        /// <param name="file">file</param>
        public static void SetUicItemType(VCFile file)
        {
            file.ItemType = ItemTypeName;
        }
    }
}
