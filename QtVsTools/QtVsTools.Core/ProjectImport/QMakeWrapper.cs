// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace QtVsTools.Core
{
    /// <summary>
    /// Evaluates a qmake .pri file with the qmake executable of a Qt version and provides the
    /// values of the SOURCES, HEADERS, RESOURCES and FORMS variables and the 'flat' CONFIG flag.
    /// </summary>
    /// <remarks>
    /// Values are captured before qmake's post-processing. Relative paths are preserved
    /// and must be resolved by the caller.
    /// </remarks>
    public class QMakeWrapper
    {
        public VersionInformation QtVersion { get; set; }

        public bool IsFlat { get; private set; }

        public string[] SourceFiles { get; private set; } = Array.Empty<string>();
        public string[] HeaderFiles { get; private set; } = Array.Empty<string>();
        public string[] ResourceFiles { get; private set; } = Array.Empty<string>();
        public string[] FormFiles { get; private set; } = Array.Empty<string>();

        private static readonly string[] Variables = { "SOURCES", "HEADERS", "RESOURCES", "FORMS" };
        private const string FlatFile = "flat.txt";
        private const string DoneFile = "done.txt";

        public bool ReadFile(string filePath)
        {
            if (QtVersion is null || !File.Exists(filePath))
                return false;

            var tempDir = Path.Combine(Path.GetTempPath(), "QtVsTools",
                "pri-" + Guid.NewGuid().ToString("N"));
            try {
                Directory.CreateDirectory(tempDir);
                var wrapperFile = Path.Combine(tempDir, "qtvstools_pri_import.pro");
                // qmake rejects files that start with a UTF-8 byte order mark; GetBytes never
                // writes one, whatever the preamble of the encoding.
                File.WriteAllBytes(wrapperFile,
                    QMakeEncoding.GetBytes(CreateWrapper(filePath, tempDir)));

                var exitCode = new QMakeImport.QMakeProcess(QtVersion)
                {
                    ProFile = wrapperFile
                }.Run();

                // Makefile generation can fail after the values were written, so check the marker.
                if (!File.Exists(Path.Combine(tempDir, DoneFile))) {
                    Messages.Print($"--- (Importing .pri file) qmake failed to evaluate {filePath}"
                        + $" (exit code {exitCode}).");
                    return false;
                }

                SourceFiles = ReadValues(tempDir, "SOURCES");
                HeaderFiles = ReadValues(tempDir, "HEADERS");
                ResourceFiles = ReadValues(tempDir, "RESOURCES");
                FormFiles = ReadValues(tempDir, "FORMS");
                IsFlat = File.Exists(Path.Combine(tempDir, FlatFile));
                return true;
            } catch (Exception exception) {
                exception.Log();
                return false;
            } finally {
                try {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, recursive: true);
                } catch (Exception exception) {
                    exception.Log();
                }
            }
        }

        private static string CreateWrapper(string priFile, string outDir)
        {
            var wrapper = new StringBuilder();
            // Let the .pri file see its own directory as project and output directory, as when it
            // was evaluated as the project itself; restore both before the mkspec features run.
            var priDir = Quote(Path.GetDirectoryName(priFile));
            wrapper.AppendLine("QTVSTOOLS_PRO_FILE_PWD = $$_PRO_FILE_PWD_");
            wrapper.AppendLine("QTVSTOOLS_OUT_PWD = $$OUT_PWD");
            wrapper.AppendLine($"_PRO_FILE_PWD_ = {priDir}");
            wrapper.AppendLine($"OUT_PWD = {priDir}");
            // The MSVC mkspecs set 'flat'; only the .pri file decides.
            wrapper.AppendLine("CONFIG -= flat");
            // qmake continues after a failed include; abort before writing incomplete results.
            wrapper.AppendLine($"!include({Quote(priFile)}): error(PriEvaluationFailed)");
            wrapper.AppendLine("_PRO_FILE_PWD_ = $$QTVSTOOLS_PRO_FILE_PWD");
            wrapper.AppendLine("OUT_PWD = $$QTVSTOOLS_OUT_PWD");
            foreach (var variable in Variables)
                wrapper.AppendLine($"write_file({Quote(Path.Combine(outDir, variable))}, {variable})");
            wrapper.AppendLine($"flat: write_file({Quote(Path.Combine(outDir, FlatFile))})");
            wrapper.AppendLine($"write_file({Quote(Path.Combine(outDir, DoneFile))})");
            return wrapper.ToString();
        }

        // qmake reads project files and writes write_file() output in the local 8-bit encoding,
        // i.e. the ANSI code page, which Encoding.Default is on .NET Framework.
        private static readonly Encoding QMakeEncoding = Encoding.Default;

        private static string Quote(string path) => $"\"{path.Replace('\\', '/')}\"";

        private static string[] ReadValues(string outDir, string variable)
        {
            var file = Path.Combine(outDir, variable);
            if (!File.Exists(file))
                return Array.Empty<string>();
            return File.ReadAllLines(file, QMakeEncoding)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToArray();
        }
    }
}
