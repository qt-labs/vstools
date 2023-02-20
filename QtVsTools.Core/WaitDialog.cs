/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Core
{
    using VisualStudio;

    public class WaitDialog : IDisposable
    {
        static IVsThreadedWaitDialogFactory factory = null;

        private IVsThreadedWaitDialog2 VsWaitDialog { get; set; }

        private bool Running { get; set; }

        bool? vsDialogCanceled = null;

        public bool Canceled
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (vsDialogCanceled.HasValue)
                    return vsDialogCanceled.Value;

                if (VsWaitDialog == null)
                    return false;

                int res = VsWaitDialog.HasCanceled(out bool canceled);
                if (res != VSConstants.S_OK)
                    return false;

                return canceled;
            }
            private set => vsDialogCanceled = value;
        }

        private WaitDialog() { }

        static WaitDialog Create(IVsThreadedWaitDialogFactory dialogFactory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (factory == null) {
                factory = dialogFactory ?? VsServiceProvider
                    .GetService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>();
                if (factory == null)
                    return null;
            }

            factory.CreateInstance(out IVsThreadedWaitDialog2 vsWaitDialog);
            if (vsWaitDialog == null)
                return null;

            return new WaitDialog
            {
                VsWaitDialog = vsWaitDialog,
                Running = true,
            };
        }

        public static WaitDialog Start(
            string caption,
            string message,
            string progressText = null,
            string statusBarText = null,
            int delay = 0,
            bool isCancelable = false,
            bool showMarqueeProgress = true,
            IVsThreadedWaitDialogFactory dialogFactory = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dialog = Create(dialogFactory);
            if (dialog == null)
                return null;

            var res = dialog.VsWaitDialog.StartWaitDialog(caption, message, progressText,
                    null, statusBarText, delay, isCancelable, showMarqueeProgress);

            if (res != VSConstants.S_OK)
                return null;

            return dialog;
        }

        public static WaitDialog StartWithProgress(
            string caption,
            string message,
            int totalSteps,
            int currentStep = 0,
            string progressText = null,
            string statusBarText = null,
            int delay = 0,
            bool isCancelable = false,
            IVsThreadedWaitDialogFactory dialogFactory = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dialog = Create(dialogFactory);
            if (dialog == null)
                return null;

            var res = dialog.VsWaitDialog.StartWaitDialogWithPercentageProgress(
                caption, message, progressText, null, statusBarText,
                isCancelable, delay, totalSteps, currentStep);

            if (res != VSConstants.S_OK)
                return null;

            return dialog;
        }

        public void Update(
            string message,
            int totalSteps,
            int currentStep,
            string progressText = null,
            string statusBarText = null,
            bool disableCancel = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!Running)
                return;

            int res = VsWaitDialog.UpdateProgress(message, progressText,
                statusBarText, currentStep, totalSteps, disableCancel, out bool canceled);

            if (res != VSConstants.S_OK)
                return;

            if (canceled)
                Stop();
        }

        public void Stop()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!Running)
                return;

            Running = false;
            VsWaitDialog.EndWaitDialog(out int canceled);
            Canceled = (canceled != 0);
        }

        void IDisposable.Dispose()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () => {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Stop();
            });
        }
    }
}
