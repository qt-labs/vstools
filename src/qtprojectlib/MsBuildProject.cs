/****************************************************************************
**
** Copyright (C) 2017 The Qt Company Ltd.
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
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using QtProjectLib.QtMsBuild;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.Build.Evaluation;

namespace QtProjectLib
{
    public class MsBuildProject
    {
        class MsBuildXmlFile
        {
            public string filePath = "";
            public XDocument xml = null;
            public bool isDirty = false;
        }

        enum Files
        {
            Project = 0,
            Filters,
            User,
            Count
        }
        MsBuildXmlFile[] files = new MsBuildXmlFile[(int)Files.Count];

        MsBuildProject()
        {
            for (int i = 0; i < files.Length; i++)
                files[i] = new MsBuildXmlFile();
        }

        MsBuildXmlFile this[Files file]
        {
            get
            {
                if ((int)file >= (int)Files.Count)
                    return files[0];
                return files[(int)file];
            }
        }

        public string ProjectXml
        {
            get
            {
                var xml = this[Files.Project].xml;
                if (xml == null)
                    return "";
                return xml.ToString(SaveOptions.None);
            }
        }

        private static XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";

        public static MsBuildProject Load(string pathToProject)
        {
            if (!File.Exists(pathToProject))
                return null;

            MsBuildProject project = new MsBuildProject();

            project[Files.Project].filePath = pathToProject;
            if (!LoadXml(project[Files.Project]))
                return null;

            project[Files.Filters].filePath = pathToProject + ".filters";
            if (File.Exists(project[Files.Filters].filePath) && !LoadXml(project[Files.Filters]))
                return null;

            project[Files.User].filePath = pathToProject + ".user";
            if (File.Exists(project[Files.User].filePath) && !LoadXml(project[Files.User]))
                    return null;

            return project;
        }

        static bool LoadXml(MsBuildXmlFile xmlFile)
        {
            try {
                var xmlText = File.ReadAllText(xmlFile.filePath, Encoding.UTF8);
                using (var reader = XmlReader.Create(new StringReader(xmlText))) {
                    xmlFile.xml = XDocument.Load(reader);
                }
            } catch (Exception) {
                return false;
            }
            return true;
        }

        public bool Save()
        {
            foreach (var file in files) {
                if (file.isDirty) {
                    file.xml.Save(file.filePath, SaveOptions.None);
                    file.isDirty = false;
                }
            }
            return true;
        }

        public string GetProperty(string property_name)
        {
            var xProperty = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Elements()
                .Where(x => x.Name.LocalName == property_name)
                .FirstOrDefault();
            if (xProperty == null)
                return string.Empty;
            return xProperty.Value;
        }

        public string GetProperty(string item_type, string property_name)
        {
            var xProperty = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Elements(ns + item_type)
                .Elements()
                .Where(x => x.Name.LocalName == property_name)
                .FirstOrDefault();
            if (xProperty == null)
                return string.Empty;
            return xProperty.Value;
        }

        public bool EnableMultiProcessorCompilation()
        {
            var xImportCppProps = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "Import")
                .Where(x =>
                    x.Attribute("Project").Value == @"$(VCTargetsPath)\Microsoft.Cpp.props")
                .FirstOrDefault();
            if (xImportCppProps == null)
                return false;

            xImportCppProps.AddAfterSelf(
                new XElement(ns + "ItemDefinitionGroup",
                    new XElement(ns + "ClCompile",
                        new XElement(ns + "MultiProcessorCompilation", "true"))));
            return true;
        }

        public bool SetDefaultWindowsSDKVersion(string winSDKVersion)
        {
            var xGlobals = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Where(x => (string)x.Attribute("Label") == "Globals")
                .FirstOrDefault();
            if (xGlobals == null)
                return false;
            if (xGlobals.Element(ns + "WindowsTargetPlatformVersion") != null)
                return true;
            xGlobals.Add(
                new XElement(ns + "WindowsTargetPlatformVersion", winSDKVersion));
            return true;
        }

        public bool AddQtMsBuildReferences()
        {
            var isQtMsBuildEnabled = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ImportGroup")
                .Elements(ns + "Import")
                .Where(x =>
                    x.Attribute("Project").Value == @"$(QtMsBuild)\qt.props")
                .Any();
            if (isQtMsBuildEnabled)
                return true;

            var xImportCppProps = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "Import")
                .Where(x =>
                    x.Attribute("Project").Value == @"$(VCTargetsPath)\Microsoft.Cpp.props")
                .FirstOrDefault();
            if (xImportCppProps == null)
                return false;

            var xImportCppTargets = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "Import")
                .Where(x =>
                    x.Attribute("Project").Value == @"$(VCTargetsPath)\Microsoft.Cpp.targets")
                .FirstOrDefault();
            if (xImportCppTargets == null)
                return false;

            xImportCppProps.AddAfterSelf(
                new XElement(ns + "PropertyGroup",
                    new XAttribute("Condition",
                        @"'$(QtMsBuild)'=='' " +
                        @"or !Exists('$(QtMsBuild)\qt.targets')"),
                    new XElement(ns + "QtMsBuild",
                        @"$(MSBuildProjectDirectory)\QtMsBuild")),

                new XElement(ns + "Target",
                    new XAttribute("Name", "QtMsBuildNotFound"),
                    new XAttribute("BeforeTargets", "CustomBuild;ClCompile"),
                    new XAttribute("Condition",
                        @"!Exists('$(QtMsBuild)\qt.targets') " +
                        @"or !Exists('$(QtMsBuild)\qt.props')"),
                    new XElement(ns + "Message",
                        new XAttribute("Importance", "High"),
                        new XAttribute("Text",
                            "QtMsBuild: could not locate qt.targets, qt.props; " +
                            "project may not build correctly."))),

                new XElement(ns + "ImportGroup",
                    new XAttribute("Condition", @"Exists('$(QtMsBuild)\qt.props')"),
                    new XElement(ns + "Import",
                        new XAttribute("Project", @"$(QtMsBuild)\qt.props"))));

            xImportCppTargets.AddAfterSelf(
                new XElement(ns + "ImportGroup",
                    new XAttribute("Condition", @"Exists('$(QtMsBuild)\qt.targets')"),
                    new XElement(ns + "Import",
                        new XAttribute("Project", @"$(QtMsBuild)\qt.targets"))));

            this[Files.Project].isDirty = true;
            return true;
        }

        bool SetCommandLines(
            QtMsBuildContainer qtMsBuild,
            IEnumerable<string> configurations,
            IEnumerable<XElement> customBuilds,
            string itemType)
        {
            var query = from customBuild in customBuilds
                        let itemName = customBuild.Attribute("Include").Value
                        from configName in configurations
                        from command in customBuild.Elements(ns + "Command")
                        let commandLine = command.Value
                        where command.Attribute("Condition").Value
                            == string.Format(
                                "'$(Configuration)|$(Platform)'=='{0}'",
                                configName)
                        select new { customBuild, itemName, configName, commandLine };
            foreach (var row in query) {
                XElement item;
                row.customBuild.Add(item =
                    new XElement(ns + itemType,
                        new XAttribute("Include", row.itemName),
                        new XAttribute("ConfigName", row.configName)));
                if (!qtMsBuild.SetCommandLine(itemType, item, row.commandLine))
                    return false;
            }
            return true;
        }

        IEnumerable<XElement> GetCustomBuilds(string toolExecName)
        {
            return this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "CustomBuild")
                .Where(x => x.Elements(ns + "Command")
                    .Where(y => (y.Value.Contains(toolExecName))).Any());
        }

        void FinalizeProjectChanges(List<XElement> customBuilds, string itemTypeName)
        {
            customBuilds
                .Elements().Where(
                    elem => elem.Name.LocalName != itemTypeName)
                .ToList().ForEach(oldElem => oldElem.Remove());

            customBuilds.Elements(ns + itemTypeName).ToList().ForEach(item =>
            {
                item.Elements().ToList().ForEach(prop =>
                {
                    string configName = prop.Parent.Attribute("ConfigName").Value;
                    prop.SetAttributeValue("Condition",
                        string.Format("'$(Configuration)|$(Platform)'=='{0}'", configName));
                    prop.Remove();
                    item.Parent.Add(prop);
                });
                item.Remove();
            });

            customBuilds.ForEach(customBuild =>
            {
                var filterCustomBuild = this[Files.Filters].xml
                    .Elements(ns + "Project")
                    .Elements(ns + "ItemGroup")
                    .Elements(ns + "CustomBuild")
                    .Where(filterItem =>
                        filterItem.Attribute("Include").Value
                        == customBuild.Attribute("Include").Value)
                    .FirstOrDefault();
                if (filterCustomBuild != null) {
                    filterCustomBuild.Name = ns + itemTypeName;
                    this[Files.Filters].isDirty = true;
                }

                customBuild.Name = ns + itemTypeName;
                this[Files.Project].isDirty = true;
            });
        }

        string AddGeneratedFilesPath(string includePathList)
        {
            HashSet<string> includes = new HashSet<string> {
                    QtVSIPSettings.GetMocDirectory(),
                    QtVSIPSettings.GetRccDirectory(),
                    QtVSIPSettings.GetUicDirectory(),
                };
            foreach (var includePath in includePathList.Split(new char[] { ';' }))
                includes.Add(includePath);
            return string.Join<string>(";", includes);
        }

        string CustomBuildMocInput(XElement cbt)
        {
            var commandLine = (string)cbt.Element(ns + "Command");
            Dictionary<QtMoc.Property, string> properties;
            if (!QtMsBuildContainer.QtMocInstance.ParseCommandLine(commandLine, out properties))
                return (string)cbt.Attribute("Include");
            string ouputFile;
            if (!properties.TryGetValue(QtMoc.Property.InputFile, out ouputFile))
                return (string)cbt.Attribute("Include");
            return ouputFile;
        }

        void RemoveGeneratedFiles(
            List<CustomBuildEval> cbEvals,
            string configName,
            string itemName,
            IEnumerable<XElement> projectItems,
            IEnumerable<XElement> filterItems)
        {
            //remove items with generated files
            var cbEval = cbEvals
                .Where(x => x.ProjectConfig == configName && x.Identity == itemName)
                .FirstOrDefault();
            if (cbEval != null) {
                var outputFiles = cbEval.Outputs.Split(new char[] { ';' });
                foreach (var outputFile in outputFiles) {
                    if (!string.IsNullOrEmpty(outputFile)) {
                        var outputItems = new List<XElement>();
                        outputItems.AddRange(projectItems
                            .Where(x => HelperFunctions.PathEquals(
                                outputFile, (string)x.Attribute("Include"))));
                        outputItems.AddRange(filterItems
                            .Where(x => HelperFunctions.PathEquals(
                                outputFile, (string)x.Attribute("Include"))));
                        foreach (var item in outputItems)
                            item.Remove();
                    }
                }
            }
        }

        public bool ConvertCustomBuildToQtMsBuild()
        {
            var cbEvals = EvaluateCustomBuild();

            var qtMsBuild = new QtMsBuildContainer(new MsBuildConverterProvider());
            qtMsBuild.BeginSetItemProperties();

            var configNames = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "ProjectConfiguration")
                .Select(x => (string)x.Attribute("Include"));

            var projectItems = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements()
                .Where(x => ((string)x.Attribute("Include"))
                .IndexOfAny(Path.GetInvalidPathChars()) == -1);


            var filterItems = this[Files.Filters].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements()
                .Where(x => projectItems.Where(y =>
                    ((string)x.Attribute("Include")).Equals((string)y.Attribute("Include"),
                    StringComparison.InvariantCultureIgnoreCase)).Any());

            var cppIncludePaths = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Elements(ns + "ClCompile")
                .Elements(ns + "AdditionalIncludeDirectories");

            //add generated files path to C++ additional include dirs
            foreach (var cppIncludePath in cppIncludePaths)
                cppIncludePath.Value = AddGeneratedFilesPath((string)cppIncludePath);

            // replace each set of .moc.cbt custom build steps
            // with a single .cpp custom build step
            var mocCbtCustomBuilds = GetCustomBuilds(QtMoc.ToolExecName)
                .Where(x =>
                ((string)x.Attribute("Include")).EndsWith(".moc.cbt",
                StringComparison.InvariantCultureIgnoreCase))
                .GroupBy(cbt => CustomBuildMocInput(cbt));

            List<XElement> cbtToRemove = new List<XElement>();
            foreach (var cbtGroup in mocCbtCustomBuilds) {

                //create new CustomBuild item for .cpp
                var newCbt = new XElement(ns + "CustomBuild",
                    new XAttribute("Include", cbtGroup.Key),
                    new XElement(ns + "FileType", "Document"));

                //add properties from .moc.cbt items
                List<string> cbtPropertyNames = new List<string> {
                    "AdditionalInputs",
                    "Command",
                    "Message",
                    "Outputs",
                };
                foreach (var cbt in cbtGroup) {
                    var enabledProperties = cbt.Elements().Where(x =>
                        cbtPropertyNames.Contains(x.Name.LocalName)
                        && !x.Parent.Elements(ns + "ExcludedFromBuild").Where(y =>
                        (string)x.Attribute("Condition") == (string)y.Attribute("Condition"))
                        .Any());
                    foreach (var property in enabledProperties)
                        newCbt.Add(new XElement(property));
                    cbtToRemove.Add(cbt);
                }
                cbtGroup.First().AddBeforeSelf(newCbt);

                //remove ClCompile item (cannot have duplicate items)
                var cppMocItems = this[Files.Project].xml
                    .Elements(ns + "Project")
                    .Elements(ns + "ItemGroup")
                    .Elements(ns + "ClCompile")
                    .Where(x =>
                        cbtGroup.Key.Equals((string)x.Attribute("Include"),
                        StringComparison.InvariantCultureIgnoreCase));
                foreach (var cppMocItem in cppMocItems)
                    cppMocItem.Remove();

                //change type of item in filter
                cppMocItems = this[Files.Filters].xml
                    .Elements(ns + "Project")
                    .Elements(ns + "ItemGroup")
                    .Elements(ns + "ClCompile")
                    .Where(x =>
                        cbtGroup.Key.Equals((string)x.Attribute("Include"),
                        StringComparison.InvariantCultureIgnoreCase));
                foreach (var cppMocItem in cppMocItems)
                    cppMocItem.Name = ns + "CustomBuild";
            }

            //remove .moc.cbt CustomBuild items
            cbtToRemove.ForEach(x => x.Remove());

            //convert moc custom build steps
            var mocCustomBuilds = GetCustomBuilds(QtMoc.ToolExecName);
            if (!SetCommandLines(qtMsBuild, configNames, mocCustomBuilds, QtMoc.ItemTypeName))
                return false;
            foreach (var qtMoc in mocCustomBuilds.Elements(ns + QtMoc.ItemTypeName)) {
                var itemName = (string)qtMoc.Attribute("Include");
                var configName = (string)qtMoc.Attribute("ConfigName");

                //remove items with generated files
                RemoveGeneratedFiles(cbEvals, configName, itemName, projectItems, filterItems);

                //set properties
                qtMsBuild.SetItemProperty(qtMoc,
                    QtMoc.Property.ExecutionDescription, "Moc'ing %(Identity)...");
                qtMsBuild.SetItemProperty(qtMoc,
                    QtMoc.Property.InputFile, "%(FullPath)");
                if (!HelperFunctions.IsSourceFile(itemName)) {
                    qtMsBuild.SetItemProperty(qtMoc,
                        QtMoc.Property.OutputFile, string.Format(@"{0}\moc_%(Filename).cpp",
                        QtVSIPSettings.GetMocDirectory()));
                    qtMsBuild.SetItemProperty(qtMoc,
                        QtMoc.Property.DynamicSource, "output");
                } else {
                    qtMsBuild.SetItemProperty(qtMoc,
                        QtMoc.Property.OutputFile, string.Format(@"{0}\%(Filename).moc",
                        QtVSIPSettings.GetMocDirectory()));
                    qtMsBuild.SetItemProperty(qtMoc,
                        QtMoc.Property.DynamicSource, "input");
                }
                var includePath = qtMsBuild.GetPropertyChangedValue(
                    QtMoc.Property.IncludePath, itemName, configName);
                if (!string.IsNullOrEmpty(includePath)) {
                    qtMsBuild.SetItemProperty(qtMoc,
                        QtMoc.Property.IncludePath, AddGeneratedFilesPath(includePath));
                }
            }

            //convert rcc custom build steps
            var rccCustomBuilds = GetCustomBuilds(QtRcc.ToolExecName);
            if (!SetCommandLines(qtMsBuild, configNames, rccCustomBuilds, QtRcc.ItemTypeName))
                return false;
            foreach (var qtRcc in rccCustomBuilds.Elements(ns + QtRcc.ItemTypeName)) {
                var itemName = (string)qtRcc.Attribute("Include");
                var configName = (string)qtRcc.Attribute("ConfigName");

                //remove items with generated files
                RemoveGeneratedFiles(cbEvals, configName, itemName, projectItems, filterItems);

                //set properties
                qtMsBuild.SetItemProperty(qtRcc,
                    QtRcc.Property.ExecutionDescription, "Rcc'ing %(Identity)...");
                qtMsBuild.SetItemProperty(qtRcc,
                    QtRcc.Property.InputFile, "%(FullPath)");
                qtMsBuild.SetItemProperty(qtRcc,
                    QtRcc.Property.OutputFile, string.Format(@"{0}\qrc_%(Filename).cpp",
                    QtVSIPSettings.GetRccDirectory()));
            }

            //convert uic custom build steps
            var uicCustomBuilds = GetCustomBuilds(QtUic.ToolExecName);
            if (!SetCommandLines(qtMsBuild, configNames, uicCustomBuilds, QtUic.ItemTypeName))
                return false;
            foreach (var qtUic in uicCustomBuilds.Elements(ns + QtUic.ItemTypeName)) {
                var itemName = (string)qtUic.Attribute("Include");
                var configName = (string)qtUic.Attribute("ConfigName");

                //remove items with generated files
                RemoveGeneratedFiles(cbEvals, configName, itemName, projectItems, filterItems);

                //set properties
                qtMsBuild.SetItemProperty(qtUic,
                    QtUic.Property.ExecutionDescription, "Uic'ing %(Identity)...");
                qtMsBuild.SetItemProperty(qtUic,
                    QtUic.Property.InputFile, "%(FullPath)");
                qtMsBuild.SetItemProperty(qtUic,
                    QtUic.Property.OutputFile, string.Format(@"{0}\ui_%(Filename).h",
                    QtVSIPSettings.GetRccDirectory()));
            }

            qtMsBuild.EndSetItemProperties();

            FinalizeProjectChanges(mocCustomBuilds.ToList(), QtMoc.ItemTypeName);
            FinalizeProjectChanges(rccCustomBuilds.ToList(), QtRcc.ItemTypeName);
            FinalizeProjectChanges(uicCustomBuilds.ToList(), QtUic.ItemTypeName);

            return true;
        }

        bool TryReplaceTextInPlace(ref string text, Regex findWhat, string newText)
        {
            var match = findWhat.Match(text);
            if (!match.Success)
                return false;
            do {
                text = text.Remove(match.Index, match.Length).Insert(match.Index, newText);
                match = findWhat.Match(text, match.Index);
            } while (match.Success);

            return true;
        }

        void ReplaceText(XElement xElem, Regex findWhat, string newText)
        {
            var elemValue = (string)xElem;
            if (!string.IsNullOrEmpty(elemValue)
                && TryReplaceTextInPlace(ref elemValue, findWhat, newText)) {
                xElem.Value = elemValue;
            }
        }

        void ReplaceText(XAttribute xAttr, Regex findWhat, string newText)
        {
            var attrValue = (string)xAttr;
            if (!string.IsNullOrEmpty(attrValue)
                && TryReplaceTextInPlace(ref attrValue, findWhat, newText)) {
                xAttr.Value = attrValue;
            }
        }

        public void ReplacePath(string oldPath, string newPath)
        {
            var findWhat = new Regex(oldPath
                .Replace("\\", "[\\\\\\/]")
                .Replace(".", "\\.")
                .Replace("$", "\\$"),
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            foreach (var xElem in this[Files.Project].xml.Descendants()) {
                if (!xElem.HasElements)
                    ReplaceText(xElem, findWhat, newPath);
                foreach (var xAttr in xElem.Attributes())
                    ReplaceText(xAttr, findWhat, newPath);
            }
        }

        class CustomBuildEval
        {
            public string ProjectConfig { get; set; }
            public string Identity { get; set; }
            public string AdditionalInputs { get; set; }
            public string Outputs { get; set; }
            public string Message { get; set; }
            public string Command { get; set; }
        }

        List<CustomBuildEval> EvaluateCustomBuild()
        {
            var eval = new List<CustomBuildEval>();
            var evaluateTarget = new XElement(ns + "Target",
                new XAttribute("Name", "EvaluateCustomBuild"),
                new XElement(ns + "PropertyGroup",
                    new XElement(ns + "CustomBuildEval", "@(CustomBuild->'" +
                    "{%(Identity)}" +
                    "{%(AdditionalInputs)}" +
                    "{%(Outputs)}" +
                    "{%(Message)}" +
                    "{%(Command)}')")));
            this[Files.Project].xml.Root.Add(evaluateTarget);

            var tempProjFile = Path.Combine(
                Path.GetDirectoryName(this[Files.Project].filePath),
                Path.GetRandomFileName());
            if (File.Exists(tempProjFile))
                File.Delete(tempProjFile);
            this[Files.Project].xml.Save(tempProjFile);
            var projRoot = ProjectRootElement.Open(tempProjFile);

            var pattern = new Regex(@"{([^}]+)}{([^}]+)}{([^}]+)}{([^}]+)}{([^}]+)}");

            var projConfigs = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "ProjectConfiguration");
            foreach (var projConfig in projConfigs) {
                string configName = (string)projConfig.Attribute("Include");
                var properties = new Dictionary<string, string>();
                foreach (var configProp in projConfig.Elements())
                    properties.Add(configProp.Name.LocalName, (string)configProp);
                var projInst = new ProjectInstance(projRoot, properties,
                    null, new ProjectCollection());
                var buildRequest = new BuildRequestData(
                    projInst, new string[] { "EvaluateCustomBuild" },
                    null, BuildRequestDataFlags.ProvideProjectStateAfterBuild);
                var buildResult = BuildManager.DefaultBuildManager.Build(
                    new BuildParameters(), buildRequest);
                string customBuildEval = buildResult.ProjectStateAfterBuild
                    .GetPropertyValue("CustomBuildEval");
                foreach (Match cbEval in pattern.Matches(customBuildEval)) {
                    eval.Add(new CustomBuildEval {
                        ProjectConfig = configName,
                        Identity = cbEval.Groups[1].Value,
                        AdditionalInputs = cbEval.Groups[2].Value,
                        Outputs = cbEval.Groups[3].Value,
                        Message = cbEval.Groups[4].Value,
                        Command = cbEval.Groups[5].Value,
                    });
                }
            }

            evaluateTarget.Remove();
            File.Delete(tempProjFile);
            return eval;
        }

        static Regex ConditionParser =
            new Regex(@"\'\$\(Configuration[^\)]*\)\|\$\(Platform[^\)]*\)\'\=\=\'([^\']+)\'");

        class MsBuildConverterProvider : IPropertyStorageProvider
        {
            public string GetProperty(object propertyStorage, string itemType, string propertyName)
            {
                XElement xmlPropertyStorage = propertyStorage as XElement;
                if (xmlPropertyStorage == null)
                    return "";
                XElement item;
                if (xmlPropertyStorage.Name.LocalName == "ItemDefinitionGroup") {
                    item = xmlPropertyStorage.Element(ns + itemType);
                    if (item == null)
                        return "";
                } else {
                    item = xmlPropertyStorage;
                }
                var prop = item.Element(ns + propertyName);
                if (prop == null)
                    return "";
                return prop.Value;
            }

            public bool SetProperty(
                object propertyStorage,
                string itemType,
                string propertyName,
                string propertyValue)
            {
                XElement xmlPropertyStorage = propertyStorage as XElement;
                if (xmlPropertyStorage == null)
                    return false;
                XElement item;
                if (xmlPropertyStorage.Name.LocalName == "ItemDefinitionGroup") {
                    item = xmlPropertyStorage.Element(ns + itemType);
                    if (item == null)
                        xmlPropertyStorage.Add(item = new XElement(ns + itemType));
                } else {
                    item = xmlPropertyStorage;
                }
                var prop = item.Element(ns + propertyName);
                if (prop != null)
                    prop.Value = propertyValue;
                else
                    item.Add(new XElement(ns + propertyName, propertyValue));
                return true;
            }

            public bool DeleteProperty(
                object propertyStorage,
                string itemType,
                string propertyName)
            {
                XElement xmlPropertyStorage = propertyStorage as XElement;
                if (xmlPropertyStorage == null)
                    return false;
                XElement item;
                if (xmlPropertyStorage.Name.LocalName == "ItemDefinitionGroup") {
                    item = xmlPropertyStorage.Element(ns + itemType);
                    if (item == null)
                        return true;
                } else {
                    item = xmlPropertyStorage;
                }

                var prop = item.Element(ns + propertyName);
                if (prop != null)
                    prop.Remove();
                return true;
            }

            public string GetConfigName(object propertyStorage)
            {
                XElement xmlPropertyStorage = propertyStorage as XElement;
                if (xmlPropertyStorage == null)
                    return "";
                if (xmlPropertyStorage.Name.LocalName == "ItemDefinitionGroup") {
                    var configName = ConditionParser
                        .Match(xmlPropertyStorage.Attribute("Condition").Value);
                    if (!configName.Success || configName.Groups.Count <= 1)
                        return "";
                    return configName.Groups[1].Value;
                }
                return xmlPropertyStorage.Attribute("ConfigName").Value;
            }

            public string GetItemType(object propertyStorage)
            {
                XElement xmlPropertyStorage = propertyStorage as XElement;
                if (xmlPropertyStorage == null)
                    return "";
                if (xmlPropertyStorage.Name.LocalName == "ItemDefinitionGroup")
                    return "";
                return xmlPropertyStorage.Name.LocalName;
            }

            public string GetItemName(object propertyStorage)
            {
                XElement xmlPropertyStorage = propertyStorage as XElement;
                if (xmlPropertyStorage == null)
                    return "";
                if (xmlPropertyStorage.Name.LocalName == "ItemDefinitionGroup")
                    return "";
                return xmlPropertyStorage.Attribute("Include").Value;
            }

            public object GetParentProject(object propertyStorage)
            {
                XElement xmlPropertyStorage = propertyStorage as XElement;
                if (xmlPropertyStorage == null)
                    return "";
                if (xmlPropertyStorage.Document == null)
                    return null;
                return xmlPropertyStorage.Document.Root;
            }

            public object GetProjectConfiguration(object project, string configName)
            {
                XElement xmlProject = project as XElement;
                if (xmlProject == null)
                    return null;
                return xmlProject.Elements(ns + "ItemDefinitionGroup")
                    .Where(config => config.Attribute("Condition").Value.Contains(configName))
                    .FirstOrDefault();
            }

            public IEnumerable<object> GetItems(
                object project,
                string itemType,
                string configName = "")
            {
                XElement xmlProject = project as XElement;
                if (xmlProject == null)
                    return new List<object>();
                return
                    xmlProject.Elements(ns + "ItemGroup")
                    .Elements(ns + "CustomBuild")
                    .Elements(ns + itemType)
                    .Where(item => (
                        configName == ""
                        || item.Attribute("ConfigName").Value == configName))
                    .GroupBy(item => item.Attribute("Include").Value)
                    .Select(item => item.First());
            }
        }

    }
}
