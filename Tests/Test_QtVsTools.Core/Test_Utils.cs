/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.Core
{
    using static QtVsTools.Core.Common.Utils;

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

        [TestMethod]
        public void Test_LogFile()
        {
            Assert.ThrowsException<ArgumentException>(() => new LogFile("<foo>", 0, 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LogFile("foo", 0, 20));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LogFile("foo", 10, 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LogFile("foo", 10, 20));

            string logFilePath = @$"{Path.GetTempPath()}\logtest.txt";
            if (File.Exists(logFilePath))
                File.WriteAllBytes(logFilePath, Array.Empty<byte>());

            var sTx = "[";
            var eTx = "]\r\n";
            Func<int, string> logTx = (int i) => $"{sTx}Log entry #{i}{eTx}";
            var log = new LogFile(logFilePath, 50, 40, sTx);

            log.Write(logTx(1));
            Assert.AreEqual(logTx(1), File.ReadAllText(logFilePath));

            log.Write(logTx(2));
            Assert.AreEqual(logTx(1) + logTx(2), File.ReadAllText(logFilePath));

            log.Write(logTx(3));
            Assert.AreEqual(logTx(1) + logTx(2) + logTx(3), File.ReadAllText(logFilePath));

            log.Write(logTx(4));
            Assert.AreEqual(logTx(1) + logTx(2) + logTx(3) + logTx(4),
                File.ReadAllText(logFilePath));

            log.Write(logTx(5));
            Assert.AreEqual(logTx(1) + logTx(2) + logTx(3) + logTx(4) + logTx(5),
                File.ReadAllText(logFilePath));
        }
    }
}
