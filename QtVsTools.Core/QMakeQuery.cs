/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using QtVsTools.SyntaxAnalysis;

namespace QtVsTools.Core
{
    using static RegExpr;

    public class QMakeQuery : QMake
    {
        public QMakeQuery(VersionInformation vi) : base(vi)
        { }

        StringBuilder stdOutput;
        protected override void OutMsg(string msg)
        {
            stdOutput.AppendLine(msg);
        }

        protected override void InfoStart(Process qmakeProc)
        {
            base.InfoStart(qmakeProc);
            InfoMsg("--- qmake: Querying persistent properties");
        }

        public Dictionary<string, string> QueryAllValues()
        {
            stdOutput = new StringBuilder();
            Query = " ";

            if (Run() == 0 && stdOutput.Length > 0) {
                return PropertyParser
                    .Parse(stdOutput.ToString())
                    .GetValues<KeyValuePair<string, string>>("PROP")
                    .GroupBy(x => x.Key)
                    .Select(x => new { x.Key, Value = x.Last().Value })
                    .ToDictionary(property => property.Key, property => property.Value);
            } else {
                return new Dictionary<string, string>();
            }
        }

        public string this[string name]
        {
            get
            {
                string value = string.Empty;
                if (Properties.TryGetValue(name, out value))
                    return value;
                else
                    return null;
            }
        }

        Dictionary<string, string> _Properties;
        Dictionary<string, string> Properties => _Properties ?? (_Properties = QueryAllValues());

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
