﻿/****************************************************************************
**
** Copyright (C) 2020 The Qt Company Ltd.
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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Editors
{
    using QtMsBuild;
    using VisualStudio;

    [Guid(GuidString)]
    public class QtDesigner : Editor
    {
        public const string GuidString = "96FE523D-6182-49F5-8992-3BEA5F7E6FF6";
        public const string Title = "Qt Designer";

        Guid? _Guid;
        public override Guid Guid => (_Guid ?? (_Guid = new Guid(GuidString))).Value;

        public override string ExecutableName => "designer.exe";

        public override Func<string, bool> WindowFilter =>
            caption => caption.StartsWith(Title);

        protected override string GetTitle(Process editorProcess)
        {
            return Title;
        }

        protected override void OnStart(Process process)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.OnStart(process);
            var document = VsShell.GetDocument(Context, ItemId);
            if (document == null)
                return;

            var project = document.ProjectItem?.ContainingProject;
            if (project == null || !QtProjectTracker.IsTracked(project.FullName))
                return;
            string projectPath = project.FullName;
            string filePath = document.FullName;
            string[] itemId = new[] { document.ProjectItem?.Name };
            var lastWriteTime = File.GetLastWriteTime(filePath);
            _ = Task.Run(async () =>
            {
                while (!process.WaitForExit(1000)) {
                    var latestWriteTime = File.GetLastWriteTime(filePath);
                    if (lastWriteTime != latestWriteTime) {
                        lastWriteTime = latestWriteTime;
                        await QtProjectIntellisense.RefreshAsync(project, projectPath);
                    }
                }
                if (lastWriteTime != File.GetLastWriteTime(filePath)) {
                    await QtProjectIntellisense.RefreshAsync(project, projectPath);
                }
            });
        }

        protected override bool Detached => QtVsToolsLegacyPackage.Instance.Options.DesignerDetached;
    }
}
