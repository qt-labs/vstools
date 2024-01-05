/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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
