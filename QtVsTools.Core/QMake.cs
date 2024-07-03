/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QtVsTools.Core
{
    public class QMake : QtBuildTool<QMake>
    {
        public Dictionary<string, string> Vars { get; set; }
        public string OutputFile { get; set; }
        private uint DebugLevel { get; set; }
        public string TemplatePrefix { get; set; }
        public bool Recursive { get; set; }
        public string ProFile { get; set; }
        public string Query { get; set; }
        public bool DisableWarnings { get; set; }

        public QMake(string qtDir, EnvDTE.DTE dte = null)
            : base(qtDir, dte)
        {}

        protected override string ToolArgs
        {
            get
            {
                var args = new StringBuilder();
                if (Vars != null) {
                    foreach (var v in Vars)
                        args.AppendFormat(" {0}={1}", v.Key, v.Value);
                }

                if (!string.IsNullOrEmpty(OutputFile))
                    args.AppendFormat(" -o \"{0}\"", MakeRelative(OutputFile));

                for (var i = 0; i < DebugLevel; ++i)
                    args.Append(" -d");

                if (!string.IsNullOrEmpty(TemplatePrefix))
                    args.AppendFormat(" -tp {0}", TemplatePrefix);

                if (Recursive)
                    args.Append(" -recursive");

                if (DisableWarnings)
                    args.Append(" -Wnone");

                if (!string.IsNullOrEmpty(ProFile))
                    args.AppendFormat(" \"{0}\"", MakeRelative(ProFile));

                if (!string.IsNullOrEmpty(Query))
                    args.AppendFormat(" -query {0}", Query);

                return args.ToString();
            }
        }

        protected override string WorkingDirectory => Path.GetDirectoryName(ProFile);

        private string MakeRelative(string absolutePath)
        {
            var workDir = new Uri(Path.GetDirectoryName(ProFile) + Path.DirectorySeparatorChar);
            var path = new Uri(absolutePath);
            return HelperFunctions.ToNativeSeparator(
                workDir.IsBaseOf(path) ? workDir.MakeRelativeUri(path).OriginalString : absolutePath);
        }
    }
}
