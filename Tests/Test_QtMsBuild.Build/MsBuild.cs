/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Build.Evaluation;

namespace QtVsTools.Test.QtMsBuild.Build
{
    public static class MsBuild
    {
        public static ProjectCollection ProjectCollection { get; } = Get();
        public static Logger Log { get; private set; }

        private static ProjectCollection Get()
        {
            var msbuild = new ProjectCollection(ToolsetDefinitionLocations.None);
            Log = new Logger(msbuild);
            msbuild.RemoveAllToolsets();
            var props = new Dictionary<string, string>
            {
                { "VsInstallRoot", Properties.VsInstallRoot },
                { "VCTargetsPath", Properties.VCTargetsPath }
            };
            var toolset = new Toolset("Current", Properties.MSBuildToolsPath, props, msbuild, null);
            msbuild.AddToolset(toolset);
            return msbuild;
        }

        public static Project Evaluate(string path, params (string name, string value)[] globals)
        {
            return ProjectCollection.LoadProject(
                path, globals.ToDictionary(x => x.name, x => x.value), null);
        }

        public static Project Evaluate(XmlReader xml, params (string name, string value)[] globals)
        {
            return ProjectCollection.LoadProject(
                xml, globals.ToDictionary(x => x.name, x => x.value), null);
        }
    }
}
