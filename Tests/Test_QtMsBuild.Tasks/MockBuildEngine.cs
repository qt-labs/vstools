/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace QtVsTools.Test.QtMsBuild.Tasks
{
    internal class MockBuildEngine : IBuildEngine4
    {
        private ConcurrentDictionary<object, object> TaskObject = new();

        public void ResetTaskObjects()
        {
            TaskObject.Clear();
        }

        public void RegisterTaskObject(object key, object obj, RegisteredTaskObjectLifetime lifetime, bool allowEarlyCollection)
        {
            TaskObject[key] = obj;
        }

        public object UnregisterTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            return TaskObject.TryRemove(key, out var obj) switch
            {
                true => obj,
                false => null
            };
        }

        public object GetRegisteredTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            return TaskObject.TryGetValue(key, out var obj) switch
            {
                true => obj,
                false => null
            };
        }

        public bool IsRunningMultipleNodes => throw new NotImplementedException();

        public bool ContinueOnError => throw new NotImplementedException();

        public int LineNumberOfTaskNode => throw new NotImplementedException();

        public int ColumnNumberOfTaskNode => throw new NotImplementedException();

        public string ProjectFileOfTaskNode => throw new NotImplementedException();

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs, string toolsVersion)
        {
            throw new NotImplementedException();
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        public BuildEngineResult BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IList<string>[] removeGlobalProperties, string[] toolsVersion, bool returnTargetOutputs)
        {
            throw new NotImplementedException();
        }

        public bool BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IDictionary[] targetOutputsPerProject, string[] toolsVersion, bool useResultsCache, bool unloadProjectsOnCompletion)
        {
            throw new NotImplementedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Reacquire()
        {
            throw new NotImplementedException();
        }

        public void Yield()
        {
            throw new NotImplementedException();
        }
    }
}
