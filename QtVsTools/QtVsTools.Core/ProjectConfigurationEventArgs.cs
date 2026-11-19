// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;

namespace QtVsTools.Core
{
    public class ProjectConfigurationEventArgs : EventArgs
    {
        public string ProjectPath { get; set; }
        public bool IsCMakeProject { get; set; }
        public string ConfigurationName { get; set; }
    }
}
