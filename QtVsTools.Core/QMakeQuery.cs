/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace QtVsTools.Core
{
    using static SyntaxAnalysis.RegExpr;

    public class QMakeQuery
    {
        private readonly string qtDir;

        private class QMakeProcess : QMake
        {
            public readonly StringBuilder StdOutput;

            public QMakeProcess(string qtDir, EnvDTE.DTE dte = null)
                : base(qtDir, dte)
            {
                Query = " ";
                StdOutput = new StringBuilder();
            }

            protected override void OutMsg(Process qmakeProc, string msg)
            {
                StdOutput.AppendLine(msg);
            }

            protected override void InfoStart(Process qmakeProc)
            {
                base.InfoStart(qmakeProc);
                InfoMsg(qmakeProc, "Querying persistent properties");
            }
        }

        public QMakeQuery(string qtDir)
        {
            this.qtDir = qtDir;
        }

        public Dictionary<string, string> QueryAllValues()
        {
            var qmake = new QMakeProcess(qtDir);
            if (qmake.Run() == 0 && qmake.StdOutput.Length > 0) {
                return PropertyParser
                    .Parse(qmake.StdOutput.ToString())
                    .GetValues<KeyValuePair<string, string>>("PROP")
                    .GroupBy(x => x.Key)
                    .Select(x => new { x.Key, x.Last().Value })
                    .ToDictionary(property => property.Key, property => property.Value);
            }
            return new Dictionary<string, string>();
        }

        public string this[string name]
        {
            get => Properties.TryGetValue(name, out var value) ? value : null;
        }

        Dictionary<string, string> _Properties;
        Dictionary<string, string> Properties => _Properties ??= QueryAllValues();

        Parser _PropertyParser;
        Parser PropertyParser
        {
            get
            {
                if (_PropertyParser != null)
                    return _PropertyParser;

                var charSeparator = Char[':'];
                var charsName = CharSet[~(charSeparator + CharVertSpace)];
                var charsValue = CharSet[~CharVertSpace];

                var propertyName = new Token("NAME", charsName.Repeat(atLeast: 1));
                var propertyValue = new Token("VALUE", charsValue.Repeat());
                var property = new Token("PROP", propertyName & charSeparator & propertyValue)
                {
                    new Rule<KeyValuePair<string, string>>
                    {
                        Create("NAME", (string name)
                            => new KeyValuePair<string, string>(name, string.Empty)),

                        Transform("VALUE", (KeyValuePair<string, string> prop, string value)
                            => new KeyValuePair<string, string>(prop.Key, value))
                    }
                };
                var propertyLine = StartOfLine & property & CharVertSpace.Repeat();
                return _PropertyParser = propertyLine.Repeat().Render();
            }
        }
    }
}
