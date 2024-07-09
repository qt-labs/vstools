// ************************************************************************************************
// Copyright (C) 2024 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
// ************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QtVsTools.Core
{
    using QtVsTools.Common;
    using static SyntaxAnalysis.RegExpr;

    public interface IQueryProcess
    {
        int Run();
        StringBuilder StdOutput { get; }
    }

    public abstract class QtBuildToolQuery
    {
        public static QtBuildToolQuery Get(string qtDir)
        {
            if (QtPaths.Exists(qtDir)) {
                var qtPaths = new QtPaths(qtDir)
                {
                    Version = true
                };

                if (qtPaths.Run() == 0 && qtPaths.StdOutput.Length > 0) {
                    var tokens = qtPaths.StdOutput.ToString().Trim(' ', '\r', '\n')
                        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length >= 2) {
                        if (System.Version.TryParse(tokens[1], out var v) && v.Major >= 2) {
                            return new QtPathsQuery()
                            {
                                QtDir = qtDir
                            };
                        }
                    }
                }
            }

            return QMake.Exists(qtDir) ? new QMakeQuery { QtDir = qtDir } : null;
        }

        public string this[string name] =>
            Properties.TryGetValue(name, out var value) ? value : null;

        protected abstract IQueryProcess CreateQueryProcess(string qtDir);

        private Dictionary<string, string> properties;
        private Dictionary<string, string> Properties => properties ??=
            new Func<Dictionary<string, string>>(() =>
            {
                var process = CreateQueryProcess(QtDir);
                if (process.Run() == 0 && process.StdOutput.Length > 0) {
                    return PropertyParser
                        .Parse(process.StdOutput.ToString())
                        .GetValues<KeyValuePair<string, string>>("PROP")
                        .GroupBy(x => x.Key)
                        .Select(x => new { x.Key, x.Last().Value })
                        .ToDictionary(property => property.Key, property => property.Value);
                }
                return new Dictionary<string, string>();
            }
        )();

        private string QtDir { get; set; }

        private static LazyFactory StaticLazy { get; } = new();
        private static Parser PropertyParser => StaticLazy.Get(() => PropertyParser, () =>
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
    }
}
