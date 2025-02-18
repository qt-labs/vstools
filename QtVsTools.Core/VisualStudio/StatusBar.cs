/***************************************************************************************************
Copyright (C) 2024 The Qt Company Ltd.
SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Tasks = System.Threading.Tasks;

namespace QtVsTools.VisualStudio
{
    using Common;

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
            ThreadHelper.JoinableTaskFactory.Run(async () => await SetTextAsync(text));
        }

        public static async Tasks.Task SetTextAsync(string text)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Self.FreezeOutput(0);
            Self.SetText(text);
            Self.FreezeOutput(1);
        }

        public static string GetText()
        {
            return ThreadHelper.JoinableTaskFactory.Run(async () => await GetTextAsync());
        }

        public static async Tasks.Task<string> GetTextAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var status = Self.GetText(out var value);
            Debug.Assert(status == VSConstants.S_OK);
            return value;
        }

        public static void Clear()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () => await ClearAsync());
        }

        public static async Tasks.Task ClearAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Self.FreezeOutput(0);
            var status = Self.Clear();
            Debug.Assert(status == VSConstants.S_OK);
            Self.FreezeOutput(1);
        }

        public static void Progress(string text, int totalSteps, int currentStep = 0)
        {
            Progress(text, (uint)totalSteps, (uint)currentStep);
        }

        public static void Progress(string text, uint totalSteps, uint currentStep = 0)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () => await ProgressAsync(text, totalSteps,
                currentStep));
        }

        public static async Tasks.Task ProgressAsync(string text, uint totalSteps, uint currentStep = 0)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

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
            ThreadHelper.JoinableTaskFactory.Run(async () => await ResetProgressAsync());
        }

        public static async Tasks.Task ResetProgressAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

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
