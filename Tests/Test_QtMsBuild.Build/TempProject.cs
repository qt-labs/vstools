/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Evaluation;

using static System.IO.Path;
using static System.IO.File;
using static System.IO.Directory;

namespace QtVsTools.Test.QtMsBuild.Build
{
    public class TempProject : Disposable
    {
        public string ProjectFileName { get; private set; } = GetRandomFileName();
        public string ProjectDir { get; } = Combine(GetTempPath(), GetRandomFileName());
        public string ProjectPath => Combine(ProjectDir, ProjectFileName);

        private void Reset() {
            if (Directory.Exists(ProjectDir))
                Delete(ProjectDir, true);
        }

        public void Clone(string path)
        {
            if (path is not { Length: > 0 } || !File.Exists(path))
                throw new ArgumentException();
            Reset();
            ProjectFileName = GetFileName(path);
            CreateDirectory(ProjectDir);
            GetFiles(GetDirectoryName(path), "*", SearchOption.TopDirectoryOnly)
                .ToList().ForEach(x => Copy(x, Combine(ProjectDir, GetFileName(x))));
        }

        public void Create(string xml)
        {
            Reset();
            ProjectFileName = GetRandomFileName();
            CreateDirectory(ProjectDir);
            WriteAllText(ProjectPath, xml);
        }

        public void GenerateBigSolution(string templatePath, int projectCount)
        {
            Reset();
            CreateDirectory(ProjectDir);
            BigSolution.Generate(ProjectDir, templatePath, projectCount);
            ProjectFileName = GetFileName(EnumerateFiles(ProjectDir, "*.sln").FirstOrDefault());
        }

        protected override void DisposeUnmanaged()
        {
            Reset();
        }
    }
}
