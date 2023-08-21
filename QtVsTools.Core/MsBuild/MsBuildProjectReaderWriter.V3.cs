/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace QtVsTools.Core.MsBuild
{
    using static MsBuildProjectFormat;
    using static Utils;

    public partial class MsBuildProjectReaderWriter
    {
        private bool ConvertToV3()
        {
            try {
                V3FormatVersion();
                V3QtMsBuildFallback();
                V3QtDefaultProps();
                V3QtSettings();
                V3WarningQtMsBuildNotFound();
                V3QtPropertySheet();
                V3UncategorizedProperties();
                V3QtTargets();
            } catch (Exception e) {
                e.Log();
                return false;
            }
            return true;
        }

        private void V3FormatVersion()
        {
            var keyword = Globals
                .Elements(ns + "Keyword")
                .FirstOrDefault(x => x.Value.StartsWith(KeywordLatest)
                    || x.Value.StartsWith(KeywordV2));
            if (keyword is null)
                Globals.Add(keyword = new XElement(ns + "Keyword"));
            keyword.SetValue($"QtVS_v{(int)Version.Latest}");
            Commit("Project format version");
        }

        private void V3QtMsBuildFallback()
        {
            RemoveProperty("QtMsBuild");
            Globals.Add(new XElement(ns + "QtMsBuild",
                new XAttribute("Condition", $@"'$(QtMsBuild)'=='' OR !Exists('{QtTargetsPath}')"),
                @"$(MSBuildProjectDirectory)\QtMsBuild"));
            Commit("Fallback for QTMSBUILD environment variable");
        }

        private void V3QtDefaultProps()
        {
            RemoveImport(QtDefaultPropsPath);
            ImportCppProps.AddAfterSelf(new XElement(ns + "Import",
                new XAttribute("Project", QtDefaultPropsPath),
                new XAttribute("Condition", $"Exists('{QtDefaultPropsPath}')")));
            Commit("Default Qt properties");
        }

        private void V3QtSettings()
        {
            var qtSettingsGroups = FindPropertyGroups("QtSettings")
                .ToList();
            if (qtSettingsGroups.Any()) {
                qtSettingsGroups.ForEach(x => x.Remove());
            } else {
                foreach (var config in Configs) {
                    qtSettingsGroups.Add(new XElement(ns + "PropertyGroup",
                        new XAttribute("Label", "QtSettings"),
                        new XAttribute("Condition",
                            $"'$(Configuration)|$(Platform)'=='{config}'")));
                }
            }

            // Relocate misplaced $(QtInstall) definitions (e.g. in v3.0 projects)
            var qtInstallProps = FindProperty("QtInstall").ToList();
            foreach (var qtSettings in qtSettingsGroups) {
                var condition = qtSettings.Attribute("Condition")?.Value?.Replace(" ", "");
                var qtInstall = qtInstallProps.LastOrDefault(x =>
                    x.Parent?.Attribute("Condition")?.Value is not { } qtInstallCondition
                    || qtInstallCondition.Replace(" ", "").Equals(condition, IgnoreCase));
                if (qtInstall is null)
                    continue;
                qtSettings.Add(new XElement(qtInstall));
            }
            qtInstallProps.ForEach(x => x.Remove());

            QtDefaultProps.AddAfterSelf(qtSettingsGroups);
            Commit("Qt build settings");
        }

        private void V3WarningQtMsBuildNotFound()
        {
            RemoveTarget("QtMsBuildNotFound");
            QtSettings.Last()
                .AddAfterSelf(new XElement(ns + "Target",
                    new XAttribute("Name", "QtMsBuildNotFound"),
                    new XAttribute("BeforeTargets", "CustomBuild;ClCompile"),
                    new XAttribute("Condition",
                        $"!Exists('{QtTargetsPath}') OR !Exists('{QtPropsPath}')"),
                    new XElement(ns + "Message",
                        new XAttribute("Importance", "High"),
                        new XAttribute("Text",
                            "QtMsBuild: could not locate qt.targets, qt.props; " +
                            "project may not build correctly."))));
            Commit("Warn if Qt/MSBuild is not found");
        }

        private void V3QtPropertySheet()
        {
            RemoveImport(QtPropsPath);
            foreach (var propSheetGroup in PropertySheetGroups) {
                propSheetGroup.Add(new XElement(ns + "Import",
                    new XAttribute("Project", QtPropsPath)));
            }
            Commit("Qt property sheet");
        }

        private void V3UncategorizedProperties()
        {
            var propGroups = FindPropertyGroups().ToList();
            propGroups.ForEach(x => x.Remove());
            UserMacros.AddAfterSelf(propGroups);
            Commit("Relocate uncategorized property sheets");
        }

        private void V3QtTargets()
        {
            RemoveImport(QtTargetsPath);
            ImportCppTargets.AddAfterSelf(new XElement(ns + "Import",
                new XAttribute("Project", QtTargetsPath),
                new XAttribute("Condition", $"Exists('{QtTargetsPath}')")));
            Commit("Qt targets");
        }

        private IEnumerable<XElement> FindPropertyGroups(string label = null)
        {
            return ProjectFile
                .Element(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Where(x => label switch {
                    { Length: > 0 } => x.Attribute("Label") is { } groupLabel
                        && groupLabel.Value.Equals(label, IgnoreCase),
                    _ => x.Attribute("Label") is null
                });
        }

        private void RemoveProperty(string name)
        {
            foreach (var property in FindProperty(name).ToList()) {
                if (property.Parent.Elements().Count() == 1)
                    property.Parent.Remove();
                else
                    property.Remove();
            }
        }

        private IEnumerable<XElement> FindProperty(string name)
        {
            return ProjectFile
                .Descendants(ns + name)
                .Where(x => x.Parent.Name == ns + "PropertyGroup");
        }

        private void RemoveImport(string name)
        {
            var imports = ProjectFile
                .Element(ns + "Project")
                .Descendants(ns + "Import")
                .Where(x => x.Attribute("Project").Value.Equals(name, IgnoreCase));
            foreach (var import in imports.ToList()) {
                if (import.Parent is { } importGroup && importGroup.Name.LocalName == "ImportGroup"
                    && importGroup.Elements().Count() == 1) {
                    importGroup.Remove();
                } else {
                    import.Remove();
                }
            }
        }

        private void RemoveTarget(string name)
        {
            var target = ProjectFile
                .Element(ns + "Project")
                .Elements(ns + "Target")
                .FirstOrDefault(x => x.Attribute("Name").Value.Equals(name, IgnoreCase));
            target?.Remove();
        }

        private const string QtDefaultPropsPath = @"$(QtMsBuild)\qt_defaults.props";
        private const string QtPropsPath = @"$(QtMsBuild)\Qt.props";
        private const string QtTargetsPath = @"$(QtMsBuild)\qt.targets";

        private XDocument ProjectFile => this[Files.Project].Xml
            ?? throw new QtVSException("Missing project file.");

        private IEnumerable<string> Configs => ProjectFile
            .Elements(ns + "Project")
            .Elements(ns + "ItemGroup")
            .Elements(ns + "ProjectConfiguration")
            .Attributes("Include")
            .Select(x => x.Value)
            is { } configs && configs.Any() ? configs
            : throw new QtVSException("Missing \"ProjectConfiguration\" items.");

        private XElement Globals => ProjectFile
            .Elements(ns + "Project")
            .Elements(ns + "PropertyGroup")
            .FirstOrDefault(x => x.Attribute("Label").Value.Equals("Globals", IgnoreCase))
            ?? throw new QtVSException("Missing \"Globals\" property group.");

        private XElement ImportCppProps => ProjectFile
            .Elements(ns + "Project")
            .Descendants(ns + "Import")
            .Where(x => x.Attribute("Project") is { } project
                && project.Value.Equals(@"$(VCTargetsPath)\Microsoft.Cpp.props", IgnoreCase))
            .Select(x => x.Parent.Name == ns + "ImportGroup" ? x.Parent : x)
            .FirstOrDefault()
            ?? throw new QtVSException("Missing \"Microsoft.Cpp.props\" import.");

        private XElement ImportCppTargets => ProjectFile
            .Elements(ns + "Project")
            .Descendants(ns + "Import")
            .Where(x => x.Attribute("Project") is { } project
                && project.Value.Equals(@"$(VCTargetsPath)\Microsoft.Cpp.targets", IgnoreCase))
            .Select(x => x.Parent.Name == ns + "ImportGroup" ? x.Parent : x)
            .FirstOrDefault()
            ?? throw new QtVSException("Missing \"Microsoft.Cpp.targets\" import.");

        private IEnumerable<XElement> PropertySheetGroups => ProjectFile
            .Elements(ns + "Project")
            .Elements(ns + "ImportGroup")
            .Where(x => x.Attribute("Label")?.Value.Equals("PropertySheets") == true)
            is { } propertySheetGroups && propertySheetGroups.Any() ? propertySheetGroups
            : throw new QtVSException("Missing \"PropertySheets\" import groups.");

        private XElement UserMacros => ProjectFile
            .Elements(ns + "Project")
            .Elements(ns + "PropertyGroup")
            .LastOrDefault(x => (string)x.Attribute("Label") == "UserMacros")
            ?? throw new QtVSException("Missing \"UserMacros\" property group.");

        private XElement QtDefaultProps => ProjectFile
            .Elements(ns + "Project")
            .Elements(ns + "Import")
            .FirstOrDefault(x => x.Attribute("Project").Value
                .Equals(QtDefaultPropsPath, IgnoreCase))
            ?? throw new QtVSException("Missing \"qt_defaults.props\" import.");

        private IEnumerable<XElement> QtSettings => FindPropertyGroups("QtSettings")
            is { } qtSettings && qtSettings.Any() ? qtSettings
            : throw new QtVSException("Missing \"QtSettings\" property groups.");
    }
}
