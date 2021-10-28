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

using Microsoft.Build.Framework;

namespace QtVsTools.QtMsBuild
{
    using Core;

    class QtProjectLogger : ILogger
    {
        public LoggerVerbosity Verbosity { get; set; }
        public string Parameters { get; set; }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.ErrorRaised += ErrorRaised;
            eventSource.WarningRaised += WarningRaised;
            eventSource.MessageRaised += MessageRaised;
            eventSource.TargetStarted += TargetStarted;
            eventSource.TargetFinished += TargetFinished;
            eventSource.TaskStarted += TaskStarted;
            eventSource.TaskFinished += TaskFinished;
            eventSource.AnyEventRaised += AnyEventRaised;
        }

        private void ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            if (Verbosity == LoggerVerbosity.Quiet)
                return;
            Messages.Print(e.Message);
        }

        private void WarningRaised(object sender, BuildWarningEventArgs e)
        {
            if (Verbosity == LoggerVerbosity.Quiet)
                return;
            Messages.Print(e.Message);
        }

        private void MessageRaised(object sender, BuildMessageEventArgs e)
        {
            if (Verbosity <= LoggerVerbosity.Quiet)
                return;
            if (Verbosity <= LoggerVerbosity.Minimal && e.SenderName != "Message")
                return;
            if (Verbosity <= LoggerVerbosity.Normal && e.Importance != MessageImportance.High)
                return;
            if (Verbosity <= LoggerVerbosity.Detailed
                && (e.Importance == MessageImportance.Low || e.SenderName == "MSBuild")) {
                return;
            }
            Messages.Print(e.Message);
        }

        private void TargetStarted(object sender, TargetStartedEventArgs e)
        {
            if (Verbosity < LoggerVerbosity.Detailed)
                return;
            Messages.Print(e.Message);
        }

        private void TargetFinished(object sender, TargetFinishedEventArgs e)
        {
            if (Verbosity < LoggerVerbosity.Detailed)
                return;
            Messages.Print(e.Message);
        }

        private void TaskStarted(object sender, TaskStartedEventArgs e)
        {
            if (Verbosity < LoggerVerbosity.Detailed)
                return;
            Messages.Print(e.Message);
        }

        private void TaskFinished(object sender, TaskFinishedEventArgs e)
        {
            if (Verbosity < LoggerVerbosity.Detailed)
                return;
            Messages.Print(e.Message);
        }

        private void AnyEventRaised(object sender, BuildEventArgs e)
        {
            if (Verbosity < LoggerVerbosity.Diagnostic
                || e is BuildMessageEventArgs
                || e is BuildErrorEventArgs
                || e is BuildWarningEventArgs
                || e is TargetStartedEventArgs
                || e is TargetFinishedEventArgs
                || e is TaskStartedEventArgs
                || e is TaskFinishedEventArgs) {
                return;
            }
            Messages.Print(e.Message);
        }

        public void Shutdown()
        {
        }
    }
}
