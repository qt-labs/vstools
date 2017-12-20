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
                    new XAttribute("Condition", "'$(QtMsBuild)'==''"),
                    new XElement(ns + "QtMsBuild",
                        @"$(Registry:HKEY_CURRENT_USER\Environment@QtMsBuild)")),

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

        public bool ConvertCustomBuildToQtMsBuild()
        {
            var qtMsBuild = new QtMsBuildContainer(new MsBuildConverterProvider());
            qtMsBuild.BeginSetItemProperties();

            var configurations = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "ProjectConfiguration")
                .Select(x => x.Attribute("Include").Value);

            var mocCustomBuilds = GetCustomBuilds(QtMoc.ToolExecName);
            var rccCustomBuilds = GetCustomBuilds(QtRcc.ToolExecName);
            var uicCustomBuilds = GetCustomBuilds(QtUic.ToolExecName);

            if (!SetCommandLines(qtMsBuild, configurations, mocCustomBuilds, QtMoc.ItemTypeName))
                return false;

            if (!SetCommandLines(qtMsBuild, configurations, rccCustomBuilds, QtRcc.ItemTypeName))
                return false;

            if (!SetCommandLines(qtMsBuild, configurations, uicCustomBuilds, QtUic.ItemTypeName))
                return false;

            qtMsBuild.EndSetItemProperties();

            FinalizeProjectChanges(mocCustomBuilds.ToList(), QtMoc.ItemTypeName);
            FinalizeProjectChanges(rccCustomBuilds.ToList(), QtRcc.ItemTypeName);
            FinalizeProjectChanges(uicCustomBuilds.ToList(), QtUic.ItemTypeName);

            return true;
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
