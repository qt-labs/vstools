/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.PriorityQueue
{
    [TestClass]
    public class Test_PriorityQueue
    {
        [TestMethod]
        public void TestEnqueueWithPriority()
        {
            var q = new PriorityQueue<string, int>();
            q.Enqueue("c", 30);
            q.Enqueue("a", 13);
            q.Enqueue("d", 47);
            q.Enqueue("b", 28);
            Assert.IsTrue(string.Join("", q) == "abcd");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestEnqueueWithSamePriority()
        {
            var q = new PriorityQueue<string, int>();
            q.Enqueue("a", 1);
            q.Enqueue("a", 1);
            Assert.IsTrue(string.Join("", q) == "a");
            q.Enqueue("b", 1);
        }

        [TestMethod]
        public void TestEnqueueContains()
        {
            var q = new PunisherQueue<string>();
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            Assert.IsTrue(q.Contains("a"));
            Assert.IsTrue(q.Contains("b"));
            Assert.IsTrue(q.Contains("c"));
            Assert.IsTrue(string.Join("", q) == "abc");
        }

        [TestMethod]
        public void TestEnqueueTwice()
        {
            var q = new PunisherQueue<string>();
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("a");
            q.Enqueue("c");
            q.Enqueue("b");
            Assert.IsTrue(string.Join("", q) == "acb");
        }

        [TestMethod]
        public void TestTryPeek()
        {
            var q = new PunisherQueue<string>();
            Assert.IsTrue(!q.TryPeek(out _));
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            Assert.IsTrue(q.TryPeek(out string s) && s == "a");
            Assert.IsTrue(string.Join("", q) == "abc");
        }

        [TestMethod]
        public void TestPeek()
        {
            var q = new PunisherQueue<string>();
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            Assert.IsTrue(q.Peek() == "a");
            Assert.IsTrue(string.Join("", q) == "abc");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestPeekEmpty()
        {
            var q = new PunisherQueue<string>();
            q.Peek();
        }

        [TestMethod]
        public void TestTryDequeue()
        {
            var q = new PunisherQueue<string>();
            Assert.IsTrue(!q.TryDequeue(out _));
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            Assert.IsTrue(q.TryDequeue(out string s) && s == "a");
            Assert.IsTrue(string.Join("", q) == "bc");
        }

        [TestMethod]
        public void TestDequeue()
        {
            var q = new PunisherQueue<string>();
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            Assert.IsTrue(q.Dequeue() == "a");
            Assert.IsTrue(string.Join("", q) == "bc");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestDequeueEmpty()
        {
            var q = new PunisherQueue<string>();
            q.Dequeue();
        }

        [TestMethod]
        public void TestClear()
        {
            var q = new PunisherQueue<string>();
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            q.Clear();
            q.Enqueue("x");
            q.Enqueue("y");
            q.Enqueue("z");
            Assert.IsTrue(string.Join("", q) == "xyz");
        }

        [TestMethod]
        public void TestConcurrency()
        {
            var q = new PunisherQueue<string>();
            int n = 0;
            _ = Task.Run(() =>
            {
                for (int i = 0; i < 10000; ++i) {
                    q.Enqueue(Path.GetRandomFileName());
                    ++n;
                    Thread.Yield();
                }
            });
            for (int i = 0; i < 10000; ++i) {
                if (!q.TryDequeue(out _))
                    --i;
                --n;
                Thread.Yield();
            }
            if (n == 0)
                Assert.Inconclusive();
            Assert.IsTrue(q.Count() == 0);
        }

        [TestMethod]
        public void TestGetItemKey()
        {
            var q = new PunisherQueue<string>(item =>
            {
                switch (item) {
                case "a":
                case "x":
                    return "ax";
                case "b":
                case "y":
                    return "by";
                case "c":
                case "z":
                    return "cz";
                default:
                    return item;
                }
            });
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            q.Enqueue("x");
            q.Enqueue("z");
            q.Enqueue("w");
            Assert.IsTrue(string.Join("", q) == "bxzw");
        }

        [TestMethod]
        public void TestRemove()
        {
            var q = new PunisherQueue<string>();
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            q.Remove("b");
            Assert.IsTrue(string.Join("", q) == "ac");
        }
    }
}
