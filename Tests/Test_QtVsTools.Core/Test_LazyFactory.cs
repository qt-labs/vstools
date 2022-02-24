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
            LazyFactory Lazy { get; } = new LazyFactory();

            public ConcurrentBag<int> InitThread { get; } = new ConcurrentBag<int>();
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
