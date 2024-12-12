/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace QtVsTools.Test.Core
{
    using VisualStudio;

    [TestClass]
    public class Test_IdleTaskManager
    {
#if VS2022
        private static GlobalServiceProvider MockServiceProvider { get; set; }

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            MockServiceProvider?.Dispose();
            MockServiceProvider = new GlobalServiceProvider();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            MockServiceProvider.Dispose();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            MockServiceProvider.Reset();
        }

        [TestMethod]
        public void Test_InstantiateIdleTaskManager()
        {
            // Arrange
            var idleTaskManager = new IdleTaskManager(ThreadHelper.JoinableTaskContext);
            var idleTasks = ArrangeIdleTasksList("activeIdleTasks", idleTaskManager);
            var processedTasks = ArrangeIdleTasksList("processedIdleTasks", idleTaskManager);;

            // Assert
            Assert.AreEqual(0, idleTasks.Count);
            Assert.AreEqual(0, processedTasks.Count);
        }

        [TestMethod]
        public async Task Test_AddAndRemoveTaskAsync()
        {
            // Arrange
            var mockTask = new Mock<IIdleTask>();
            var idleTaskManager = await ArrangeIdleTaskManagerAsync();
            var idleTasks = ArrangeIdleTasksList("activeIdleTasks", idleTaskManager);
            var processedTasks = ArrangeIdleTasksList("processedIdleTasks", idleTaskManager);;

            // Act and Assert
            idleTaskManager.Add(mockTask.Object);
            Assert.AreEqual(1, idleTasks.Count);
            Assert.AreEqual(0, processedTasks.Count);

            idleTaskManager.Remove(mockTask.Object);
            Assert.AreEqual(0, idleTasks.Count);
            Assert.AreEqual(0, processedTasks.Count);
        }

        [TestMethod]
        public async Task Test_RunTaskAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Arrange
            var idleTaskManager = await ArrangeIdleTaskManagerAsync();
            var idleTasks = ArrangeIdleTasksList("activeIdleTasks", idleTaskManager);
            var processedTasks = ArrangeIdleTasksList("processedIdleTasks", idleTaskManager);;

            var mockTask = new Mock<IIdleTask>();
            mockTask.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act and Assert
            idleTaskManager.Add(mockTask.Object);
            Assert.AreEqual(1, idleTasks.Count);

            idleTaskManager.OnEnterIdle((uint)_VSLONGIDLEREASON.LIR_NOUSERINPUT);

            // Assert
            mockTask.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreEqual(1, processedTasks.Count);
        }

        [TestMethod]
        public async Task Test_RunTwoTasksAndRemoveCurrentlyRunningTaskAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Arrange
            var mockTask1 = new Mock<IIdleTask>();
            var mockTask2 = new Mock<IIdleTask>();
            var idleTaskManager = await ArrangeIdleTaskManagerAsync();

            // Simulate a long-running task
            var tcs = new TaskCompletionSource<bool>();
            mockTask1.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(tcs.Task);
            mockTask2.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            idleTaskManager.Add(mockTask1.Object);
            idleTaskManager.Add(mockTask2.Object);

            // Act, simulate entering idle state
            idleTaskManager.OnEnterIdle((uint)_VSLONGIDLEREASON.LIR_NOUSERINPUT);

            // Give some time for the task to start running
            await Task.Delay(100);

            // Remove the currently running task
            idleTaskManager.Remove(mockTask1.Object);

            // Complete the long-running task to allow the test to proceed
            tcs.SetResult(true);

            // Assert
            mockTask1.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockTask2.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Test_RemoveTaskNotCurrentlyRunningAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Arrange
            var mockTask1 = new Mock<IIdleTask>();
            var mockTask2 = new Mock<IIdleTask>();
            var idleTaskManager = await ArrangeIdleTaskManagerAsync();

            mockTask1.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockTask2.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            idleTaskManager.Add(mockTask1.Object);
            idleTaskManager.Add(mockTask2.Object);

            // Act, simulate entering idle state
            idleTaskManager.OnEnterIdle((uint)_VSLONGIDLEREASON.LIR_NOUSERINPUT);
            idleTaskManager.Remove(mockTask2.Object); // Remove the second task

            // Assert
            mockTask1.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockTask2.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Test_RemoveTaskBeforeItStartsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Arrange
            var mockTask1 = new Mock<IIdleTask>();
            var mockTask2 = new Mock<IIdleTask>();
            var idleTaskManager = await ArrangeIdleTaskManagerAsync();

            mockTask1.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockTask2.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            idleTaskManager.Add(mockTask1.Object);
            idleTaskManager.Add(mockTask2.Object);

            // Act, remove the first task before it starts
            idleTaskManager.Remove(mockTask1.Object);
            // Act, simulate entering idle state
            idleTaskManager.OnEnterIdle((uint)_VSLONGIDLEREASON.LIR_NOUSERINPUT);

            // Assert
            mockTask1.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Never);
            mockTask2.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Test_RemoveAllTasksAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Arrange
            var mockTask1 = new Mock<IIdleTask>();
            var mockTask2 = new Mock<IIdleTask>();
            var idleTaskManager = await ArrangeIdleTaskManagerAsync();

            mockTask1.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockTask2.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            idleTaskManager.Add(mockTask1.Object);
            idleTaskManager.Add(mockTask2.Object);

            // Act
            idleTaskManager.Remove(mockTask1.Object);
            idleTaskManager.Remove(mockTask2.Object);
            // Act, simulate entering idle state
            idleTaskManager.OnEnterIdle((uint)_VSLONGIDLEREASON.LIR_NOUSERINPUT);

            // Assert
            mockTask1.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Never);
            mockTask2.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Test_RemoveTaskWhileAnotherTaskIsRunningAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Arrange
            var mockTask1 = new Mock<IIdleTask>();
            var mockTask2 = new Mock<IIdleTask>();
            var idleTaskManager = await ArrangeIdleTaskManagerAsync();

            // Simulate a long-running task
            var tcs = new TaskCompletionSource<bool>();
            mockTask1.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(tcs.Task);
            mockTask2.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            idleTaskManager.Add(mockTask1.Object);
            idleTaskManager.Add(mockTask2.Object);

            // Act, simulate entering idle state
            idleTaskManager.OnEnterIdle((uint)_VSLONGIDLEREASON.LIR_NOUSERINPUT);

            // Give some time for the task to start running
            await Task.Delay(100);

            // Remove the second task while the first task is running
            idleTaskManager.Remove(mockTask2.Object);

            // Complete the long-running task to allow the test to proceed
            tcs.SetResult(true);

            // Assert
            mockTask1.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockTask2.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Test_RemoveTaskThatDoesNotExistAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Arrange
            var mockTask = new Mock<IIdleTask>();
            var idleTaskManager = await ArrangeIdleTaskManagerAsync();

            // Act
            idleTaskManager.Remove(mockTask.Object); // Attempt to remove a task that does not exist

            // Assert
            var idleTasks = ArrangeIdleTasksList("activeIdleTasks", idleTaskManager);
            Assert.AreEqual(0, idleTasks.Count);
            var processedTasks = ArrangeIdleTasksList("processedIdleTasks", idleTaskManager);;
            Assert.AreEqual(0, processedTasks.Count);
        }

        [TestMethod]
        public async Task Test_RemoveCurrentlyRunningTaskAndEnsureNextTaskIndexAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Arrange
            var mockTask1 = new Mock<IIdleTask>();
            var mockTask2 = new Mock<IIdleTask>();
            var mockTask3 = new Mock<IIdleTask>();
            var mockTask4 = new Mock<IIdleTask>();
            var idleTaskManager = await ArrangeIdleTaskManagerAsync();

            // Simulate a long-running task
            var tcs = new TaskCompletionSource<bool>();
            mockTask1.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockTask2.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(tcs.Task);
            mockTask3.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockTask4.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            idleTaskManager.Add(mockTask1.Object);
            idleTaskManager.Add(mockTask2.Object);
            idleTaskManager.Add(mockTask3.Object);
            idleTaskManager.Add(mockTask4.Object);

            // Act, simulate entering idle state
            idleTaskManager.OnEnterIdle((uint)_VSLONGIDLEREASON.LIR_NOUSERINPUT);

            // Give some time for the task to start running
            await Task.Delay(100);

            // Remove the currently running task (task 2)
            idleTaskManager.Remove(mockTask2.Object);

            // Complete the long-running task to allow the test to proceed
            tcs.SetResult(true);

            // Assert
            mockTask1.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockTask2.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockTask3.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockTask4.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Verify
            var idleTasks = ArrangeIdleTasksList("activeIdleTasks", idleTaskManager);
            Assert.AreEqual(0, idleTasks.Count);
            var processedTasks = ArrangeIdleTasksList("processedIdleTasks", idleTaskManager);;
            Assert.AreEqual(3, processedTasks.Count);
        }

        [TestMethod]
        public async Task Test_RunningTasksWithOnExitIdleAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Arrange
            var mockTask1 = new Mock<IIdleTask>();
            var mockTask2 = new Mock<IIdleTask>();
            var mockTask3 = new Mock<IIdleTask>();
            var mockTask4 = new Mock<IIdleTask>();
            var idleTaskManager = await ArrangeIdleTaskManagerAsync();

            // Simulate a long-running task
            var tcs = new TaskCompletionSource<bool>();
            mockTask1.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockTask2.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(tcs.Task);
            mockTask3.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockTask4.Setup(t => t.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            idleTaskManager.Add(mockTask1.Object);
            idleTaskManager.Add(mockTask2.Object);
            idleTaskManager.Add(mockTask3.Object);
            idleTaskManager.Add(mockTask4.Object);

            // Act, simulate entering idle state
            idleTaskManager.OnEnterIdle((uint)_VSLONGIDLEREASON.LIR_NOUSERINPUT);

            // Give some time for the task to start running
            await Task.Delay(100);

            //  Act, simulate leaving idle state
            idleTaskManager.OnExitIdle();

            // Complete the long-running task to allow the test to proceed
            tcs.SetResult(true);

            // Assert
            mockTask1.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockTask2.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockTask3.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Never);
            mockTask4.Verify(t => t.RunAsync(It.IsAny<CancellationToken>()), Times.Never);

            // Verify
            var idleTasks = ArrangeIdleTasksList("activeIdleTasks", idleTaskManager);
            Assert.AreEqual(3, idleTasks.Count);
            var processedTasks = ArrangeIdleTasksList("processedIdleTasks", idleTaskManager);;
            Assert.AreEqual(1, processedTasks.Count);
        }

        private static async Task<IdleTaskManager> ArrangeIdleTaskManagerAsync()
        {
            var mockLongIdleManager = new Mock<IVsLongIdleManager>();
            var mockAsyncServiceProvider = new Mock<IAsyncServiceProvider>();
            mockAsyncServiceProvider.Setup(p => p.GetServiceAsync(typeof(SVsLongIdleManager)))
                .ReturnsAsync(mockLongIdleManager.Object);

            var idleTaskManager = new IdleTaskManager(ThreadHelper.JoinableTaskContext);
            await idleTaskManager.InitializeAsync(mockAsyncServiceProvider.Object,
                CancellationToken.None);
            return idleTaskManager;
        }

        private static List<IIdleTask> ArrangeIdleTasksList(string fieldName, IdleTaskManager manager)
        {
            var processedTasksField = typeof(IdleTaskManager).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (List<IIdleTask>)processedTasksField.GetValue(manager);
        }
#endif
    }
}
