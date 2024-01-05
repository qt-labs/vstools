/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.Core
{
    using QtVsTools.Core.Common;

    [TestClass]
    public class Test_Utils
    {
        private double NanosecondsPerTick => Math.Pow(10.0, 9.0) / Stopwatch.Frequency;

        [TestMethod]
        public void Test_LastIndexOfArray()
        {
            Assert.AreEqual(-1, Array.Empty<int>().LastIndexOfArray(Array.Empty<int>()));
            Assert.AreEqual(-1, Array.Empty<int>().LastIndexOfArray(new[] { 1, 2, 3 }));
            Assert.AreEqual(-1, new[] { 1, 2, 3 }.LastIndexOfArray(Array.Empty<int>()));
            Assert.AreEqual(-1, new[] { 1, 2, 3 }.LastIndexOfArray(new[] { 1, 2, 3, 4 }));

            var text = "the quick brown fox jumped over the lazy dog";
            var haystack = text.ToArray();
            var find = new Func<string, long>(what =>
            {
                var needle = what.ToArray();
                var time = Stopwatch.StartNew();
                var index = haystack.LastIndexOfArray(needle);
                time.Stop();
                Assert.IsTrue(index == text.LastIndexOf(what));
                return time.ElapsedTicks;
            });

            var test = new[] { "dog", "fox", "cat", "the", text }
                .ToDictionary(x => x, x => 0.0);
            var testRepeats = 100000;
            var testCases = test.Keys.ToList();
            foreach (var _ in Enumerable.Range(0, testRepeats))
                testCases.ForEach(x => test[x] += find(x));

            var min = test.Values.Min() / testRepeats;
            var avg = test.Values.Sum() / (test.Count * testRepeats);
            var max = test.Values.Max() / testRepeats;
            Debug.WriteLine($"  * Best case...{min * NanosecondsPerTick:F0} nsecs");
            Debug.WriteLine($"  * Average.....{avg * NanosecondsPerTick:F0} nsecs");
            Debug.WriteLine($"  * Worst case..{max * NanosecondsPerTick:F0} nsecs");
        }
    }
}
