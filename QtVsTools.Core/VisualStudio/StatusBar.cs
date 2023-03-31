/***************************************************************************************************
Copyright (C) 2023 The Qt Company Ltd.
SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.VisualStudio
{
    using System.Diagnostics;
    using Common;
    using Microsoft.VisualStudio;

    public static class StatusBar
    {
        private static LazyFactory Lazy { get; } = new();

        private static IVsStatusbar Self => Lazy.Get(() => Self, () =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return VsServiceProvider.GetService<SVsStatusbar, IVsStatusbar>();
        });

        private static uint Cookie { get; set; }

        public static void SetText(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Self.SetText(text);
        }

        public static string Label
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                string value = "";
                Debug.Assert(Self.GetText(out value) == VSConstants.S_OK);
                return value;
            }
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                Debug.Assert(Self.SetText(value) == VSConstants.S_OK);
            }
        }

        public static void Clear()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Debug.Assert(Self.Clear() == VSConstants.S_OK);
        }

        public static void Progress(string text, int totalSteps, int currentStep = 0)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Progress(text, (uint)totalSteps, (uint)currentStep);
        }

        public static void Progress(string text, uint totalSteps, uint currentStep = 0)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Self is null)
                return;
            if (Cookie == 0)
                Self.FreezeOutput(0);
            var cookie = Cookie;
            Self.Progress(ref cookie, 1, text, currentStep, totalSteps);
            Cookie = cookie;
        }

        public static void ResetProgress()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Self is null)
                return;
            if (Cookie == 0)
                return;
            var cookie = Cookie;
            Self?.Progress(ref cookie, 0, string.Empty, 0, 0);
            Cookie = 0;
        }
    }
}
