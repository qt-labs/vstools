/****************************************************************************
**
** Copyright (C) 2021 The Qt Company Ltd.
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
                    // Issue waiting warning
                    Log.LogWarning("Qt::BuildLock[{0}]: Waiting...", Name);
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
