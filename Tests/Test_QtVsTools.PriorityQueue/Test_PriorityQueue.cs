// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.PriorityQueue
{
    [TestClass]
    public class Test_PriorityQueue
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestEnqueueWithPriority()
        {
            var q = new PriorityQueue<string, int>();
            q.Enqueue("c", 30);
            q.Enqueue("a", 13);
            q.Enqueue("d", 47);
            q.Enqueue("b", 28);
            Assert.AreEqual("abcd", string.Join("", q));
        }

        [TestMethod]
        public void TestEnqueueWithSamePriority()
        {
            var q = new PriorityQueue<string, int>();
            q.Enqueue("a", 1);
            q.Enqueue("a", 1);
            Assert.AreEqual("a", string.Join("", q));
            Assert.ThrowsExactly<InvalidOperationException>(() => q.Enqueue("b", 1));
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
            Assert.AreEqual("abc", string.Join("", q));
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
            Assert.AreEqual("acb", string.Join("", q));
        }

        [TestMethod]
        public void TestTryPeek()
        {
            var q = new PunisherQueue<string>();
            Assert.IsFalse(q.TryPeek(out _));
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            Assert.IsTrue(q.TryPeek(out var s) && s == "a");
            Assert.AreEqual("abc", string.Join("", q));
        }

        [TestMethod]
        public void TestPeek()
        {
            var q = new PunisherQueue<string>();
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            Assert.AreEqual("a", q.Peek());
            Assert.AreEqual("abc", string.Join("", q));
        }

        [TestMethod]
        public void TestPeekEmpty()
        {
            var q = new PunisherQueue<string>();
            Assert.ThrowsExactly<InvalidOperationException>(() => q.Peek());
        }

        [TestMethod]
        public void TestTryDequeue()
        {
            var q = new PunisherQueue<string>();
            Assert.IsFalse(q.TryDequeue(out _));
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            Assert.IsTrue(q.TryDequeue(out var s) && s == "a");
            Assert.AreEqual("bc", string.Join("", q));
        }

        [TestMethod]
        public void TestDequeue()
        {
            var q = new PunisherQueue<string>();
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            Assert.AreEqual("a", q.Dequeue());
            Assert.AreEqual("bc", string.Join("", q));
        }

        [TestMethod]
        public void TestDequeueEmpty()
        {
            var q = new PunisherQueue<string>();
            Assert.ThrowsExactly<InvalidOperationException>(() => q.Dequeue());
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
            Assert.AreEqual("xyz", string.Join("", q));
        }

        [TestMethod]
        public void TestConcurrency()
        {
            var q = new PunisherQueue<string>();
            var n = 0;
            _ = Task.Run(() =>
            {
                for (var i = 0; i < 10000; ++i) {
                    q.Enqueue(Path.GetRandomFileName());
                    ++n;
                    Thread.Yield();
                }
            }, TestContext.CancellationToken);
            for (var i = 0; i < 10000; ++i) {
                if (!q.TryDequeue(out _))
                    --i;
                --n;
                Thread.Yield();
            }
            if (n == 0)
                Assert.Inconclusive();
            Assert.HasCount(0, q);
        }

        [TestMethod]
        public void TestGetItemKey()
        {
            var q = new PunisherQueue<string>(item =>
            {
                return item switch
                {
                    "a" or "x" => "ax",
                    "b" or "y" => "by",
                    "c" or "z" => "cz",
                    _ => item
                };
            });
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            q.Enqueue("x");
            q.Enqueue("z");
            q.Enqueue("w");
            Assert.AreEqual("bxzw", string.Join("", q));
        }

        [TestMethod]
        public void TestRemove()
        {
            var q = new PunisherQueue<string>();
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            q.Remove("b");
            Assert.AreEqual("ac", string.Join("", q));
        }
    }
}
