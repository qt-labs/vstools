// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace QtVsTools.TestAdapter
{
    internal class Logger : IDisposable
    {
        private readonly IMessageLogger logger;
        private readonly string workload;
        private bool showAdapterOutput = true;

        internal Logger(IMessageLogger logger, string workload = "discovery")
        {
            this.logger = logger;
            this.workload = workload;
            ForceSendMessage($"Starting Qt tests {workload}.");
        }

        internal void SetShowAdapterOutput(bool show) => showAdapterOutput = show;

        internal void SendMessage(string message, TestMessageLevel level = TestMessageLevel.Informational)
        {
            if (showAdapterOutput)
                logger.SendMessage(level, message);
        }

        internal void ForceSendMessage(string message, TestMessageLevel level = TestMessageLevel.Informational)
        {
            logger.SendMessage(level, message);
        }

        public void Dispose()
        {
            ForceSendMessage($"Finished Qt tests {workload}.");
        }
    }
}
