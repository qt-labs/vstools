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
using QtVsTools.VisualStudio;
using EnvDTE;

namespace QtProjectLib
{
    using static QtVsTools.SyntaxAnalysis.RegExpr;
    using static HelperFunctions;

    public class MsBuildProject
    {
        class MsBuildXmlFile
        {
            public string filePath = "";
            public XDocument xml = null;
            public bool isDirty = false;
            public XDocument xmlCommitted = null;
            public bool isCommittedDirty = false;
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
                    xmlFile.xml = XDocument.Load(reader, LoadOptions.SetLineInfo);
                }
            } catch (Exception) {
                return false;
            }
            xmlFile.xmlCommitted = new XDocument(xmlFile.xml);
            return true;
        }

        public bool Save()
        {
            foreach (var file in files) {
                if (file.isDirty) {
                    file.xml?.Save(file.filePath, SaveOptions.None);
                    file.isCommittedDirty = file.isDirty = false;
                }
            }
            return true;
        }

        void Commit()
        {
            foreach (var file in files.Where(x => x.xml != null)) {
                if (file.isDirty) {
                    //file was modified: sync committed copy
                    file.xmlCommitted = new XDocument(file.xml);
                    file.isCommittedDirty = true;
                } else {
                    //fail-safe: ensure non-dirty files are in sync with committed copy
                    file.xml = new XDocument(file.xmlCommitted);
                    file.isDirty = file.isCommittedDirty;
                }
            }
        }

        void Rollback()
        {
            foreach (var file in files.Where(x => x.xml != null)) {
                file.xml = new XDocument(file.xmlCommitted);
                file.isDirty = file.isCommittedDirty;
            }
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
            var xClCompileDefs = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Elements(ns + "ClCompile");
            foreach (var xClCompileDef in xClCompileDefs)
                xClCompileDef.Add(new XElement(ns + "MultiProcessorCompilation", "true"));

            this[Files.Project].isDirty = true;
            Commit();
            return true;
        }

        /// <summary>
        /// Parser for project configuration conditional expressions of the type:
        ///
        ///     '$(Configuration)|$(Platform)'=='_TOKEN_|_TOKEN_'
        ///
        /// </summary>
        Parser _ConfigCondition;
        Parser ConfigCondition
        {
            get
            {
                if (_ConfigCondition == null) {
                    var config = new Token("Configuration", CharWord.Repeat());
                    var platform = new Token("Platform", CharWord.Repeat());
                    var expr = "'$(Configuration)|$(Platform)'=='" & config & "|" & platform & "'";
                    try {
                        _ConfigCondition = expr.Render();
                    } catch { }
                }
                return _ConfigCondition;
            }
        }

        /// <summary>
        /// Parser for project format version string:
        ///
        ///     QtVS_vNNN
        ///
        /// </summary>
        Parser _ProjectFormatVersion;
        Parser ProjectFormatVersion
        {
            get
            {
                if (_ProjectFormatVersion == null) {
                    var expr = "QtVS_v" & new Token("VERSION", Char['0', '9'].Repeat(3))
                    {
                        new Rule<int> { Capture(value => int.Parse(value)) }
                    };
                    try {
                        _ProjectFormatVersion = expr.Render();
                    } catch { }
                }
                return _ProjectFormatVersion;
            }
        }

        int? ParseProjectFormatVersion(string text)
        {
            if (ProjectFormatVersion == null)
                return null;
            try {
                return ProjectFormatVersion.Parse(text)
                    .GetValues<int>("VERSION")
                    .First();
            } catch {
                return null;
            }
        }

        const StringComparison IGNORE_CASE = StringComparison.InvariantCultureIgnoreCase;
        readonly StringComparer IGNORE_CASE_ = StringComparer.InvariantCultureIgnoreCase;

        /// <summary>
        /// Converts project format version to the latest version:
        ///  * Set latest project version;
        ///  * Add QtSettings property group;
        ///  * Set QtInstall property;
        ///  * Remove hard-coded macros, include paths and libs related to Qt modules.
        ///  * Set QtModules property;
        /// </summary>
        /// <returns>true if successful</returns>
        public bool UpdateProjectFormatVersion()
        {
            if (ConfigCondition == null)
                return false;

            // Get default Qt dir
            string defaultQtDir = null;
            var defaultVersionName = QtVersionManager.The().GetDefaultVersion();
            var defaultVersion = QtVersionManager.The().GetVersionInfo(defaultVersionName);
            if (defaultVersion != null)
                defaultQtDir = defaultVersion.qtDir;

            // Get project global properties
            var globals = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Where(x => (string)x.Attribute("Label") == "Globals")
                .FirstOrDefault();
            if (globals == null)
                return false;

            // Set Qt project format version
            var projKeyword = globals.Element(ns + "Keyword");
            if (projKeyword == null)
                return false;
            var oldVersion = ParseProjectFormatVersion(projKeyword.Value);
            if (oldVersion.HasValue && oldVersion.Value == Resources.qtProjectFormatVersion)
                return true; // nothing to do!

            projKeyword.SetValue(string.Format("QtVS_v{0}", Resources.qtProjectFormatVersion));

            // Find import of qt.props
            var qtPropsImport = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ImportGroup")
                .Elements(ns + "Import")
                .Where(x => (string)x.Attribute("Project") == @"$(QtMsBuild)\qt.props")
                .FirstOrDefault();
            if (qtPropsImport == null)
                return false;

            // Upgrading from v3.0?
            Dictionary<string, XElement> oldQtInstall = null;
            Dictionary<string, XElement> oldQtSettings = null;
            if (oldVersion.HasValue && oldVersion.Value == Resources.qtMinFormatVersion_Settings) {
                oldQtInstall = this[Files.Project].xml
                    .Elements(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Elements(ns + "QtInstall")
                    .ToDictionary(x => (string)x.Parent.Attribute("Condition"));
                oldQtInstall.Values.ToList()
                    .ForEach(x => x.Remove());

                oldQtSettings = this[Files.Project].xml
                    .Elements(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Where(x => (string)x.Attribute("Label") == "QtSettings")
                    .ToDictionary(x => (string)x.Attribute("Condition"));
                oldQtSettings.Values.ToList()
                    .ForEach(x => x.Remove());
            }

            // Get project configurations
            var configs = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "ProjectConfiguration");

            // Find location for import of qt.props and for the QtSettings property group:
            // (cf. ".vcxproj file elements" https://docs.microsoft.com/en-us/cpp/build/reference/vcxproj-file-structure?view=vs-2019#vcxproj-file-elements)
            XElement insertionPoint = null;

            // * After the last UserMacros property group
            insertionPoint = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Where(x => (string)x.Attribute("Label") == "UserMacros")
                .LastOrDefault();

            // * After the last PropertySheets import group
            insertionPoint = (insertionPoint != null) ? insertionPoint : this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ImportGroup")
                .Where(x => (string)x.Attribute("Label") == "PropertySheets")
                .LastOrDefault();

            // * Before the first ItemDefinitionGroup
            insertionPoint = (insertionPoint != null) ? insertionPoint : this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Select(x => x.ElementsBeforeSelf().Last())
                .FirstOrDefault();

            // * Before the first ItemGroup
            insertionPoint = (insertionPoint != null) ? insertionPoint : this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Select(x => x.ElementsBeforeSelf().Last())
                .FirstOrDefault();

            // * Before the import of Microsoft.Cpp.targets
            insertionPoint = (insertionPoint != null) ? insertionPoint : this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "Import")
                .Where(x =>
                    (string)x.Attribute("Project") == @"$(VCTargetsPath)\Microsoft.Cpp.targets")
                .Select(x => x.ElementsBeforeSelf().Last())
                .FirstOrDefault();

            // * At the end of the file
            insertionPoint = (insertionPoint != null) ? insertionPoint : this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements()
                .LastOrDefault();

            if (insertionPoint == null)
                return false;

            // Move import of qt.props to insertion point
            if (qtPropsImport.Parent.Elements().SingleOrDefault() == qtPropsImport)
                qtPropsImport.Parent.Remove(); // Remove import group
            else
                qtPropsImport.Remove(); // Remove import (group contains other imports)
            insertionPoint.AddAfterSelf(
                new XElement(ns + "ImportGroup",
                    new XAttribute("Condition", @"Exists('$(QtMsBuild)\qt.props')"),
                    new XElement(ns + "Import",
                        new XAttribute("Project", @"$(QtMsBuild)\qt.props"))));

            // Create QtSettings property group above import of qt.props
            var qtSettings = new List<XElement>();
            foreach (var config in configs) {
                var configQtSettings = new XElement(ns + "PropertyGroup",
                        new XAttribute("Label", "QtSettings"),
                        new XAttribute("Condition",
                            string.Format("'$(Configuration)|$(Platform)'=='{0}'",
                            (string)config.Attribute("Include"))));
                insertionPoint.AddAfterSelf(configQtSettings);
                qtSettings.Add(configQtSettings);
            }

            // Add import of default property values
            insertionPoint.AddAfterSelf(
                new XElement(ns + "ImportGroup",
                    new XAttribute("Condition", @"Exists('$(QtMsBuild)\qt_defaults.props')"),
                    new XElement(ns + "Import",
                        new XAttribute("Project", @"$(QtMsBuild)\qt_defaults.props"))));

            //// Upgrading from v3.0: move Qt settings to newly created import groups
            if (oldVersion.HasValue && oldVersion.Value == Resources.qtMinFormatVersion_Settings) {
                foreach (var configQtSettings in qtSettings) {
                    var configCondition = (string)configQtSettings.Attribute("Condition");

                    XElement oldConfigQtInstall;
                    if (oldQtInstall.TryGetValue(configCondition, out oldConfigQtInstall))
                        configQtSettings.Add(oldConfigQtInstall);

                    XElement oldConfigQtSettings;
                    if (oldQtSettings.TryGetValue(configCondition, out oldConfigQtSettings)) {
                        foreach (var qtSetting in oldConfigQtSettings.Elements())
                            configQtSettings.Add(qtSetting);
                    }
                }

                this[Files.Project].isDirty = true;
                Commit();
                return true;
            }

            //// Upgrading from v2.0

            // Get project user properties (old format)
            var userProps = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ProjectExtensions")
                .Elements(ns + "VisualStudio")
                .Elements(ns + "UserProperties")
                .FirstOrDefault();

            // Copy Qt build reference to QtInstall project property
            this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Where(x => ((string)x.Attribute("Label")) == Resources.projLabelQtSettings)
                .ToList()
                .ForEach(config =>
                {
                    string qtInstallValue = defaultVersionName;
                    if (userProps != null) {
                        string platform = null;
                        try {
                            platform = ConfigCondition
                                .Parse((string)config.Attribute("Condition"))
                                .GetValues<string>("Platform")
                                .FirstOrDefault();
                        } catch { }

                        if (!string.IsNullOrEmpty(platform)) {
                            var qtInstallName = string.Format("Qt5Version_x0020_{0}", platform);
                            qtInstallValue = (string)userProps.Attribute(qtInstallName);
                        }
                    }
                    if (!string.IsNullOrEmpty(qtInstallValue))
                        config.Add(new XElement(ns + "QtInstall", qtInstallValue));
                });

            // Get C++ compiler properties
            var compiler = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Elements(ns + "ClCompile");

            // Get linker properties
            var linker = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Elements(ns + "Link");

            // Qt module names, to copy to QtModules property
            var moduleNames = new HashSet<string>();

            // Qt module macros, to remove from compiler macros property
            var moduleDefines = new HashSet<string>();

            // Qt module includes, to remove from compiler include directories property
            var moduleIncludePaths = new HashSet<string>();

            // Qt module link libraries, to remove from liker dependencies property
            var moduleLibs = new HashSet<string>();

            // Go through all known Qt modules and check which ones are currently being used
            foreach (var module in QtModules.Instance.GetAvailableModuleInformation()) {

                if (IsModuleUsed(module, compiler, linker)) {

                    // Qt module names, to copy to QtModules property
                    if (!string.IsNullOrEmpty(module.proVarQT))
                        moduleNames.UnionWith(module.proVarQT.Split(' '));

                    // Qt module macros, to remove from compiler macros property
                    moduleDefines.UnionWith(module.Defines);

                    // Qt module includes, to remove from compiler include directories property
                    moduleIncludePaths.UnionWith(
                        module.IncludePath.Select(x => Path.GetFileName(x)));

                    // Qt module link libraries, to remove from liker dependencies property
                    moduleLibs.UnionWith(
                        module.AdditionalLibraries.Select(x => Path.GetFileName(x)));
                    moduleLibs.UnionWith(
                        module.AdditionalLibrariesDebug.Select(x => Path.GetFileName(x)));
                    moduleLibs.Add(module.LibRelease);
                    moduleLibs.Add(module.LibDebug);
                }
            }

            // Remove Qt module macros from compiler properties
            foreach (var defines in compiler.Elements(ns + "PreprocessorDefinitions")) {
                defines.SetValue(string.Join(";", defines.Value.Split(';')
                    .Where(x => !moduleDefines.Contains(x))));
            }

            // Remove Qt module include paths from compiler properties
            foreach (var inclPath in compiler.Elements(ns + "AdditionalIncludeDirectories")) {
                inclPath.SetValue(string.Join(";", inclPath.Value.Split(';')
                    .Select(x => Unquote(x))
                    .Where(x =>
                        // Exclude include paths of Qt modules
                        !moduleIncludePaths.Contains(Path.GetFileName(x), IGNORE_CASE_)
                        // Exclude paths rooted on $(QTDIR)
                        || (!x.StartsWith("$(QTDIR)", IGNORE_CASE)
                            // Exclude paths rooted on the default Qt dir
                            && (string.IsNullOrEmpty(defaultQtDir)
                                || !x.StartsWith(defaultQtDir, IGNORE_CASE))))));
            }

            // Remove Qt module libraries from linker properties
            foreach (var libs in linker.Elements(ns + "AdditionalDependencies")) {
                libs.SetValue(string.Join(";", libs.Value.Split(';')
                    .Where(x => !moduleLibs.Contains(Path.GetFileName(x), IGNORE_CASE_))));
            }

            // Add Qt module names to QtModules project property
            this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Where(x => ((string)x.Attribute("Label")) == Resources.projLabelQtSettings)
                .ToList()
                .ForEach(x => x.Add(new XElement(ns + "QtModules", string.Join(";", moduleNames))));

            // Remove project user properties (old format)
            if (userProps != null) {
                userProps.Attributes().ToList().ForEach(userProp =>
                {
                    if (userProp.Name.LocalName == "lupdateOptions"
                        || userProp.Name.LocalName == "lupdateOnBuild"
                        || userProp.Name.LocalName == "lreleaseOptions"
                        || userProp.Name.LocalName == "MocDir"
                        || userProp.Name.LocalName == "MocOptions"
                        || userProp.Name.LocalName == "RccDir"
                        || userProp.Name.LocalName == "UicDir"
                        || userProp.Name.LocalName.StartsWith("Qt5Version_x0020_"))
                    {
                        userProp.Remove();
                    }
                });
            }

            // Remove old properties from .user file
            if (this[Files.User].xml != null) {
                this[Files.User].xml
                    .Elements(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Elements()
                    .ToList()
                    .ForEach(userProp =>
                    {
                        if (userProp.Name.LocalName == "QTDIR"
                            || userProp.Name.LocalName == "QmlDebug"
                            || userProp.Name.LocalName == "QmlDebugSettings"
                            || (userProp.Name.LocalName == "LocalDebuggerCommandArguments"
                                && (string)userProp == "$(QmlDebug)"
                            )
                            || (userProp.Name.LocalName == "LocalDebuggerEnvironment"
                                && (string)userProp == "PATH=$(QTDIR)\\bin%3b$(PATH)"
                            )
                        ) {
                            userProp.Remove();
                        }
                    });
                this[Files.User].isDirty = true;
            }

            // Convert OutputFile --> <tool>Dir + <tool>FileName
            var qtItems = this[Files.Project].xml
                .Elements(ns + "Project")
                .SelectMany(x => x.Elements(ns + "ItemDefinitionGroup")
                    .Union(x.Elements(ns + "ItemGroup")))
                .SelectMany(x => x.Elements(ns + "QtMoc")
                    .Union(x.Elements(ns + "QtRcc"))
                    .Union(x.Elements(ns + "QtUic")));
            foreach (var qtItem in qtItems) {
                var outputFile = qtItem.Element(ns + "OutputFile");
                if (outputFile != null) {
                    var qtTool = qtItem.Name.LocalName;
                    var outDir = Path.GetDirectoryName(outputFile.Value);
                    var outFileName = Path.GetFileName(outputFile.Value);
                    if (!string.IsNullOrEmpty(outDir))
                        qtItem.Add(new XElement(ns + qtTool + "Dir", outDir));
                    else
                        qtItem.Add(new XElement(ns + qtTool + "Dir", "$(ProjectDir)"));
                    qtItem.Add(new XElement(ns + qtTool + "FileName", outFileName));
                }
            }

            // Remove old properties from project items
            var oldQtProps = new[] { "QTDIR", "InputFile", "OutputFile" };
            var oldCppProps = new[] { "IncludePath", "Define", "Undefine" };
            var oldPropsAny = oldQtProps.Union(oldCppProps);
            this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Union(this[Files.Project].xml
                    .Elements(ns + "Project")
                    .Elements(ns + "ItemGroup"))
                .Elements().ToList().ForEach(item =>
                {
                    var itemName = item.Name.LocalName;
                    item.Elements().ToList().ForEach(itemProp =>
                    {
                        var propName = itemProp.Name.LocalName;
                        if ((itemName == "QtMoc" && oldPropsAny.Contains(propName))
                            || (itemName == "QtRcc" && oldQtProps.Contains(propName))
                            || (itemName == "QtUic" && oldQtProps.Contains(propName))
                            || (itemName == "QtRepc" && oldPropsAny.Contains(propName))
                        ) {
                            itemProp.Remove();
                        }
                    });
                });

            this[Files.Project].isDirty = true;
            Commit();
            return true;
        }

        bool IsModuleUsed(
            QtModuleInfo module,
            IEnumerable<XElement> compiler,
            IEnumerable<XElement> linker)
        {
            // Module .lib is present in linker additional dependencies
            if (linker.Elements(ns + "AdditionalDependencies")
                .SelectMany(x => x.Value.Split(';'))
                .Any(x => Path.GetFileName(x).Equals(module.LibRelease, IGNORE_CASE)
                    || Path.GetFileName(x).Equals(module.LibDebug, IGNORE_CASE))) {
                return true;
            }

            // Module macro is present in pre-processor definitions
            if (compiler.Elements(ns + "PreprocessorDefinitions")
                .SelectMany(x => x.Value.Split(';'))
                .Any(x => module.Defines.Contains(x))) {
                return true;
            }

            // Module is not present
            return false;
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

            this[Files.Project].isDirty = true;
            Commit();
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
            Commit();
            return true;
        }

        delegate string ItemCommandLineReplacement(string itemName, string cmdLine);

        bool SetCommandLines(
            QtMsBuildContainer qtMsBuild,
            IEnumerable<XElement> configurations,
            IEnumerable<XElement> customBuilds,
            string toolExec,
            string itemType,
            string workingDir,
            IEnumerable<ItemCommandLineReplacement> extraReplacements)
        {
            var query = from customBuild in customBuilds
                        let itemName = customBuild.Attribute("Include").Value
                        from config in configurations
                        from command in customBuild.Elements(ns + "Command")
                        where command.Attribute("Condition").Value
                            == string.Format(
                                "'$(Configuration)|$(Platform)'=='{0}'",
                                (string)config.Attribute("Include"))
                        select new { customBuild, itemName, config, command };

            var projPath = this[Files.Project].filePath;
            bool error = false;
            using (var evaluator = new MSBuildEvaluator(this[Files.Project])) {
                foreach (var row in query) {

                    var configId = (string)row.config.Attribute("Include");
                    if (!row.command.Value.Contains(toolExec)) {
                        Messages.PaneMessageSafe(VsServiceProvider.GetService<DTE>(), string.Format(
                            "{0}: warning: [{1}] converting \"{2}\", configuration \"{3}\": " +
                            "tool not found: \"{4}\"; applying default options",
                            projPath, itemType, row.itemName, configId, toolExec), 5000);
                        continue;
                    }

                    XElement item;
                    row.customBuild.Add(item =
                        new XElement(ns + itemType,
                            new XAttribute("Include", row.itemName),
                            new XAttribute("ConfigName", configId)));
                    var configName = (string)row.config.Element(ns + "Configuration");
                    var platformName = (string)row.config.Element(ns + "Platform");
                    var commandLine = row.command.Value
                        .Replace(Path.GetFileName(row.itemName), "%(Filename)%(Extension)",
                            StringComparison.InvariantCultureIgnoreCase)
                        .Replace(configName, "$(Configuration)",
                            StringComparison.InvariantCultureIgnoreCase)
                        .Replace(platformName, "$(Platform)",
                            StringComparison.InvariantCultureIgnoreCase);
                    foreach (var replace in extraReplacements)
                        commandLine = replace(row.itemName, commandLine);

                    evaluator.Properties.Clear();
                    foreach (var configProp in row.config.Elements())
                        evaluator.Properties.Add(configProp.Name.LocalName, (string)configProp);
                    if (!qtMsBuild.SetCommandLine(itemType, item, commandLine, evaluator)) {
                        int lineNumber = 1;
                        var errorLine = row.command as IXmlLineInfo;
                        if (errorLine != null && errorLine.HasLineInfo())
                            lineNumber = errorLine.LineNumber;

                        Messages.PaneMessageSafe(VsServiceProvider.GetService<DTE>(), string.Format(
                            "{0}({1}): error: [{2}] converting \"{3}\", configuration \"{4}\": " +
                            "failed to convert custom build command",
                            projPath, lineNumber, itemType, row.itemName, configId), 5000);

                        item.Remove();
                        error = true;
                    }
                }
            }

            return !error;
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
                var filterCustomBuild = this[Files.Filters]?.xml
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
            using (var evaluator = new MSBuildEvaluator(this[Files.Project])) {
                if (!QtMsBuildContainer.QtMocInstance.ParseCommandLine(
                    commandLine, evaluator, out properties)) {
                    return (string)cbt.Attribute("Include");
                }
            }
            string ouputFile;
            if (!properties.TryGetValue(QtMoc.Property.InputFile, out ouputFile))
                return (string)cbt.Attribute("Include");
            return ouputFile;
        }

        bool RemoveGeneratedFiles(
            string projDir,
            List<CustomBuildEval> cbEvals,
            string configName,
            string itemName,
            Dictionary<string, List<XElement>> projItemsByPath,
            Dictionary<string, List<XElement>> filterItemsByPath)
        {
            //remove items with generated files
            bool hasGeneratedFiles = false;
            var cbEval = cbEvals
                .Where(x => x.ProjectConfig == configName && x.Identity == itemName)
                .FirstOrDefault();
            if (cbEval != null) {
                var outputFiles = cbEval.Outputs
                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => HelperFunctions.CanonicalPath(
                        Path.IsPathRooted(x) ? x : Path.Combine(projDir, x)));
                var outputItems = new List<XElement>();
                foreach (var outputFile in outputFiles) {
                    List<XElement> mocOutput = null;
                    if (projItemsByPath.TryGetValue(outputFile, out mocOutput)) {
                        outputItems.AddRange(mocOutput);
                        hasGeneratedFiles |= hasGeneratedFiles ? true : mocOutput
                            .Where(x => !x.Elements(ns + "ExcludedFromBuild")
                                .Where(y =>
                                    (string)y.Attribute("Condition") == string.Format(
                                        "'$(Configuration)|$(Platform)'=='{0}'", configName)
                                    && y.Value == "true")
                                .Any())
                            .Any();
                    }
                    if (filterItemsByPath.TryGetValue(outputFile, out mocOutput))
                        outputItems.AddRange(mocOutput);
                }
                foreach (var item in outputItems.Where(x => x.Parent != null))
                    item.Remove();
            }
            return hasGeneratedFiles;
        }

        public bool ConvertCustomBuildToQtMsBuild()
        {
            var cbEvals = EvaluateCustomBuild();

            var qtMsBuild = new QtMsBuildContainer(new MsBuildConverterProvider());
            qtMsBuild.BeginSetItemProperties();

            var projDir = Path.GetDirectoryName(this[Files.Project].filePath);

            var configurations = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "ProjectConfiguration");

            var projItemsByPath = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements()
                .Where(x => ((string)x.Attribute("Include"))
                    .IndexOfAny(Path.GetInvalidPathChars()) == -1)
                .GroupBy(x => HelperFunctions.CanonicalPath(
                    Path.Combine(projDir, (string)x.Attribute("Include"))),
                    StringComparer.InvariantCultureIgnoreCase)
                .ToDictionary(x => x.Key, x => new List<XElement>(x));

            var filterItemsByPath = (this[Files.Filters].xml != null)
                ? this[Files.Filters].xml
                    .Elements(ns + "Project")
                    .Elements(ns + "ItemGroup")
                    .Elements()
                    .Where(x => ((string)x.Attribute("Include"))
                        .IndexOfAny(Path.GetInvalidPathChars()) == -1)
                    .GroupBy(x => HelperFunctions.CanonicalPath(
                        Path.Combine(projDir, (string)x.Attribute("Include"))),
                        StringComparer.InvariantCultureIgnoreCase)
                    .ToDictionary(x => x.Key, x => new List<XElement>(x))
                : new Dictionary<string, List<XElement>>();

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
                ((string)x.Attribute("Include")).EndsWith(".cbt",
                StringComparison.InvariantCultureIgnoreCase)
                || ((string)x.Attribute("Include")).EndsWith(".moc",
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
                cppMocItems = this[Files.Filters]?.xml
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
            if (!SetCommandLines(qtMsBuild, configurations, mocCustomBuilds,
                QtMoc.ToolExecName, QtMoc.ItemTypeName,
                Path.GetDirectoryName(this[Files.Project].filePath),
                new ItemCommandLineReplacement[]
                {
                    (item, cmdLine) => cmdLine.Replace(
                        string.Format(@"\moc_{0}.cpp", Path.GetFileNameWithoutExtension(item)),
                        @"\moc_%(Filename).cpp", StringComparison.InvariantCultureIgnoreCase)
                    .Replace(
                        string.Format(" -o moc_{0}.cpp", Path.GetFileNameWithoutExtension(item)),
                        @" -o $(ProjectDir)\moc_%(Filename).cpp",
                            StringComparison.InvariantCultureIgnoreCase),

                    (item, cmdLine) => cmdLine.Replace(
                        string.Format(@"\{0}.moc", Path.GetFileNameWithoutExtension(item)),
                        @"\%(Filename).moc", StringComparison.InvariantCultureIgnoreCase)
                    .Replace(
                        string.Format(" -o {0}.moc", Path.GetFileNameWithoutExtension(item)),
                        @" -o $(ProjectDir)\%(Filename).moc",
                            StringComparison.InvariantCultureIgnoreCase),
                })) {
                Rollback();
                return false;
            }
            List<XElement> mocDisableDynamicSource = new List<XElement>();
            foreach (var qtMoc in mocCustomBuilds.Elements(ns + QtMoc.ItemTypeName)) {
                var itemName = (string)qtMoc.Attribute("Include");
                var configName = (string)qtMoc.Attribute("ConfigName");

                //remove items with generated files
                var hasGeneratedFiles = RemoveGeneratedFiles(
                    projDir, cbEvals, configName, itemName,
                    projItemsByPath, filterItemsByPath);

                //set properties
                qtMsBuild.SetItemProperty(qtMoc,
                    QtMoc.Property.ExecutionDescription, "Moc'ing %(Identity)...");
                qtMsBuild.SetItemProperty(qtMoc,
                    QtMoc.Property.InputFile, "%(FullPath)");
                if (!HelperFunctions.IsSourceFile(itemName)) {
                    qtMsBuild.SetItemProperty(qtMoc,
                        QtMoc.Property.DynamicSource, "output");
                    if (!hasGeneratedFiles)
                        mocDisableDynamicSource.Add(qtMoc);
                } else {
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
            if (!SetCommandLines(qtMsBuild, configurations, rccCustomBuilds,
                QtRcc.ToolExecName, QtRcc.ItemTypeName,
                Path.GetDirectoryName(this[Files.Project].filePath),
                new ItemCommandLineReplacement[]
                {
                    (item, cmdLine) => cmdLine.Replace(
                        string.Format(@"\qrc_{0}.cpp", Path.GetFileNameWithoutExtension(item)),
                        @"\qrc_%(Filename).cpp", StringComparison.InvariantCultureIgnoreCase)
                    .Replace(
                        string.Format(" -o qrc_{0}.cpp", Path.GetFileNameWithoutExtension(item)),
                        @" -o $(ProjectDir)\qrc_%(Filename).cpp",
                            StringComparison.InvariantCultureIgnoreCase),
                })) {
                Rollback();
                return false;
            }
            foreach (var qtRcc in rccCustomBuilds.Elements(ns + QtRcc.ItemTypeName)) {
                var itemName = (string)qtRcc.Attribute("Include");
                var configName = (string)qtRcc.Attribute("ConfigName");

                //remove items with generated files
                RemoveGeneratedFiles(projDir, cbEvals, configName, itemName,
                    projItemsByPath, filterItemsByPath);

                //set properties
                qtMsBuild.SetItemProperty(qtRcc,
                    QtRcc.Property.ExecutionDescription, "Rcc'ing %(Identity)...");
                qtMsBuild.SetItemProperty(qtRcc,
                    QtRcc.Property.InputFile, "%(FullPath)");
            }

            //convert repc custom build steps
            var repcCustomBuilds = GetCustomBuilds(QtRepc.ToolExecName);
            if (!SetCommandLines(qtMsBuild, configurations, repcCustomBuilds,
                QtRepc.ToolExecName, QtRepc.ItemTypeName,
                Path.GetDirectoryName(this[Files.Project].filePath),
                new ItemCommandLineReplacement[] { })) {
                Rollback();
                return false;
            }
            foreach (var qtRepc in repcCustomBuilds.Elements(ns + QtRepc.ItemTypeName)) {
                var itemName = (string)qtRepc.Attribute("Include");
                var configName = (string)qtRepc.Attribute("ConfigName");

                //remove items with generated files
                RemoveGeneratedFiles(projDir, cbEvals, configName, itemName,
                    projItemsByPath, filterItemsByPath);

                //set properties
                qtMsBuild.SetItemProperty(qtRepc,
                    QtRepc.Property.ExecutionDescription, "Repc'ing %(Identity)...");
                qtMsBuild.SetItemProperty(qtRepc,
                    QtRepc.Property.InputFile, "%(FullPath)");
            }


            //convert uic custom build steps
            var uicCustomBuilds = GetCustomBuilds(QtUic.ToolExecName);
            if (!SetCommandLines(qtMsBuild, configurations, uicCustomBuilds,
                QtUic.ToolExecName, QtUic.ItemTypeName,
                Path.GetDirectoryName(this[Files.Project].filePath),
                new ItemCommandLineReplacement[]
                {
                    (item, cmdLine) => cmdLine.Replace(
                        string.Format(@"\ui_{0}.h", Path.GetFileNameWithoutExtension(item)),
                        @"\ui_%(Filename).h", StringComparison.InvariantCultureIgnoreCase)
                    .Replace(
                        string.Format(" -o ui_{0}.h", Path.GetFileNameWithoutExtension(item)),
                        @" -o $(ProjectDir)\ui_%(Filename).h",
                            StringComparison.InvariantCultureIgnoreCase),
                })) {
                Rollback();
                return false;
            }
            foreach (var qtUic in uicCustomBuilds.Elements(ns + QtUic.ItemTypeName)) {
                var itemName = (string)qtUic.Attribute("Include");
                var configName = (string)qtUic.Attribute("ConfigName");

                //remove items with generated files
                RemoveGeneratedFiles(projDir, cbEvals, configName, itemName,
                    projItemsByPath, filterItemsByPath);

                //set properties
                qtMsBuild.SetItemProperty(qtUic,
                    QtUic.Property.ExecutionDescription, "Uic'ing %(Identity)...");
                qtMsBuild.SetItemProperty(qtUic,
                    QtUic.Property.InputFile, "%(FullPath)");
            }

            qtMsBuild.EndSetItemProperties();

            //disable dynamic C++ source for moc headers without generated files
            //(needed for the case of #include "moc_foo.cpp" in source file)
            foreach (var qtMoc in mocDisableDynamicSource) {
                qtMsBuild.SetItemProperty(qtMoc,
                    QtMoc.Property.DynamicSource, "false");
            }

            FinalizeProjectChanges(mocCustomBuilds.ToList(), QtMoc.ItemTypeName);
            FinalizeProjectChanges(rccCustomBuilds.ToList(), QtRcc.ItemTypeName);
            FinalizeProjectChanges(repcCustomBuilds.ToList(), QtRepc.ItemTypeName);
            FinalizeProjectChanges(uicCustomBuilds.ToList(), QtUic.ItemTypeName);

            this[Files.Project].isDirty = this[Files.Filters].isDirty = true;
            Commit();
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
            this[Files.Project].isDirty = true;
            Commit();
        }

        class MSBuildEvaluator : IVSMacroExpander, IDisposable
        {
            MsBuildXmlFile projFile;
            string tempProjFilePath;
            XElement evaluateTarget;
            XElement evaluateProperty;
            ProjectRootElement projRoot;
            public Dictionary<string, string> expansionCache;

            public Dictionary<string, string> Properties
            {
                get;
                private set;
            }

            public MSBuildEvaluator(MsBuildXmlFile projFile)
            {
                this.projFile = projFile;
                tempProjFilePath = string.Empty;
                evaluateTarget = evaluateProperty = null;
                expansionCache = new Dictionary<string, string>();
                Properties = new Dictionary<string, string>();
            }

            public void Dispose()
            {
                if (evaluateTarget != null) {
                    evaluateTarget.Remove();
                    if (File.Exists(tempProjFilePath))
                        File.Delete(tempProjFilePath);
                }
            }

            string ExpansionCacheKey(string stringToExpand)
            {
                var key = new StringBuilder();
                foreach (var property in Properties)
                    key.AppendFormat("{0};{1};", property.Key, property.Value);
                key.Append(stringToExpand);
                return key.ToString();
            }

            bool TryExpansionCache(string stringToExpand, out string expandedString)
            {
                return expansionCache.TryGetValue(
                    ExpansionCacheKey(stringToExpand), out expandedString);
            }

            void AddToExpansionCache(string stringToExpand, string expandedString)
            {
                expansionCache[ExpansionCacheKey(stringToExpand)] = expandedString;
            }

            public string ExpandString(string stringToExpand)
            {
                string expandedString;
                if (TryExpansionCache(stringToExpand, out expandedString))
                    return expandedString;

                if (evaluateTarget == null) {
                    projFile.xmlCommitted.Root.Add(evaluateTarget = new XElement(ns + "Target",
                        new XAttribute("Name", "MSBuildEvaluatorTarget"),
                        new XElement(ns + "PropertyGroup",
                            evaluateProperty = new XElement(ns + "MSBuildEvaluatorProperty"))));
                }
                if (stringToExpand != (string)evaluateProperty) {
                    evaluateProperty.SetValue(stringToExpand);
                    if (!string.IsNullOrEmpty(tempProjFilePath) && File.Exists(tempProjFilePath))
                        File.Delete(tempProjFilePath);
                    tempProjFilePath = Path.Combine(
                        Path.GetDirectoryName(projFile.filePath),
                        Path.GetRandomFileName());
                    if (File.Exists(tempProjFilePath))
                        File.Delete(tempProjFilePath);
                    projFile.xmlCommitted.Save(tempProjFilePath);
                    projRoot = ProjectRootElement.Open(tempProjFilePath);
                }
                var projInst = new ProjectInstance(projRoot, Properties,
                    null, new ProjectCollection());
                var buildRequest = new BuildRequestData(
                    projInst, new string[] { "MSBuildEvaluatorTarget" },
                    null, BuildRequestDataFlags.ProvideProjectStateAfterBuild);
                var buildResult = BuildManager.DefaultBuildManager.Build(
                    new BuildParameters(), buildRequest);
                expandedString = buildResult.ProjectStateAfterBuild
                    .GetPropertyValue("MSBuildEvaluatorProperty");

                AddToExpansionCache(stringToExpand, expandedString);
                return expandedString;
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

            var pattern = new Regex(@"{([^}]+)}{([^}]+)}{([^}]+)}{([^}]+)}{([^}]+)}");

            var projConfigs = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "ProjectConfiguration");

            using (var evaluator = new MSBuildEvaluator(this[Files.Project])) {

                foreach (var projConfig in projConfigs) {

                    evaluator.Properties.Clear();
                    foreach (var configProp in projConfig.Elements())
                        evaluator.Properties.Add(configProp.Name.LocalName, (string)configProp);

                    var expandedValue = evaluator.ExpandString(
                        "@(CustomBuild->'" +
                            "{%(Identity)}" +
                            "{%(AdditionalInputs)}" +
                            "{%(Outputs)}" +
                            "{%(Message)}" +
                            "{%(Command)}')");

                    foreach (Match cbEval in pattern.Matches(expandedValue)) {
                        eval.Add(new CustomBuildEval
                        {
                            ProjectConfig = (string)projConfig.Attribute("Include"),
                            Identity = cbEval.Groups[1].Value,
                            AdditionalInputs = cbEval.Groups[2].Value,
                            Outputs = cbEval.Groups[3].Value,
                            Message = cbEval.Groups[4].Value,
                            Command = cbEval.Groups[5].Value,
                        });
                    }
                }
            }

            return eval;
        }

        public bool BuildTarget(string target)
        {
            if (this[Files.Project].isDirty)
                return false;

            var configurations = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "ProjectConfiguration");

            using (var buildManager = new BuildManager()) {

                foreach (var config in configurations) {

                    var configProps = config.Elements()
                        .ToDictionary(x => x.Name.LocalName, x => x.Value);

                    var projectInstance = new ProjectInstance(this[Files.Project].filePath,
                        new Dictionary<string, string>(configProps)
                        { { "DesignTimeBuild", "true" } },
                        null, new ProjectCollection());

                    var buildRequest = new BuildRequestData(projectInstance,
                        targetsToBuild: new[] { target },
                        hostServices: null,
                        flags: BuildRequestDataFlags.ProvideProjectStateAfterBuild);

                    var result = buildManager.Build(new BuildParameters(), buildRequest);
                    if (result.OverallResult != BuildResultCode.Success)
                        return false;

                }
            }
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
