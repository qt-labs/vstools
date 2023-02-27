/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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

        private Guid? guid;
        public override Guid Guid => guid ??= new Guid(GuidString);

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

        protected override bool Detached => QtVsToolsPackage.Instance.Options.DesignerDetached;
    }
}
