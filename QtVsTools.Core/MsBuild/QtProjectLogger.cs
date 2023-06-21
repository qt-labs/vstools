/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Microsoft.Build.Framework;

namespace QtVsTools.Core.MsBuild
{
    using Core;

    internal class QtProjectLogger : ILogger
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
            switch (Verbosity) {
            case <= LoggerVerbosity.Quiet:
            case <= LoggerVerbosity.Minimal when e.SenderName != "Message":
            case <= LoggerVerbosity.Normal when e.Importance != MessageImportance.High:
            case <= LoggerVerbosity.Detailed
                when e.Importance == MessageImportance.Low || e.SenderName == "MSBuild":
                break;
            default:
                Messages.Print(e.Message);
                break;
            }
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
            if (Verbosity < LoggerVerbosity.Diagnostic)
                return;

            if (e is BuildMessageEventArgs or BuildErrorEventArgs or BuildWarningEventArgs
                or TargetStartedEventArgs or TargetFinishedEventArgs or TaskStartedEventArgs
                or TaskFinishedEventArgs) {
                return;
            }

            Messages.Print(e.Message);
        }

        public void Shutdown()
        {}
    }
}
