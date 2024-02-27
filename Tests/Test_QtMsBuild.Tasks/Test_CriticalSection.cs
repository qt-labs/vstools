/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Tasks
{
    using QtVsTools.QtMsBuild.Tasks;

    [TestClass]
    public class Test_CriticalSection
    {
        private const string LockName = "TEST_CRITICAL_SECTION";
        private int WorkerCount { get; set; }
        private int WorkDuration { get; set; }
        private int TimeoutSecs { get; set; }
        private bool SerializedWork { get; set; }
        private bool FixedTimeout { get; set; }
        private MockBuildEngine BuildEngine { get; set; } = new();
        private MockTaskLogger Logger { get; set; } = new();

        [TestInitialize]
        public void TestInitialize()
        {
            WorkerCount = 100;
            WorkDuration = 1000;
            SerializedWork = true;
            TimeoutSecs = 10;
            FixedTimeout = false;
            CriticalSection.BuildEngine = BuildEngine;
            CriticalSection.Log = Logger;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (SerializedWork)
                CriticalSection.Execute(false, LockName);
            BuildEngine.ResetTaskObjects();
            Workers = new();
        }

        [TestMethod]
        public async Task FailByRaceConditionAsync()
        {
            if (Properties.Configuration == "Release")
                Assert.Inconclusive("Disabled in the 'Release' configuration.");
            SerializedWork = false;
            if (await DoWorkAsync())
                Assert.Inconclusive();
        }

        [TestMethod]
        public async Task FailByTimeoutAsync()
        {
            if (Properties.Configuration == "Release")
                Assert.Inconclusive("Disabled in the 'Release' configuration.");
            FixedTimeout = true;
            if (await DoWorkAsync())
                Assert.Inconclusive();
        }

        [TestMethod]
        public async Task SerializedWorkAsync()
        {
            if (Properties.Configuration == "Release")
                Assert.Inconclusive("Disabled in the 'Release' configuration.");
            Assert.IsTrue(await DoWorkAsync());
        }

        private async Task<bool> DoWorkAsync()
        {
            var tasks = Enumerable.Range(1, WorkerCount)
                .Select(workerId => Task<bool>.Factory.StartNew(() =>
                {
                    if (SerializedWork && !EnterCriticalSection())
                        return false;
                    var result = DoWork(workerId);
                    if (SerializedWork)
                        LeaveCriticalSection();
                    return result;
                },
                CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default));
            var taskResults = await Task.WhenAll(tasks);
            return taskResults.All(result => result == true);
        }

        private bool EnterCriticalSection()
        {
            return CriticalSection.Execute(true, LockName, TimeoutSecs, FixedTimeout);
        }

        private void LeaveCriticalSection()
        {
            CriticalSection.Execute(false, LockName);
        }

        private ConcurrentBag<int> Workers { get; set; } = new();

        private bool DoWork(int id)
        {
            if (Workers.Count > 0)
                return false;
            Workers.Add(id);
            Thread.Sleep(WorkDuration);
            Workers.TryTake(out _);
            return true;
        }
    }
}
