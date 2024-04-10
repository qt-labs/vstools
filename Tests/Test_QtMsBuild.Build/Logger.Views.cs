/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Build.Framework;

using static Microsoft.Build.Evaluation.ProjectCollection;

namespace QtVsTools.Test.QtMsBuild.Build
{
    public partial class Logger : ILogger
    {
        public IEnumerable<EventArgs> Events(Func<EventArgs, bool> filter = null)
        {
            return Events<EventArgs>(filter);
        }

        public IEnumerable<T> Events<T>(Func<T, bool> filter = null) where T : EventArgs
        {
            var events = EventArgs.OfType<T>().ToList();
            if (filter != null)
                events = events.Where(filter).ToList();
            return events;
        }

        public string Report(Func<EventArgs, bool> filter = null)
        {
            return Report<EventArgs>(filter);
        }

        public string Report<T>(Func<T, bool> filter = null) where T : EventArgs
        {
            return string.Join("", Events(filter)
                .Select(FormatEvent)
                .Where(x => !string.IsNullOrEmpty(x)))
                .Trim(' ', '\r', '\n');
        }

        public IEnumerable<string> Errors => Events<BuildErrorEventArgs>()
            .Select(x => x.Message);

        public IEnumerable<string> Warnings => Events<BuildWarningEventArgs>()
            .Select(x => x.Message);

        public IEnumerable<string> Messages => Events<BuildMessageEventArgs>()
            .Select(x => x.Message);

        public IEnumerable<ProjectAddedToProjectCollectionEventArgs> Imports =>
            Events<ProjectAddedToProjectCollectionEventArgs>();
    }
}
