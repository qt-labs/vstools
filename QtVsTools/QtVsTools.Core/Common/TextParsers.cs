// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace QtVsTools.Core.Common
{
    /// <summary>
    /// Connection parameters of a QML debug session, see
    /// <see cref="TextParsers.ParseQmlDebuggerArgs"/>.
    /// </summary>
    public class QmlDebugConnectParams
    {
        public ushort Port { get; set; }
        public ushort? MaxPort { get; set; }
        public string Host { get; set; }
        public string File { get; set; }
        public bool Block { get; set; }
    }

    /// <summary>
    /// Small text parsers used throughout the extension, in one place so that their behavior is
    /// covered by tests (see Test_TextParsers).
    /// </summary>
    public static class TextParsers
    {
        #region qmake -query / qtpaths -query

        // NAME:VALUE, the name up to the first colon
        private static readonly Regex QueryPropertyRegex = new(
            @"^(?<name>[^:\r\n]+):(?<value>[^\r\n]*)", RegexOptions.Multiline);

        /// <summary>
        /// Parses the output of 'qmake -query': one 'NAME:VALUE' per line. The last value wins
        /// if a name occurs more than once. Lines without a colon are skipped.
        /// </summary>
        public static Dictionary<string, string> ParseQueryProperties(string output)
        {
            var properties = new Dictionary<string, string>();
            foreach (Match match in QueryPropertyRegex.Matches(output ?? string.Empty))
                properties[match.Groups["name"].Value] = match.Groups["value"].Value;
            return properties;
        }

        #endregion

        #region vcvars environment

        // NAME=VALUE, terminated by a line break
        private static readonly Regex VcVarsRegex = new(
            @"^(?<name>[^=\r\n]+)=(?<value>[^\r\n]*)\r?\n", RegexOptions.Multiline);

        /// <summary>
        /// Parses the output of 'vcvars*.bat && set': one 'NAME=VALUE' per line, each line
        /// terminated by a line break, the value split at ';'. Names are compared
        /// case-insensitively; the last value wins if a name occurs more than once.
        /// </summary>
        public static Dictionary<string, List<string>> ParseVcVarsEnvironment(string output)
        {
            var env = new Dictionary<string, List<string>>(Utils.CaseIgnorer);
            foreach (Match match in VcVarsRegex.Matches(output ?? string.Empty)) {
                env[match.Groups["name"].Value] = match.Groups["value"].Value
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }
            return env;
        }

        #endregion

        #region Dev releases

        private static readonly Regex DevReleaseRegex = new(
            @"href=""(?<version>[0-9]+\.[0-9]+\.[0-9]+)/?""");

        /// <summary>
        /// Returns the versions linked as 'href="X.Y.Z"' or 'href="X.Y.Z/"' in the HTML listing
        /// of the dev release download directory.
        /// </summary>
        public static IEnumerable<System.Version> ParseDevReleaseVersions(string html)
        {
            return DevReleaseRegex.Matches(html ?? string.Empty)
                .Cast<Match>()
                .Select(match => System.Version.TryParse(match.Groups["version"].Value,
                    out var version) ? version : null)
                .Where(version => version != null)
                .ToList();
        }

        #endregion

        #region CMake

        // find_package(Qt6 ...) or find_package(<name> NAMES Qt6 ...)
        private static readonly Regex QtFindPackageRegex = new(
            @"\bfind_package\s*\(\s*(?:\w+\s+NAMES\s+)?Qt[56](?!\w)");

        /// <summary>
        /// Returns true if a CMakeLists.txt text contains 'find_package(Qt5 ...)' or
        /// 'find_package(Qt6 ...)', optionally as 'find_package(&lt;name&gt; NAMES Qt6 ...)'.
        /// </summary>
        public static bool HasQtFindPackage(string cmakeLists)
        {
            return QtFindPackageRegex.IsMatch(cmakeLists ?? string.Empty);
        }

        #endregion

        #region WSL paths

        private static readonly Regex WslPathRegex = new(@"\A/mnt/(?<drive>\w)/(?<path>.*)");

        /// <summary>
        /// Converts a WSL path of the form '/mnt/c/dir/file' into 'c:\dir/file'. Returns null if
        /// the path is not of that form.
        /// </summary>
        public static string ConvertWslPath(string path)
        {
            var match = WslPathRegex.Match(path ?? string.Empty);
            return match.Success
                ? $@"{match.Groups["drive"].Value}:\{match.Groups["path"].Value}"
                : null;
        }

        #endregion

        #region QML debugger

        // Stops at a quote, so that "-qmljsdebugger=...,block" in a quoted command line keeps its
        // last parameter.
        private static readonly Regex QmlDebuggerArgsRegex =
            new(@"-qmljsdebugger=(?<params>[^\s""]*)");

        /// <summary>
        /// Extracts the QML debug connection parameters from command line arguments of the form
        ///
        ///   -qmljsdebugger=port:&lt;port_num&gt;[,port_max][,host:&lt;address&gt;][,file:&lt;name&gt;][,block]
        ///
        /// 'port', 'host' and 'file' may appear in any order, 'block' only last. Parsing stops at
        /// the first unknown parameter. Returns null if there is no known parameter.
        /// </summary>
        public static QmlDebugConnectParams ParseQmlDebuggerArgs(string args)
        {
            var match = QmlDebuggerArgsRegex.Match(args ?? string.Empty);
            if (!match.Success)
                return null;

            var parts = match.Groups["params"].Value.Split(',');
            var connParams = new QmlDebugConnectParams();
            var known = 0;
            for (var i = 0; i < parts.Length; i++) {
                var part = parts[i];
                if (part.StartsWith("port:", StringComparison.Ordinal)
                    && TryParsePort(part.Substring("port:".Length), out var port)) {
                    connParams.Port = port;
                    if (i + 1 < parts.Length && TryParsePort(parts[i + 1], out var maxPort)) {
                        connParams.MaxPort = maxPort;
                        i++;
                    }
                } else if (part.StartsWith("host:", StringComparison.Ordinal)
                    && part.Length > "host:".Length) {
                    connParams.Host = part.Substring("host:".Length);
                } else if (part.StartsWith("file:", StringComparison.Ordinal)
                    && part.Length > "file:".Length) {
                    connParams.File = part.Substring("file:".Length);
                } else {
                    connParams.Block = known > 0 && part == "block";
                    break;
                }
                known++;
            }
            return known > 0 ? connParams : null;
        }

        private static bool TryParsePort(string text, out ushort port)
        {
            port = 0;
            return text.Length > 0 && text.All(c => c is >= '0' and <= '9')
                && ushort.TryParse(text, out port);
        }

        #endregion

        #region Qt/MSBuild projects

        // [0-9], not \d: \d also matches other Unicode digits, which int.Parse rejects.
        private static readonly Regex ProjectFormatVersionRegex =
            new(@"QtVS_v(?<version>[0-9]{3})");

        /// <summary>
        /// Returns the number NNN of the first 'QtVS_vNNN' in a text, e.g. the Keyword of a
        /// Qt/MSBuild project. Returns null if there is none.
        /// </summary>
        public static int? ParseProjectFormatVersion(string text)
        {
            var match = ProjectFormatVersionRegex.Match(text ?? string.Empty);
            return match.Success ? int.Parse(match.Groups["version"].Value) : null;
        }

        private static readonly Regex ConfigConditionRegex = new(
            @"'\$\(Configuration\)\|\$\(Platform\)'\s*==\s*'(?<configuration>\w*)\|(?<platform>\w*)'");

        /// <summary>
        /// Returns the platform of a project configuration condition of the form
        /// '$(Configuration)|$(Platform)'=='Debug|x64'. Returns null if the text has no such
        /// condition.
        /// </summary>
        public static string ParseConfigConditionPlatform(string condition)
        {
            var match = ConfigConditionRegex.Match(condition ?? string.Empty);
            return match.Success ? match.Groups["platform"].Value : null;
        }

        private static readonly char[] SlashChars =
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };

        /// <summary>
        /// Returns a regex that matches any of the given paths, regardless of case and of the
        /// directory separators used. Paths without a name, e.g. empty or only slashes, are
        /// ignored; without any other path, the regex matches nothing.
        /// </summary>
        public static Regex CreatePathRegex(params string[] paths)
        {
            var alternatives = paths
                .Select(path => path.Split(SlashChars, StringSplitOptions.RemoveEmptyEntries))
                .Where(parts => parts.Length > 0)
                .Select(parts => "(?:" + string.Join(@"[\\/]", parts.Select(Regex.Escape)) + ")")
                .ToList();
            // An empty alternative would match everywhere, and ReplacePath would then rewrite
            // every value of the project file.
            return new Regex(alternatives.Count > 0 ? string.Join("|", alternatives) : "(?!)",
                RegexOptions.IgnoreCase);
        }

        #endregion
    }
}
