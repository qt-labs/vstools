/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.Core
{
    using Common;

    [TestClass]
    public class Test_LazyFactory
    {
        class LazyClass
        {
            LazyFactory Lazy { get; } = new();

            public ConcurrentBag<int> InitThread { get; } = new();
            public string LazyProperty => Lazy.Get(() =>
                LazyProperty, () =>
                {
                    InitThread.Add(Thread.CurrentThread.ManagedThreadId);
                    return "LAZYVALUE";
                });
        }

        [TestMethod]
        public void Test_ThreadSafety()
        {
            var lazyObject = new LazyClass();
            var task = Task.Run(async () =>
            {
                var tasks = new Task[3000];
                for (int i = 0; i < tasks.Length; i++) {
                    var n = i;
                    tasks[i] = Task.Run(() =>
                    {
                        Debug.WriteLine($"Lazy value #{n} is {lazyObject.LazyProperty}");
                    });
                }
                await Task.WhenAll(tasks);
            });
            while (!task.IsCompleted)
                Thread.Sleep(100);

            Assert.IsTrue(lazyObject.InitThread.Count ==  1);
        }
    }
}
