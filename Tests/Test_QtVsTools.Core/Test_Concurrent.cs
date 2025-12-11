// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QtVsTools.Test.Core
{
    [TestClass]
    public class Test_ConcurrentTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Test_SingleHolderLock_AllowsOnlyOneThread()
        {
            // We'll track how many threads are "inside" the protected section
            var inCritical = 0;
            var maxConcurrent = 0;

            // This action tries to acquire the lock, increment 'inCritical', sleep, and release.
            async Task LockWorkAsync(string resource)
            {
                var gotLock = await Synchronized.GetAsync(resource);
                Assert.IsTrue(gotLock, "Should acquire lock with no timeout.");

                // We are "in critical section" now
                var newCount = Interlocked.Increment(ref inCritical);
                // Check if we've exceeded previous concurrency
                var snapshot = Math.Max(newCount, maxConcurrent);
                Interlocked.Exchange(ref maxConcurrent, snapshot);

                // Simulate some work
                Thread.Sleep(200);

                // Done, decrement
                Interlocked.Decrement(ref inCritical);

                // Release so next thread can proceed
                Synchronized.Release(resource);
            }

            // We'll run 3 parallel tasks on the same resource
            const string resourceName = "SingleHolderTest";

            var t1 = Task.Run(() => LockWorkAsync(resourceName), TestContext.CancellationToken);
            var t2 = Task.Run(() => LockWorkAsync(resourceName), TestContext.CancellationToken);
            var t3 = Task.Run(() => LockWorkAsync(resourceName), TestContext.CancellationToken);

            await Task.WhenAll(t1, t2, t3);

            // If single-holder logic is correct, maxConcurrent should never exceed 1.
            Assert.IsLessThanOrEqualTo(1, maxConcurrent,
                "Single-holder lock should never allow more than 1 concurrent holder.");

            // Cleanup
            Synchronized.Free(resourceName);
        }

        [TestMethod]
        public void Test_LockTimesOut_IfNotReleased()
        {
            const string resourceName = "TimeoutTest";

            // Acquire and do NOT release
            var lockAcquired = Synchronized.Get(resourceName);
            Assert.IsTrue(lockAcquired, "First call should acquire successfully.");

            // Try to get it again with a small timeout
            var secondAcquired = Synchronized.Get(resourceName, 100);
            // Because we never released, this call should time out and return false
            Assert.IsFalse(secondAcquired,
                "Should fail to acquire lock due to timeout and no release.");

            // Release again after reacquiring, then free
            Synchronized.Release(resourceName);
            Synchronized.Free(resourceName);
        }

        [TestMethod]
        public void Test_LockRelease_UnblocksWaitingThread()
        {
            const string resourceName = "ReleaseUnblockTest";

            // Acquire in the main thread, then we'll have a second thread that waits
            var firstAcquired = Synchronized.Get(resourceName);
            Assert.IsTrue(firstAcquired, "First call should acquire lock.");

            var secondThreadAcquired = false;
            var second = new Thread(() =>
            {
                // This should block until we release
                var gotIt = Synchronized.Get(resourceName, 2000);
                secondThreadAcquired = gotIt;
                if (gotIt)
                    Synchronized.Release(resourceName);
            });
            second.Start();

            // Sleep briefly to ensure second thread is blocked
            Thread.Sleep(500);

            // Now release from main thread
            Synchronized.Release(resourceName);

            // Join the second thread, expect it to have eventually acquired the lock
            second.Join();

            Assert.IsTrue(secondThreadAcquired,
                "Second thread should have acquired lock after we released.");

            Synchronized.Free(resourceName);
        }

        [TestMethod]
        public async Task Test_AsyncLock_CanBeAcquiredAndReleased()
        {
            const string resourceName = "AsyncLockTest";

            // Acquire lock asynchronously
            var firstAcquired = await Synchronized.GetAsync(resourceName, timeout: 1000);
            Assert.IsTrue(firstAcquired, "Async lock acquire should succeed initially.");

            // Try to re-acquire on a different task without releasing. This should time out
            // because we haven't released yet.
            var secondAcquired = await Synchronized.GetAsync(resourceName, 500);
            Assert.IsFalse(secondAcquired,
                "Second async call should time out since we never released.");

            // Now release so we can reacquire
            Synchronized.Release(resourceName);

            // This time, we should succeed quickly
            var thirdAcquired = await Synchronized.GetAsync(resourceName, 500);
            Assert.IsTrue(thirdAcquired, "After release, a new async call should succeed.");

            // Release again after reacquiring, then free
            Synchronized.Release(resourceName);
            Synchronized.Free(resourceName);
        }

        [TestMethod]
        public void Test_Free_RemovesLockResource()
        {
            // Acquire a resource
            const string resourceName = "TestFreeResource";
            var acquired = Synchronized.Get(resourceName, 1000);
            Assert.IsTrue(acquired, "Should be able to acquire new resource lock.");

            // Release & then Free it
            Synchronized.Release(resourceName);
            Synchronized.Free(resourceName);

            // We should be able to reacquire with a new lock object because 'Free' removed the old
            // entry.
            var reacquired = Synchronized.Get(resourceName, 500);
            Assert.IsTrue(reacquired,
                "After Free, reacquiring should succeed on a new resource lock.");

            // Release again after reacquiring, then free
            Synchronized.Release(resourceName);
            Synchronized.Free(resourceName);
        }

        [TestMethod]
        public void Test_AcquireInThreadA_ReleaseFromMainThread_ShouldNotDeadlock()
        {
            const string resourceName = "CrossThreadLockTest";

            var readyToRelease = new AutoResetEvent(false);
            var acquiredSignal = new AutoResetEvent(false);

            // Thread A: Acquire the resource, signal that it is acquired, then wait until main
            // thread signals "ok, you can proceed." But we won't actually release from A.
            var threadA = new Thread(() =>
            {
                var lockAcquired = Synchronized.Get(resourceName, 2000);
                Assert.IsTrue(lockAcquired, "Thread A should acquire lock successfully.");

                // Signal main thread that we have acquired
                acquiredSignal.Set();

                // Wait here until main thread signals we can exit (meaning it tried releasing)
                readyToRelease.WaitOne();

                // We do *not* release here. We'll rely on thread B to do so.
            });
            threadA.Start();

            // Wait until Thread A signals that it has acquired the resource
            Assert.IsTrue(acquiredSignal.WaitOne(2000), "Thread A didn't acquire in time.");

            // Now, from the *main thread*, try to release the resource that thread A acquired.
            // This tests if we can cross-thread release.
            Synchronized.Release(resourceName);

            // Now let thread A finish, it should simply set IsLocked = false, so it won't
            // deadlock.
            readyToRelease.Set();
            threadA.Join();

            // Verify we can reacquire it now that we've released cross-thread.
            var reacquired = Synchronized.Get(resourceName, 500);
            Assert.IsTrue(reacquired, "Resource should be free now after cross-thread release.");

            // Release again after reacquiring, then free
            Synchronized.Release(resourceName);
            Synchronized.Free(resourceName);
        }

        [TestMethod]
        public async Task Test_AcquireInBackgroundThread_ReleaseFromMainThread_ShouldNotDeadlock()
        {
            // The resource name for our test
            const string resourceName = "CrossThreadAsyncTest";

            // Acquire in a background thread (inside GetAsync). Since GetAsync internally does
            // Task.Run(() => Get(...)), that blocking call happens on a pool thread, not the main
            // test thread.
            var acquired = await Synchronized.GetAsync(resourceName, timeout: 2000);
            Assert.IsTrue(acquired, "Should acquire resource via async call without timing out.");

            // The resource is now locked, but the background thread has finished because it only
            // needed to lock briefly to set IsLocked = true. We never released from that
            // background thread.

            //  Now *this thread* (the main MSTest thread) attempts to Release.
            Synchronized.Release(resourceName);

            // Verify we can reacquire it now that we've released cross-thread.
            var reacquired = await Synchronized.GetAsync(resourceName, 500);
            Assert.IsTrue(reacquired,
                "Resource should be free after cross-thread Release from the main thread.");

            // Release again after reacquiring, then free
            Synchronized.Release(resourceName);
            Synchronized.Free(resourceName);
        }
    }
}
