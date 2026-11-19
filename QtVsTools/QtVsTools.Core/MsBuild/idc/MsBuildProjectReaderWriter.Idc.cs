// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace QtVsTools.Core.MsBuild
{
    public partial class MsBuildProjectReaderWriter
    {
        private void ConvertIdcPostBuilds()
        {
            var idcPostBuilds = GetPostBuildEvents("idc.exe");
            foreach (var idcPostBuild in idcPostBuilds) {
                this[Files.Project].Xml.Root.Add(new XElement(ns + "PropertyGroup",
                    new XAttribute("Label", "QtIDC"),
                    idcPostBuild.Parent.Attribute("Condition"),
                    new XElement(ns + "QtIDC", new XText("true")),
                    new XElement(ns + "QtIDCVersion", new XText("1.0"))));
                idcPostBuild.Remove();
            }
        }

        private List<XElement> GetPostBuildEvents(string toolExecName)
        {
            return this[Files.Project].Xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Elements(ns + "PostBuildEvent")
                .Where(x => x.Elements(ns + "Command")
                    .Any(y => y.Value.Contains(toolExecName)))
                .ToList();
        }
    }
}
