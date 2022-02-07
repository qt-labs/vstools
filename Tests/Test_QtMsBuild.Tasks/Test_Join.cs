/****************************************************************************
**
** Copyright (C) 2021 The Qt Company Ltd.
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using QtVsTools.QtMsBuild.Tasks;

namespace QtVsTools.Test.QtMsBuild.Tasks
{
    [TestClass]
    public class Test_Join
    {
        readonly ITaskItem[] LeftItems = new TaskItem[]
        {
                new TaskItem("A", new Dictionary<string, string> {
                    { "X", "foo" },
                    { "Y", "42" },
                }),
                new TaskItem("B", new Dictionary<string, string> {
                    { "X", "sna" },
                    { "Y", "99" },
                }),
                new TaskItem("C", new Dictionary<string, string> {
                    { "X", "bar" },
                    { "Y", "3.14159" },
                }),
        };
        readonly ITaskItem[] RightItems = new TaskItem[]
        {
                new TaskItem("A", new Dictionary<string, string> {
                    { "Z", "foo" },
                    { "Y", "99" },
                }),
                new TaskItem("B", new Dictionary<string, string> {
                    { "Z", "sna" },
                    { "Y", "2.71828" },
                }),
                new TaskItem("B", new Dictionary<string, string> {
                    { "Z", "bar" },
                    { "Y", "42" },
                }),
                new TaskItem("A", new Dictionary<string, string> {
                    { "Z", "bar" },
                    { "Y", "99" },
                }),
        };

        [TestMethod]
        public void Basic()
        {
            // JOIN ON 'Y'
            //
            //       Left            Right     -->   Result
            // ---------------  ---------------  ---------------
            //   X  | Y           Z  | Y           X  | Y  | Z
            // ---------------  ---------------  ---------------
            //  foo | 42         foo | 99         foo | 42 | bar
            //  sna | 99         sna | 2.71828    sna | 99 | foo
            //  bar | 3.14159    bar | 42         sna | 99 | bar
            // ---------------   bar | 99        ---------------
            //                  ---------------

            var criteria = new string[] { "Y" };
            ITaskItem[] result;
            Assert.IsTrue(
                Join.Execute(LeftItems, RightItems, out result, criteria));
            Assert.IsTrue(result != null && result.Length == 3);

            Assert.IsTrue(result[0].GetMetadata("X") == "foo");
            Assert.IsTrue(result[0].GetMetadata("Y") == "42");
            Assert.IsTrue(result[0].GetMetadata("Z") == "bar");

            Assert.IsTrue(result[1].GetMetadata("X") == "sna");
            Assert.IsTrue(result[1].GetMetadata("Y") == "99");
            Assert.IsTrue(result[1].GetMetadata("Z") == "foo");

            Assert.IsTrue(result[2].GetMetadata("X") == "sna");
            Assert.IsTrue(result[2].GetMetadata("Y") == "99");
            Assert.IsTrue(result[2].GetMetadata("Z") == "bar");
        }

        [TestMethod]
        public void RowNumber()
        {
            // JOIN ON 'ROW_NUMBER'
            //
            //        Left                 Right       -->        Result
            // -------------------  -------------------  ------------------------
            //  # |  X  | Y          # |  Z  | Y          # |  X  | Y       | Z
            // -------------------  -------------------  ------------------------
            //  0 | foo | 42         0 | foo | 99         0 | foo | 42      | foo
            //  1 | sna | 99         1 | sna | 2.71828    1 | sna | 99      | sna
            //  2 | bar | 3.14159    2 | bar | 42         2 | bar | 3.14159 | bar
            // -------------------   3 | bar | 99        ------------------------
            //                      -------------------

            var criteria = new string[] { "ROW_NUMBER" };
            ITaskItem[] result;
            Assert.IsTrue(
                Join.Execute(LeftItems, RightItems, out result, criteria));
            Assert.IsTrue(result != null && result.Length == 3);

            Assert.IsTrue(result[0].GetMetadata("X") == "foo");
            Assert.IsTrue(result[0].GetMetadata("Y") == "42");
            Assert.IsTrue(result[0].GetMetadata("Z") == "foo");

            Assert.IsTrue(result[1].GetMetadata("X") == "sna");
            Assert.IsTrue(result[1].GetMetadata("Y") == "99");
            Assert.IsTrue(result[1].GetMetadata("Z") == "sna");

            Assert.IsTrue(result[2].GetMetadata("X") == "bar");
            Assert.IsTrue(result[2].GetMetadata("Y") == "3.14159");
            Assert.IsTrue(result[2].GetMetadata("Z") == "bar");
        }

        [TestMethod]
        public void Empty()
        {
            // JOIN ON 'ROW_NUMBER, Y'
            //
            //        Left                 Right       --> Result
            // -------------------  -------------------  -----------
            //  # |  X  | Y          # |  Z  | Y           (empty)
            // -------------------  -------------------  -----------
            //  0 | foo | 42         0 | foo | 99
            //  1 | sna | 99         1 | sna | 2.71828
            //  2 | bar | 3.14159    2 | bar | 42
            // -------------------   3 | bar | 99
            //                      -------------------

            var criteria = new string[] { "ROW_NUMBER", "Y" };
            ITaskItem[] result;
            Assert.IsTrue(
                Join.Execute(LeftItems, RightItems, out result, criteria));
            Assert.IsTrue(result != null && result.Length == 0);
        }

        [TestMethod]
        public void MultipleCriteria()
        {
            // JOIN ON 'ROW_NUMBER, Y'
            //
            //        Left                 Right       --> Result
            // -------------------  -------------------  ----------------
            //  # |  X  | Y          # |  Z  | Y           X  | Y  | Z
            // -------------------  -------------------  ----------------
            //  0 | foo | 42         0 | foo | 99         zzz | 99 | bar
            //  1 | sna | 99         1 | sna | 2.71828
            //  2 | bar | 3.14159    2 | bar | 42
            //  3 | zzz | 99         3 | bar | 99
            // -------------------  -------------------

            var newLeftItems = LeftItems
                .Append(new TaskItem("D", new Dictionary<string, string> {
                    { "X", "zzz" },
                    { "Y", "99" },
                }))
                .ToArray();

            var criteria = new string[] { "ROW_NUMBER", "Y" };
            ITaskItem[] result;
            Assert.IsTrue(
                Join.Execute(newLeftItems, RightItems, out result, criteria));
            Assert.IsTrue(result != null && result.Length == 1);

            Assert.IsTrue(result[0].GetMetadata("X") == "zzz");
            Assert.IsTrue(result[0].GetMetadata("Y") == "99");
            Assert.IsTrue(result[0].GetMetadata("Z") == "bar");
        }

        [TestMethod]
        public void Default()
        {
            // JOIN ON '' <=> JOIN ON 'Identity'
            //
            //  Left                   Right               --> Result
            // ---------------------  ---------------------  ----------------------
            //  Id. |  X  | Y          Id. |  Z  | Y          Id. |  X  | Y  | Z
            // ---------------------  ---------------------  ----------------------
            //   A  | foo | 42          A  | foo | 99          A  | foo | 42 | foo
            //   B  | sna | 99          B  | sna | 2.71828     A  | foo | 42 | bar
            //   C  | bar | 3.14159     B  | bar | 42          B  | sna | 99 | sna
            // ---------------------    A  | bar | 99          B  | bar | 99 | bar
            //                        ---------------------  ----------------------

            ITaskItem[] result;
            Assert.IsTrue(
                Join.Execute(LeftItems, RightItems, out result));
            Assert.IsTrue(result != null && result.Length == 4);

            Assert.IsTrue(result[0].GetMetadata("X") == "foo");
            Assert.IsTrue(result[0].GetMetadata("Y") == "42");
            Assert.IsTrue(result[0].GetMetadata("Z") == "foo");

            Assert.IsTrue(result[1].GetMetadata("X") == "foo");
            Assert.IsTrue(result[1].GetMetadata("Y") == "42");
            Assert.IsTrue(result[1].GetMetadata("Z") == "bar");

            Assert.IsTrue(result[2].GetMetadata("X") == "sna");
            Assert.IsTrue(result[2].GetMetadata("Y") == "99");
            Assert.IsTrue(result[2].GetMetadata("Z") == "sna");

            Assert.IsTrue(result[3].GetMetadata("X") == "sna");
            Assert.IsTrue(result[3].GetMetadata("Y") == "99");
            Assert.IsTrue(result[3].GetMetadata("Z") == "bar");
        }
    }
}
