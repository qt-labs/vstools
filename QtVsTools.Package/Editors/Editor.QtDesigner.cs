/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Editors
{
    using Core.MsBuild;
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

            if (document?.ProjectItem?.ContainingProject?.Object is not VCProject vcProject)
                return;

            if (MsBuildProject.GetOrAdd(vcProject) is not { IsTracked: true } project)
                return;

            var filePath = document.FullName;
            var lastWriteTime = File.GetLastWriteTime(filePath);

            _ = Task.Run(async () =>
            {
                while (!process.WaitForExit(1000)) {
                    var latestWriteTime = File.GetLastWriteTime(filePath);
                    if (lastWriteTime == latestWriteTime)
                        continue;
                    lastWriteTime = latestWriteTime;
                    await project.RefreshAsync();
                }
                if (lastWriteTime != File.GetLastWriteTime(filePath)) {
                    await project.RefreshAsync();
                }
            });
        }

        protected override bool Detached => QtVsToolsPackage.Instance.Options.DesignerDetached;
    }
}
