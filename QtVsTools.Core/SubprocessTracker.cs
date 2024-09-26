/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QtVsTools.Core
{
    using static NativeAPI;

    // Reference: https://stackoverflow.com/questions/3342941/
    public static class SubprocessTracker
    {
        static SubprocessTracker()
        {
            // This feature is not supported in Windows 7 or earlier.
            if (Environment.OSVersion.Version < new System.Version(6, 2))
                return;

            JobHandle = CreateJobObject(IntPtr.Zero, "Tracker" + Process.GetCurrentProcess().Id);

            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                }
            };
            var infoSize = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            var infoPtr = Marshal.AllocHGlobal(infoSize);
            Marshal.StructureToPtr(info, infoPtr, false);

            try {
                if (!SetInformationJobObject(JobHandle,
                    JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation,
                    infoPtr,
                    (uint)infoSize)) {
                    throw new Win32Exception();
                }
            } finally {
                Marshal.FreeHGlobal(infoPtr);
            }
        }

        public static bool AddProcess(Process process)
        {
            return JobHandle != IntPtr.Zero
                && process != null
                && AssignProcessToJobObject(JobHandle, process.Handle);
        }

        private static readonly IntPtr JobHandle;
    }
}
