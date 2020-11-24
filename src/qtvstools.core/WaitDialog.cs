/****************************************************************************
**
** Copyright (C) 2018 The Qt Company Ltd.
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using QtVsTools.VisualStudio;

namespace QtVsTools.Core
{
    public class WaitDialog
    {
        static IVsThreadedWaitDialogFactory factory = null;

        public IVsThreadedWaitDialog2 VsWaitDialog { get; private set; }

        public bool Running { get; private set; }

        bool? vsDialogCanceled = null;

        public bool Canceled
        {
            get
            {
                if (vsDialogCanceled.HasValue)
                    return vsDialogCanceled.Value;

                if (VsWaitDialog == null)
                    return false;

                bool canceled = false;
                int res = VsWaitDialog.HasCanceled(out canceled);
                if (res != VSConstants.S_OK)
                    return false;

                return canceled;
            }
            private set
            {
                vsDialogCanceled = value;
            }
        }

        private WaitDialog() { }

        static WaitDialog Create()
        {
            if (factory == null) {
                factory = VsServiceProvider
                    .GetService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>();
                if (factory == null)
                    return null;
            }

            IVsThreadedWaitDialog2 vsWaitDialog = null;
            factory.CreateInstance(out vsWaitDialog);
            if (vsWaitDialog == null)
                return null;

            return new WaitDialog
            {
                VsWaitDialog = vsWaitDialog,
                Running = true,
            };
        }

        public static WaitDialog Start(
            string waitCaption,
            string waitMessage,
            string progressText,
            string statusBarText,
            int delayToShowDialog,
            bool isCancelable,
            bool showMarqueeProgress)
        {
            var dialog = Create();
            if (dialog == null)
                return null;

            var res = dialog.VsWaitDialog.StartWaitDialog(waitCaption, waitMessage, progressText,
                    null, statusBarText, delayToShowDialog, isCancelable, showMarqueeProgress);

            if (res != VSConstants.S_OK)
                return null;

            return dialog;
        }

        public static WaitDialog StartWithProgress(
            string waitCaption,
            string waitMessage,
            string progressText,
            string statusBarText,
            int delayToShowDialog,
            bool isCancelable,
            int totalSteps,
            int currentStep)
        {
            var dialog = Create();
            if (dialog == null)
                return null;

            var res = dialog.VsWaitDialog.StartWaitDialogWithPercentageProgress(
                waitCaption, waitMessage, progressText, null, statusBarText,
                isCancelable, delayToShowDialog, totalSteps, currentStep);

            if (res != VSConstants.S_OK)
                return null;

            return dialog;
        }

        public void Update(
            string updatedWaitMessage,
            string progressText,
            string statusBarText,
            int currentStep,
            int totalSteps,
            bool disableCancel)
        {
            if (!Running)
                return;

            bool canceled = false;
            int res = VsWaitDialog.UpdateProgress(updatedWaitMessage, progressText,
                statusBarText, currentStep, totalSteps, disableCancel, out canceled);

            if (res != VSConstants.S_OK)
                return;

            if (canceled)
                Stop();
        }

        public void Stop()
        {
            if (!Running)
                return;

            Running = false;
            int canceled = 0;
            VsWaitDialog.EndWaitDialog(out canceled);
            Canceled = (canceled != 0);
        }

    }
}
