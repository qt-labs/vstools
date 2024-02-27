/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

#region Task TaskName="CriticalSection"

#region Using
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Win32;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK CriticalSection
/////////////////////////////////////////////////////////////////////////////////////////////////
// Enter or leave a critical section during build
// Parameters:
//      in bool   Lock: 'true' when entering the critical section, 'false' when leaving
//      in string Name: Critical section lock name
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class CriticalSection
    {
        public static QtMSBuild.ITaskLoggingHelper Log { get; set; }
        public static IBuildEngine BuildEngine { get; set; }

        public static bool Execute(
        #region Parameters
            System.Boolean Lock,
            System.String Name,
            System.Int32 Timeout = 0,
            System.Boolean FixedTimeout = false,
            System.Int32 Delay = 0)
        #endregion
        {
            #region Code
            var buildEngine = BuildEngine as IBuildEngine4;
            Name = Name.Trim(' ', '{', '}');
            var tmpFile = Path.Combine(Path.GetTempPath(), string.Format("qtmsbuild{0}.tmp", Name));

            if (Timeout <= 0)
                Timeout = 10;

            var eventName = string.Format("Global\\QtMSBuild.Lock.Project-{0}", Name);
            var waitHandle = buildEngine.GetRegisteredTaskObject(eventName,
                RegisteredTaskObjectLifetime.Build) as EventWaitHandle;
            if (waitHandle == null && !EventWaitHandle.TryOpenExisting(eventName, out waitHandle)) {
                // Lock does not exist; create lock
                bool lockCreated;
                waitHandle = new EventWaitHandle(
                    true, EventResetMode.AutoReset, eventName, out lockCreated);
                if (lockCreated) {
                    // Keep lock alive until end of build
                    buildEngine.RegisterTaskObject(
                        eventName, waitHandle, RegisteredTaskObjectLifetime.Build, false);
                }
            }

            if (!Lock) {
                if (!FixedTimeout)
                    File.WriteAllBytes(tmpFile, new byte[0]);
                if (Delay > 0)
                    Thread.Sleep(Delay);
                waitHandle.Set();
                return true;
            }

            var timeoutReference = DateTime.Now;
            while (!waitHandle.WaitOne(3000)) {
                if (Log.HasLoggedErrors) {
                    Log.LogError("Qt::BuildLock[{0}]: Errors logged; wait aborted", Name);
                    return false;
                }
                if (!FixedTimeout && File.Exists(tmpFile))
                    timeoutReference = File.GetLastWriteTime(tmpFile);
                if (DateTime.Now.Subtract(timeoutReference).TotalSeconds >= Timeout) {
                    Log.LogError("Qt::BuildLock[{0}]: Timeout; wait aborted", Name);
                    return false;
                }
            }
            #endregion

            return true;
        }
    }
}
#endregion
