// ************************************************************************************************
// Copyright (C) 2023 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
// ************************************************************************************************

using System.IO;
using System.Linq;

namespace QtVsTools.Core.MsBuild
{
    using CommandLine;
    using Common;

    public abstract class QtTool
    {
        protected readonly Parser Parser;
        private readonly Option outputOption;
        private Option helpOption;
        private Option versionOption;

        protected QtTool(bool defaultInputOutput = true)
        {
            Parser = new Parser();
            Parser.SetSingleDashWordOptionMode(
                Parser.SingleDashWordOptionMode.ParseAsLongOptions);

            helpOption = Parser.AddHelpOption();
            versionOption = Parser.AddVersionOption();

            if (!defaultInputOutput)
                return;

            Parser.AddOption(outputOption = new Option("o")
            {
                ValueName = "file",
                Flags = Option.Flag.ShortOptionStyle
            });
        }

        protected virtual void ExtractInputOutput(
            string toolExecName,
            out string inputPath,
            out string outputPath)
        {
            inputPath = outputPath = "";

            var filePath = Parser.PositionalArguments
                .FirstOrDefault(arg => !arg.EndsWith(toolExecName, Utils.IgnoreCase));
            if (!string.IsNullOrEmpty(filePath))
                inputPath = filePath;

            if (outputOption != null && Parser.IsSet(outputOption))
                outputPath = Parser.Value(outputOption);
        }

        protected bool ParseCommandLine(
            string commandLine,
            IVsMacroExpander macros,
            string toolExecName,
            out string qtDir,
            out string inputPath,
            out string outputPath)
        {
            qtDir = inputPath = outputPath = "";
            if (!Parser.Parse(commandLine, macros, toolExecName))
                return false;

            var execPath = Parser.PositionalArguments
                .FirstOrDefault(arg => arg.EndsWith(toolExecName, Utils.IgnoreCase));
            if (!string.IsNullOrEmpty(execPath)) {
                var execDir = Path.GetDirectoryName(execPath);
                if (!string.IsNullOrEmpty(execDir))
                    qtDir = HelperFunctions.CanonicalPath(Path.Combine(execDir, ".."));
            }

            ExtractInputOutput(toolExecName, out inputPath, out outputPath);
            return true;
        }
    }
}
