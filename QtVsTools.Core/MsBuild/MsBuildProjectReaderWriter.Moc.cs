// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace QtVsTools.Core.MsBuild
{
    using static Common.Utils;

    public partial class MsBuildProjectReaderWriter
    {
        private class MocPreprocessDefinesInfo
        {
            public string Command { get; set; }
            public string Inputs { get; set; }
            public string Outputs { get; set; }
            public string Message { get; set; }

            public bool IsComplete => !string.IsNullOrEmpty(Command)
                && !string.IsNullOrEmpty(Inputs) && !string.IsNullOrEmpty(Outputs);
        }

        /// <summary>
        /// Try to convert moc_predefs CustomBuild items into per-config property groups. Returns
        /// false if any configuration is missing or incomplete.
        /// </summary>
        private bool TryConvertMocCbtCustomBuildsToPropertyGroups(
            IReadOnlyList<XElement> mocCbtCustomBuilds, IEnumerable<XElement> configurations)
        {
            // gather per-config predefs command/inputs/outputs from the qmake custombuild items
            var mocPreprocessByConfig = new Dictionary<string, MocPreprocessDefinesInfo>(CaseIgnorer);
            foreach (var cbt in mocCbtCustomBuilds) {
                foreach (var command in cbt.Elements(ns + "Command")) {
                    var condition = (string)command.Attribute("Condition");
                    var configName = GetConfigNameFromCondition(condition);
                    if (string.IsNullOrEmpty(configName))
                        continue;

                    if (!mocPreprocessByConfig.TryGetValue(configName, out var info))
                        mocPreprocessByConfig[configName] = info = new MocPreprocessDefinesInfo();

                    info.Command = NormalizeCustomBuildValue((string)command);

                    var inputs = cbt.Elements(ns + "AdditionalInputs")
                        .FirstOrDefault(x => (string)x.Attribute("Condition") == condition);
                    if (inputs != null)
                        info.Inputs = NormalizeCustomBuildValue((string)inputs);

                    var outputs = cbt.Elements(ns + "Outputs")
                        .FirstOrDefault(x => (string)x.Attribute("Condition") == condition);
                    if (outputs != null)
                        info.Outputs = NormalizeCustomBuildValue((string)outputs);

                    var message = cbt.Elements(ns + "Message")
                        .FirstOrDefault(x => (string)x.Attribute("Condition") == condition);
                    info.Message = message != null ? (string)message : "Generate moc_predefs.h";
                }
            }

            // require a complete set of configs and all required fields
            var configNames = configurations.Select(x => (string)x.Attribute("Include"))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
            var allConfigsCaptured = configNames.Count > 0
                && configNames.All(x => mocPreprocessByConfig.ContainsKey(x));

            var allConfigsValid = allConfigsCaptured
                && configNames.All(x => mocPreprocessByConfig.TryGetValue(x, out var info)
                    && info.IsComplete);

            if (!allConfigsValid)
                return false;

            if (this[Files.Project]?.Xml?.Root is not {} root)
                return false;

            // insert after the last existing propertygroup to avoid hard-coded import names
            var lastPropertyGroup = root.Elements(ns + "PropertyGroup").LastOrDefault();

            foreach (var config in configNames) {
                if (!mocPreprocessByConfig.TryGetValue(config, out var info))
                    continue;

                var propertyGroup = new XElement(ns + "PropertyGroup",
                    new XAttribute("Label", "QtMocPreprocessDefines"),
                    new XAttribute("Condition", $"'$(Configuration)|$(Platform)'=='{config}'"));

                propertyGroup.Add(new XElement(ns + "QtMocPreprocessDefinesCommand", info.Command));
                propertyGroup.Add(new XElement(ns + "QtMocPreprocessDefinesInputs", info.Inputs));
                propertyGroup.Add(new XElement(ns + "QtMocPreprocessDefinesOutputs", info.Outputs));
                propertyGroup.Add(new XElement(ns + "QtMocPreprocessDefinesMessage", info.Message));

                if (lastPropertyGroup != null)
                    lastPropertyGroup.AddAfterSelf(propertyGroup);
                else
                    root.AddFirst(propertyGroup);
                lastPropertyGroup = propertyGroup;
            }

            // remove the original moc_predefs custombuild items and filters
            var mocPreprocessIncludes = new HashSet<string>(mocCbtCustomBuilds
                .Select(x => (string)x.Attribute("Include")), CaseIgnorer);
            foreach (var cbt in mocCbtCustomBuilds)
                cbt.Remove();

            if (mocPreprocessIncludes.Count <= 0 || this[Files.Filters]?.Xml == null)
                return true;

            // remove moc_predefs items from filters
            var filterCustomBuilds = this[Files.Filters].Xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "CustomBuild")
                .Where(x => mocPreprocessIncludes.Contains((string)x.Attribute("Include")))
                .ToList();
            filterCustomBuilds.ForEach(x => x.Remove());

            return true;
        }

        private void ConvertMocCbtCustomBuildsToCustomBuild(
            IEnumerable<XElement> mocPreprocessCustomBuilds)
        {
            // replace each set of .moc.cbt custom build steps
            // with a single .cpp custom build step
            var mocCbtCustomBuilds = mocPreprocessCustomBuilds
                .GroupBy(CustomBuildMocInput);

            var cbtToRemove = new List<XElement>();
            foreach (var cbtGroup in mocCbtCustomBuilds) {

                //create new CustomBuild item for .cpp
                var newCbt = new XElement(ns + "CustomBuild",
                    new XAttribute("Include", cbtGroup.Key),
                    new XElement(ns + "FileType", "Document"));

                //add properties from .moc.cbt items
                var cbtPropertyNames = new List<string> {
                    "AdditionalInputs",
                    "Command",
                    "Message",
                    "Outputs"
                };
                foreach (var cbt in cbtGroup) {
                    var enabledProperties = cbt.Elements().Where(x =>
                        x.Parent != null
                        && cbtPropertyNames.Contains(x.Name.LocalName)
                        && x.Parent.Elements(ns + "ExcludedFromBuild")
                            .All(y => (string)x.Attribute("Condition") != (string)y.Attribute("Condition")));
                    foreach (var property in enabledProperties) {
                        property.Value = property.Value.Replace("debug", "$(IntDir)")
                            .Replace("release", "$(IntDir)");
                        newCbt.Add(new XElement(property));
                    }
                    cbtToRemove.Add(cbt);
                }
                cbtGroup.First().AddBeforeSelf(newCbt);

                //remove ClCompile item (cannot have duplicate items)
                var cppMocItems = this[Files.Project].Xml
                    .Elements(ns + "Project")
                    .Elements(ns + "ItemGroup")
                    .Elements(ns + "ClCompile")
                    .Where(x =>
                        string.Equals(cbtGroup.Key, (string)x.Attribute("Include"), IgnoreCase));
                foreach (var cppMocItem in cppMocItems)
                    cppMocItem.Remove();

                //change type of item in filter
                cppMocItems = this[Files.Filters]?.Xml
                    ?.Elements(ns + "Project")
                    .Elements(ns + "ItemGroup")
                    .Elements(ns + "ClCompile")
                    .Where(x =>
                        string.Equals(cbtGroup.Key, (string)x.Attribute("Include"), IgnoreCase));
                foreach (var cppMocItem in cppMocItems)
                    cppMocItem.Name = ns + "CustomBuild";
            }

            //remove .moc.cbt CustomBuild items
            cbtToRemove.ForEach(x => x.Remove());
        }

        /// <summary> Normalize qmake custom build values for use in property groups. </summary>
        private static string NormalizeCustomBuildValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var normalized = value
                .Replace("debug", "$(IntDir)")
                .Replace("release", "$(IntDir)")
                .Replace(";%(AdditionalInputs)", "")
                .Replace("%(AdditionalInputs)", "")
                .Replace(";%(Outputs)", "")
                .Replace("%(Outputs)", "");

            return normalized.Trim(';');
        }

        /// <summary>
        /// Extracts the Configuration|Platform name from a standard MSBuild Condition string.
        /// </summary>
        private static string GetConfigNameFromCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return "";
            var match = ConditionParser.Match(condition);
            return match.Success ? match.Groups[1].Value : "";
        }
    }
}
