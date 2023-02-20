/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
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
            System.String Name)
        #endregion
        {
            #region Code
            var buildEngine = BuildEngine as IBuildEngine4;

            // Acquire lock
            string lockName = string.Format("Global\\{0}", Name);
            EventWaitHandle buildLock = null;
            if (!EventWaitHandle.TryOpenExisting(lockName, out buildLock)) {
                // Lock does not exist; create lock
                bool lockCreated;
                buildLock = new EventWaitHandle(
                    true, EventResetMode.AutoReset, lockName, out lockCreated);
                if (lockCreated) {
                    // Keep lock alive until end of build
                    buildEngine.RegisterTaskObject(
                        Name, buildLock, RegisteredTaskObjectLifetime.Build, false);
                }
            }
            if (buildLock == null) {
                Log.LogError("Qt::BuildLock[{0}]: Error accessing lock", Name);
                return false;
            }
            if (Lock) {
                // Wait until locked
                if (!buildLock.WaitOne(1000)) {
                    var t = Stopwatch.StartNew();
                    do {
                        // Check for build errors
                        if (Log.HasLoggedErrors) {
                            Log.LogError("Qt::BuildLock[{0}]: Errors logged; wait aborted", Name);
                            return false;
                        }
                        // Timeout after 10 secs.
                        if (t.ElapsedMilliseconds >= 10000) {
                            Log.LogError("Qt::BuildLock[{0}]: Timeout; wait aborted", Name);
                            return false;
                        }
                    } while (!buildLock.WaitOne(1000));
                }
            } else {
                // Unlock
                buildLock.Set();
            }
            #endregion

            return true;
        }
    }
}
#endregion
