﻿/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace QtVsTools
{
    public class VsTemplate
    {
        private int Version { get; }
        private string Type { get; }

        private IEnumerable<string> ProjectTypes { get; }
        private IEnumerable<string> TemplateGroupIds { get; }

        public struct ProjectItem
        {
            public bool OpenInEditor;
            public bool ReplaceParameters;
            public string TargetFileName;
            public string TemplateFileName;
        }

        public IEnumerable<ProjectItem> ProjectItems { get; }

        public string Assembly { get; }
        public string FullClassName { get; }

        public bool IsValid { get; }

        public VsTemplate(string templatePath)
        {
            var xmlDoc = XDocument.Parse(File.ReadAllText(templatePath, Encoding.UTF8));

            var ns = xmlDoc.Root?.GetDefaultNamespace();
            if (xmlDoc.Descendants(ns + "VSTemplate").FirstOrDefault() is not {} templateElement)
                return;

            Version = new Version(templateElement.Attribute("Version")?.Value ?? "").Major;
            Type = templateElement.Attribute("Type")?.Value;

            ProjectTypes = templateElement.Elements(ns + "TemplateData")
                .Elements(ns + "ProjectTypeTag")
                .Select(e => e.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value));

            TemplateGroupIds = templateElement.Elements(ns + "TemplateData")
                .Elements(ns + "TemplateGroupID")
                .Select(e => e.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value));

            var contentScope = templateElement.Elements(ns + "TemplateContent");
            if (Type == "Project")
                contentScope = contentScope.Elements(ns + "Project");
            ProjectItems = contentScope.Elements(ns + "ProjectItem")
                .Select(e => new ProjectItem
                {
                    OpenInEditor = bool.TryParse(
                        e.Attribute("OpenInEditor")?.Value, out var open) && open,
                    ReplaceParameters = bool.TryParse(
                        e.Attribute("ReplaceParameters")?.Value, out var replace) && replace,
                    TargetFileName = e.Attribute("TargetFileName")?.Value,
                    TemplateFileName = e.Value
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.TargetFileName)
                     || !string.IsNullOrWhiteSpace(item.TemplateFileName));

            IsValid = Version is >= 2 and <= 4 && ProjectTypes.Contains("Qt") && ProjectItems.Any();
            if (Type == "Item")
                IsValid &= TemplateGroupIds.Contains("QtVsTools");

            if (templateElement.Element(ns + "WizardExtension") is not {} wizardExtensionElement)
                return;

            FullClassName = wizardExtensionElement.Element(ns + "FullClassName")?.Value;
            Assembly = ExtractAssembly(wizardExtensionElement.Element(ns + "Assembly")?.Value);
        }

        private static string ExtractAssembly(string assemblyText)
        {
            var match = Regex.Match(assemblyText ?? "", @"[^,=]+(?=(,|$))");
            return match.Success ? match.Value.Trim() : "";
        }
    }
}
