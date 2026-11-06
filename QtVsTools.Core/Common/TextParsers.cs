// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace QtVsTools.Core.Common
{
    using QtVsTools.Common;
    using static SyntaxAnalysis.RegExpr;

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
    /// covered by tests.
    /// </summary>
    public static class TextParsers
    {
        private static LazyFactory StaticLazy { get; } = new();

        #region qmake -query / qtpaths -query

        /// <summary>
        /// Parses the output of 'qmake -query': one 'NAME:VALUE' per line. The last value wins
        /// if a name occurs more than once.
        /// </summary>
        public static Dictionary<string, string> ParseQueryProperties(string output)
        {
            return QueryPropertyParser
                .Parse(output)
                .GetValues<KeyValuePair<string, string>>("PROP")
                .GroupBy(x => x.Key)
                .Select(x => new { x.Key, x.Last().Value })
                .ToDictionary(property => property.Key, property => property.Value);
        }

        private static Parser QueryPropertyParser => StaticLazy.Get(() => QueryPropertyParser, () =>
        {
            var charSeparator = Char[':'];
            var charsName = CharSet[~(charSeparator + CharVertSpace)];
            var charsValue = CharSet[~CharVertSpace];

            var propertyName = new Token("NAME", charsName.Repeat(atLeast: 1));
            var propertyValue = new Token("VALUE", charsValue.Repeat());
            var property = new Token("PROP", propertyName & charSeparator & propertyValue)
                {
                    new Rule<KeyValuePair<string, string>>
                    {
                        Create("NAME", (string name) =>
                            new KeyValuePair<string, string>(name, "")),
                        Transform("VALUE", (KeyValuePair<string, string> prop, string value) =>
                            new KeyValuePair<string, string>(prop.Key, value))
                    }
                };
            var propertyLine = StartOfLine & property & CharVertSpace.Repeat();
            return propertyLine.Repeat().Render();
        });

        #endregion

        #region vcvars environment

        /// <summary>
        /// Parses the output of 'vcvars*.bat && set': one 'NAME=VALUE' per line, the value split
        /// at ';'. Names are compared case-insensitively.
        /// </summary>
        public static Dictionary<string, List<string>> ParseVcVarsEnvironment(string output)
        {
            return VcVarsParser.Parse(output)
                .GetValues<KeyValuePair<string, List<string>>>("env_var")
                .ToDictionary(envVar => envVar.Key, envVar => envVar.Value ?? new(),
                    Utils.CaseIgnorer);
        }

        private static Parser VcVarsParser => StaticLazy.Get(() => VcVarsParser, () =>
        {
            var tokenName = new Token("name", (~Chars["=\r\n"]).Repeat(atLeast: 1));
            var tokenValuePart = new Token("value_part", (~Chars[";\r\n"]).Repeat(atLeast: 1));
            var tokenValue = new Token("value", (tokenValuePart | Chars[';']).Repeat())
            {
                new Rule<List<string>>
                {
                    Capture(_ => new List<string>()),
                    Update("value_part", (List<string> parts, string part) => parts.Add(part))
                }
            };
            var tokenEnvVar = new Token("env_var", tokenName & "=" & tokenValue & LineBreak)
            {
                new Rule<KeyValuePair<string, List<string>>>
                {
                    Create("name", (string name)
                        => new KeyValuePair<string, List<string>>(name, null)),
                    Transform("value", (KeyValuePair<string, List<string>> prop, List<string> value)
                        => new KeyValuePair<string, List<string>>(prop.Key, value))
                }
            };
            return tokenEnvVar.Render();
        });

        #endregion

        #region Dev releases

        /// <summary>
        /// Returns the versions linked as 'href="X.Y.Z"' or 'href="X.Y.Z/"' in the HTML listing
        /// of the dev release download directory.
        /// </summary>
        public static IEnumerable<System.Version> ParseDevReleaseVersions(string html)
        {
            return DevReleaseParser.Parse(html).GetValues<System.Version>("VERSION");
        }

        private static Parser DevReleaseParser => StaticLazy.Get(() => DevReleaseParser, () =>
        {
            var tokenVersion = new Token("VERSION", Number & "." & Number & "." & Number)
            {
                new Rule<System.Version> { Capture(value => new System.Version(value)) }
            };
            var regexHrefVersion = "href=\"" & tokenVersion & Chars["/"].Optional() & "\"";
            var regexResponse = (regexHrefVersion | AnyChar | VertSpace).Repeat();
            return regexResponse.Render();
        });

        #endregion

        #region CMake

        /// <summary>
        /// Returns true if a CMakeLists.txt text contains 'find_package(Qt5 ...)' or
        /// 'find_package(Qt6 ...)', optionally as 'find_package(<name> NAMES Qt6 ...)'.
        /// </summary>
        public static bool HasQtFindPackage(string cmakeLists)
        {
            try {
                return CMakeListsParser.Parse(cmakeLists).Any();
            } catch (ParseErrorException) {
                return false;
            }
        }

        private static Parser CMakeListsParser => StaticLazy.Get(() => CMakeListsParser, () =>
        {
            var cmakeListsParser
                = WordBoundary & "find_package" & Space & "(" & Space
                & (Word & Space & "NAMES" & Space).Optional()
                & new Token("qt_version", "Qt" & CharSet['5', '6']) & ~CharWord;
            return cmakeListsParser.Render(HorizSpace);
        });

        #endregion

        #region WSL paths

        /// <summary>
        /// Converts a WSL path of the form '/mnt/c/dir/file' into 'c:\dir/file'. Returns null if
        /// the path is not of that form.
        /// </summary>
        public static string ConvertWslPath(string path)
        {
            WslPath wslPath;
            try {
                wslPath = WslPathParser.Parse(path)
                    .GetValues<WslPath>("WSLPATH").FirstOrDefault();
            } catch (ParseErrorException) {
                return null;
            }
            return wslPath == null ? null : $@"{wslPath.Drive}:\{wslPath.Path}";
        }

        private class WslPath
        {
            public string Drive;
            public string Path;
        }

        private static Parser WslPathParser => StaticLazy.Get(() => WslPathParser, () =>
            new Token("WSLPATH", SkipWs_Disable, StartOfFile
                & "/mnt/" & new Token("DRIVE", CharWord) & "/" & new Token("PATH", AnyChar.Repeat()))
            {
                new Rule<WslPath>
                {
                    Update("DRIVE", (WslPath wslPath, string drive) => wslPath.Drive = drive),
                    Update("PATH", (WslPath wslPath, string path) => wslPath.Path = path)
                }
            }
            .Render());

        #endregion

        #region QML debugger

        /// <summary>
        /// Extracts the QML debug connection parameters from command line arguments of the form
        ///
        ///   -qmljsdebugger=port:<port_num>[,port_max][,host:<address>][,file:<name>][,block]
        ///
        /// Returns null if the arguments contain no such parameters.
        /// </summary>
        public static QmlDebugConnectParams ParseQmlDebuggerArgs(string args)
        {
            try {
                return QmlDebuggerArgsParser
                    .Parse(args)
                    .GetValues<QmlDebugConnectParams>(QmlTokenId.ConnectParams)
                    .FirstOrDefault();
            } catch (ParseErrorException) {
                return null;
            }
        }

        private enum QmlTokenId { ConnectParams, Port, MaxPort, Host, File, Block }

        private static Parser QmlDebuggerArgsParser => StaticLazy.Get(() =>
            QmlDebuggerArgsParser, () => new Token(QmlTokenId.ConnectParams, RxConnectParams)
            {
                new Rule<QmlDebugConnectParams>
                {
                    Update(QmlTokenId.Port, (QmlDebugConnectParams conn, ushort n) => conn.Port = n),
                    Update(QmlTokenId.MaxPort,
                        (QmlDebugConnectParams conn, ushort n) => conn.MaxPort = n),
                    Update(QmlTokenId.Host, (QmlDebugConnectParams conn, string s) => conn.Host = s),
                    Update(QmlTokenId.File, (QmlDebugConnectParams conn, string s) => conn.File = s),
                    Update(QmlTokenId.Block, (QmlDebugConnectParams conn, bool b) => conn.Block = b)
                }
            }
            .Render());

        private static SyntaxAnalysis.RegExpr RxConnectParams =>
            "-qmljsdebugger="
            & ((RxPort | RxHost | RxFile) & RxDelim).Repeat(atLeast: 1) & RxBlock.Optional();

        private static SyntaxAnalysis.RegExpr RxPort =>
            "port:" & new Token(QmlTokenId.Port, CharDigit.Repeat(atLeast: 1))
            {
                new Rule<ushort> { Capture(ushort.Parse) }
            }
            & (
                "," & new Token(QmlTokenId.MaxPort, CharDigit.Repeat(atLeast: 1))
                {
                    new Rule<ushort> { Capture(ushort.Parse) }
                }
            ).Optional();

        private static SyntaxAnalysis.RegExpr RxHost =>
            "host:" & new Token(QmlTokenId.Host,
                (~CharSet[CharSpace, Chars[","]]).Repeat(atLeast: 1));

        private static SyntaxAnalysis.RegExpr RxFile =>
            "file:" & new Token(QmlTokenId.File,
                (~CharSet[CharSpace, Chars[","]]).Repeat(atLeast: 1));

        private static SyntaxAnalysis.RegExpr RxBlock =>
            new Token(QmlTokenId.Block, "block")
            {
                new Rule<bool> { Capture(_ => true) }
            };

        private static SyntaxAnalysis.RegExpr RxDelim =>
            ("," & !LookAhead[CharSpace | EndOfLine]) | LookAhead[CharSpace | EndOfLine];

        #endregion

        #region Qt/MSBuild projects

        /// <summary>
        /// Returns the number NNN of the first 'QtVS_vNNN' in a text, e.g. the Keyword of a
        /// Qt/MSBuild project. Returns null if there is none.
        /// </summary>
        public static int? ParseProjectFormatVersion(string text)
        {
            try {
                return ProjectFormatVersionParser.Parse(text ?? string.Empty)
                    .GetValues<int>("VERSION").First();
            } catch (ParseErrorException) {
                return null;
            }
        }

        private static Parser ProjectFormatVersionParser => StaticLazy.Get(() =>
            ProjectFormatVersionParser, () =>
            {
                var expr = "QtVS_v" & new Token("VERSION", Char['0', '9'].Repeat(3))
                {
                    new Rule<int> { Capture(int.Parse) }
                };
                return expr.Render();
            });

        private static readonly char[] SlashChars =
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };

        /// <summary>
        /// Returns a regex that matches any of the given paths, regardless of case and of the
        /// directory separators used.
        /// </summary>
        public static Regex CreatePathRegex(params string[] paths)
        {
            SyntaxAnalysis.RegExpr slash = CharSet[SlashChars];
            return paths
                .Select(findWhatPath => (SyntaxAnalysis.RegExpr)(
                    CaseInsensitive &
                    findWhatPath.Split(SlashChars, StringSplitOptions.RemoveEmptyEntries)
                        .Select(dirName => (SyntaxAnalysis.RegExpr)dirName)
                        .Aggregate((path, dirName) => path & slash & dirName)))
                .Aggregate((a, b) => a | b)
                .Render().Regex;
        }

        #endregion
    }
}
