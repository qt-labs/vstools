/****************************************************************************
**
** Copyright (C) 2019 The Qt Company Ltd.
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

using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.RegExpr
{
    using QtVsTest.Macros;

    [TestClass]
    public class Test_MacroParser
    {
        readonly MacroParser Parser = MacroParser.Get();

        [TestMethod]
        public void TestMacro()
        {
            string testInput =
                "//# wait 15000 Assembly QtVsTools => GetAssembly(\"QtVsTools\")" + "\r\n" +
                "var VsixType = QtVsTools.GetType(\"QtVsTools.Vsix\");" + "\r\n" +
                "var VsixInstance = VsixType.GetProperty(\"Instance\"," + "\r\n" +
                "    BindingFlags.Public | BindingFlags.Static);" + "\r\n" +
                "//# wait 15000 object Vsix => VsixInstance.GetValue(null)" + "\r\n" +
                "Result = \"(ok)\";";

            var macroLines = Parser.Parse(testInput);

            Debug.Assert(macroLines != null);
            Debug.Assert(macroLines.Count() == 6);
            Debug.Assert(macroLines.Skip(0).First() is Statement);
            Debug.Assert(macroLines.Skip(1).First() is CodeLine);
            Debug.Assert(macroLines.Skip(2).First() is CodeLine);
            Debug.Assert(macroLines.Skip(3).First() is CodeLine);
            Debug.Assert(macroLines.Skip(4).First() is Statement);
            Debug.Assert(macroLines.Skip(5).First() is CodeLine);
        }
    }
}
