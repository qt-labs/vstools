// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Linq;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Tasks
{
    using QtVsTools.QtMsBuild.Tasks;

    [TestClass]
    public class Test_Join
    {
        private readonly ITaskItem[] leftItems = new TaskItem[]
        {
                new("A", new Dictionary<string, string> {
                    { "X", "foo" },
                    { "Y", "42" }
                }),
                new("B", new Dictionary<string, string> {
                    { "X", "sna" },
                    { "Y", "99" }
                }),
                new("C", new Dictionary<string, string> {
                    { "X", "bar" },
                    { "Y", "3.14159" }
                })
        };

        private readonly ITaskItem[] rightItems = new TaskItem[]
        {
                new("A", new Dictionary<string, string> {
                    { "Z", "foo" },
                    { "Y", "99" }
                }),
                new("B", new Dictionary<string, string> {
                    { "Z", "sna" },
                    { "Y", "2.71828" }
                }),
                new("B", new Dictionary<string, string> {
                    { "Z", "bar" },
                    { "Y", "42" }
                }),
                new("A", new Dictionary<string, string> {
                    { "Z", "bar" },
                    { "Y", "99" }
                })
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

            var criteria = new[] { "Y" };
            Assert.IsTrue(
                Join.Execute(leftItems, rightItems, out var result, criteria));
            Assert.IsNotNull(result);
            Assert.HasCount(3, result);

            Assert.AreEqual("foo", result[0].GetMetadata("X"));
            Assert.AreEqual("42", result[0].GetMetadata("Y"));
            Assert.AreEqual("bar", result[0].GetMetadata("Z"));

            Assert.AreEqual("sna", result[1].GetMetadata("X"));
            Assert.AreEqual("99", result[1].GetMetadata("Y"));
            Assert.AreEqual("foo", result[1].GetMetadata("Z"));

            Assert.AreEqual("sna", result[2].GetMetadata("X"));
            Assert.AreEqual("99", result[2].GetMetadata("Y"));
            Assert.AreEqual("bar", result[2].GetMetadata("Z"));
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

            var criteria = new[] { "ROW_NUMBER" };
            Assert.IsTrue(
                Join.Execute(leftItems, rightItems, out var result, criteria));
            Assert.IsNotNull(result);
            Assert.HasCount(3, result);

            Assert.AreEqual("foo", result[0].GetMetadata("X"));
            Assert.AreEqual("42", result[0].GetMetadata("Y"));
            Assert.AreEqual("foo", result[0].GetMetadata("Z"));

            Assert.AreEqual("sna", result[1].GetMetadata("X"));
            Assert.AreEqual("99", result[1].GetMetadata("Y"));
            Assert.AreEqual("sna", result[1].GetMetadata("Z"));

            Assert.AreEqual("bar", result[2].GetMetadata("X"));
            Assert.AreEqual("3.14159", result[2].GetMetadata("Y"));
            Assert.AreEqual("bar", result[2].GetMetadata("Z"));
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

            var criteria = new[] { "ROW_NUMBER", "Y" };
            Assert.IsTrue(
                Join.Execute(leftItems, rightItems, out var result, criteria));
            Assert.IsNotNull(result);
            Assert.HasCount(0, result);
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

            var newLeftItems = leftItems
                .Append(new TaskItem("D", new Dictionary<string, string> {
                    { "X", "zzz" },
                    { "Y", "99" }
                }))
                .ToArray();

            var criteria = new[] { "ROW_NUMBER", "Y" };
            Assert.IsTrue(
                Join.Execute(newLeftItems, rightItems, out var result, criteria));
            Assert.IsNotNull(result);
            Assert.HasCount(1, result);

            Assert.AreEqual("zzz", result[0].GetMetadata("X"));
            Assert.AreEqual("99", result[0].GetMetadata("Y"));
            Assert.AreEqual("bar", result[0].GetMetadata("Z"));
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

            Assert.IsTrue(
                Join.Execute(leftItems, rightItems, out var result));
            Assert.IsNotNull(result);
            Assert.HasCount(4, result);

            Assert.AreEqual("foo", result[0].GetMetadata("X"));
            Assert.AreEqual("42", result[0].GetMetadata("Y"));
            Assert.AreEqual("foo", result[0].GetMetadata("Z"));

            Assert.AreEqual("foo", result[1].GetMetadata("X"));
            Assert.AreEqual("42", result[1].GetMetadata("Y"));
            Assert.AreEqual("bar", result[1].GetMetadata("Z"));

            Assert.AreEqual("sna", result[2].GetMetadata("X"));
            Assert.AreEqual("99", result[2].GetMetadata("Y"));
            Assert.AreEqual("sna", result[2].GetMetadata("Z"));

            Assert.AreEqual("sna", result[3].GetMetadata("X"));
            Assert.AreEqual("99", result[3].GetMetadata("Y"));
            Assert.AreEqual("bar", result[3].GetMetadata("Z"));
        }
    }
}
