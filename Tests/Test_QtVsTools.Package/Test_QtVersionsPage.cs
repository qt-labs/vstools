/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace QtVsTools.Test.Package
{
    [TestClass]
    public class Test_QtVersionsPage
    {
        // UI automation property conditions
        string SetGlobals => @"
//# using System.IO
var elementSubtree = (TreeScope.Element | TreeScope.Subtree);
var isButton = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
var isDataGrid = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataGrid);
var isEdit = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
var isText = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text);
var isWindow = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);";

        // Open menu: Tools > Options...
        string OpenVsOptions => @"
//# ui context VSROOT => ""MenuBar"", ""Tools""
//# ui pattern Invoke
//# ui context => ""Options...""
//# ui pattern Invoke";

        // Select options: page Qt > Versions
        string SelectQtVersionsPage => @"
//# ui context VSROOT => ""Options"", ""Qt""
//# ui pattern ExpandCollapse qtOptions
qtOptions.Expand();
//# ui context => ""Versions""
//# ui pattern SelectionItem qtVersions
qtVersions.Select();";

        // Get reference to data grid with Qt versions
        string GetQtVersionsTable => @"
//# ui context VSROOT => ""Options""
//# ui find => elementSubtree, isDataGrid
//# ui pattern Grid qtVersionsTable";

        // Add new row to versions table
        string AddNewRow => @"
var lastRow = qtVersionsTable.Current.RowCount - 1;
UiContext = qtVersionsTable.GetItem(lastRow, 1);
//# ui find => elementSubtree, isButton
//# ui pattern Invoke
{
    //# ui context VSROOT => ""Options""
    //# ui find => elementSubtree, isDataGrid
    //# ui pattern Grid qtVersionsTableAux
    qtVersionsTable = qtVersionsTableAux;
}
UiContext = qtVersionsTable.GetItem(lastRow, 1);
//# ui find => elementSubtree, isEdit
//# ui pattern Value newVersionName
newVersionName.SetValue(""TEST_"" + Path.GetRandomFileName());";

        // Set UI context to the path field of the new row
        string SelectNewRowPath => @"
UiContext = qtVersionsTable.GetItem(lastRow, 3);
//# ui find => elementSubtree, isEdit";

        // Save changes to the versions table and close the VS options dialog
        //  * Any error message will be copied to 'Result'
        string SaveChanges => @"
//# ui context VSROOT => ""Options"", ""OK""
//# ui pattern Invoke
//# thread ui
try {
    //# ui context VSROOT 100 => ""Options""
    //# ui find => elementSubtree, isWindow
} catch (TimeoutException) {
    return;
}
if (UiContext == null)
    return;
//# ui find => elementSubtree, isText
Result = UiContext.Current.Name;
//# ui context VSROOT => ""Options""
//# ui find => elementSubtree, isWindow
//# ui context => ""OK""
//# ui pattern Invoke
//# ui context VSROOT => ""Options"", ""Cancel""
//# ui pattern Invoke";

        // Add new variable 'qtPath' with the path to the Qt version in the top row
        //  * This is assumed to be a valid path to an existing Qt version
        string GetFirstRowPath => @"
if (qtVersionsTable.Current.RowCount <= 1) {
    Result = MACRO_ERROR_MSG(""No Qt version registered."");
    return;
}
UiContext = qtVersionsTable.GetItem(0, 3);
//# ui find => elementSubtree, isEdit
//# ui pattern Value path
string qtPath = path.Current.Value;
if (Path.GetFileName(qtPath).Equals(""qmake.exe"", StringComparison.InvariantCultureIgnoreCase))
    qtPath = Path.GetDirectoryName(qtPath);
if (Path.GetFileName(qtPath).Equals(""bin"", StringComparison.InvariantCultureIgnoreCase))
    qtPath = Path.GetDirectoryName(qtPath);";

        [TestMethod]
        // Add new (empty) row => error
        public void Test_EmptyVersion()
        {
            string result;
            using (var vs = QtVsTestClient.Attach()) {
                result = vs.RunMacro($@"
                    {SetGlobals}
                    {OpenVsOptions}
                    {SelectQtVersionsPage}
                    {GetQtVersionsTable}
                    {AddNewRow}
                    {SaveChanges}");
            }
            Assert.IsTrue(result.Contains("Invalid Qt versions"), result);
        }

        [TestMethod]
        // Add new row and copy the path from the top row => OK
        public void Test_AddNewVersion()
        {
            string result;
            using (var vs = QtVsTestClient.Attach()) {
                result = vs.RunMacro($@"
                    {SetGlobals}
                    {OpenVsOptions}
                    {SelectQtVersionsPage}
                    {GetQtVersionsTable}
                    {GetFirstRowPath}
                    {AddNewRow}
                    {SelectNewRowPath}
                    //# ui pattern Value newVersionPath
                    newVersionPath.SetValue(qtPath);
                    {SaveChanges}");
            }
            Assert.IsTrue(result.StartsWith(QtVsTestClient.MacroOk), result);
        }

        [TestMethod]
        // Add new row, copy the path from the top row, and append "qmake.exe" => OK
        public void Test_AddBinToPath()
        {
            string result;
            using (var vs = QtVsTestClient.Attach()) {
                result = vs.RunMacro($@"
                    {SetGlobals}
                    {OpenVsOptions}
                    {SelectQtVersionsPage}
                    {GetQtVersionsTable}
                    {GetFirstRowPath}
                    {AddNewRow}
                    {SelectNewRowPath}
                    //# ui pattern Value newVersionPath
                    newVersionPath.SetValue(Path.Combine(qtPath, ""bin""));
                    {SaveChanges}");
            }
            Assert.IsTrue(result.StartsWith(QtVsTestClient.MacroOk), result);
        }

        [TestMethod]
        // Add new row, copy the path from the top row, and append "bin\qmake.exe" => OK
        public void Test_AddBinQMakeToPath()
        {
            string result;
            using (var vs = QtVsTestClient.Attach()) {
                result = vs.RunMacro($@"
                    {SetGlobals}
                    {OpenVsOptions}
                    {SelectQtVersionsPage}
                    {GetQtVersionsTable}
                    {GetFirstRowPath}
                    {AddNewRow}
                    {SelectNewRowPath}
                    //# ui pattern Value newVersionPath
                    newVersionPath.SetValue(Path.Combine(qtPath, ""bin"", ""qmake.exe""));
                    {SaveChanges}");
            }
            Assert.IsTrue(result.StartsWith(QtVsTestClient.MacroOk), result);
        }

        [TestMethod]
        // Add new row, copy the path from the top row, and append "include" => ERROR
        public void Test_AddIncludeToPath()
        {
            string result;
            using (var vs = QtVsTestClient.Attach()) {
                result = vs.RunMacro($@"
                    {SetGlobals}
                    {OpenVsOptions}
                    {SelectQtVersionsPage}
                    {GetQtVersionsTable}
                    {GetFirstRowPath}
                    {AddNewRow}
                    {SelectNewRowPath}
                    //# ui pattern Value newVersionPath
                    newVersionPath.SetValue(Path.Combine(qtPath, ""include""));
                    {SaveChanges}");
            }
            Assert.IsTrue(result.Contains("Invalid Qt versions"), result);
        }

        [ClassCleanup]
        // Remove registry keys created during tests
        public static void RemoveTestKeys()
        {
            var qtVersions = Registry.CurrentUser
                .OpenSubKey(@"Software\Digia\Versions", writable: true);
            using (qtVersions) {
                var allVersions = qtVersions.GetSubKeyNames();
                var testVersions = allVersions.Where(k => k.StartsWith("TEST"));
                foreach (var testVersion in testVersions)
                    qtVersions.DeleteSubKey(testVersion);
                qtVersions.Close();
            }
        }
    }
}
