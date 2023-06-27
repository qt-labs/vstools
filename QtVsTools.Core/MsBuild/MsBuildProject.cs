/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Win32;

namespace QtVsTools.Core.MsBuild
{
    using SyntaxAnalysis;
    using static HelperFunctions;
    using static SyntaxAnalysis.RegExpr;
    using static Utils;

    public class MsBuildProject
    {
        private class MsBuildXmlFile
        {
            public string filePath = "";
            public XDocument xml;
            public bool isDirty;
            public XDocument xmlCommitted;
            public bool isCommittedDirty;
        }

        private enum Files
        {
            Project = 0,
            Filters,
            User,
            Count
        }

        private readonly MsBuildXmlFile[] files = new MsBuildXmlFile[(int)Files.Count];

        private MsBuildProject()
        {
            for (int i = 0; i < files.Length; i++)
                files[i] = new MsBuildXmlFile();
        }

        private MsBuildXmlFile this[Files file]
        {
            get => (int)file >= (int)Files.Count ? files[0] : files[(int)file];
        }

        public string ProjectXml
        {
            get
            {
                var xml = this[Files.Project].xml;
                return xml?.ToString(SaveOptions.None) ?? "";
            }
        }

        private static readonly XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";

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

        private static bool LoadXml(MsBuildXmlFile xmlFile)
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
                if (!file.isDirty)
                    continue;
                file.xml?.Save(file.filePath, SaveOptions.None);
                file.isCommittedDirty = file.isDirty = false;
            }
            return true;
        }

        private void Commit()
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

        private void Rollback()
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
                .FirstOrDefault(x => x.Name.LocalName == property_name);
            return xProperty?.Value ?? "";
        }

        public string GetProperty(string item_type, string property_name)
        {
            var xProperty = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Elements(ns + item_type)
                .Elements()
                .FirstOrDefault(x => x.Name.LocalName == property_name);
            return xProperty?.Value ?? "";
        }

        public IEnumerable<string> GetItems(string item_type)
        {
            return this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + item_type)
                .Select(x => (string)x.Attribute("Include"));
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
        private Parser _ConfigCondition;

        private Parser ConfigCondition
        {
            get
            {
                if (_ConfigCondition != null)
                    return _ConfigCondition;
                var config = new Token("Configuration", CharWord.Repeat());
                var platform = new Token("Platform", CharWord.Repeat());
                var expr = "'$(Configuration)|$(Platform)'=='" & config & "|" & platform & "'";
                try {
                    _ConfigCondition = expr.Render();
                } catch { }
                return _ConfigCondition;
            }
        }

        /// <summary>
        /// Parser for project format version string:
        ///
        ///     QtVS_vNNN
        ///
        /// </summary>
        private Parser _ProjectFormatVersion;

        private Parser ProjectFormatVersion
        {
            get
            {
                if (_ProjectFormatVersion != null)
                    return _ProjectFormatVersion;
                var expr = "QtVS_v" & new Token("VERSION", Char['0', '9'].Repeat(3))
                {
                    new Rule<int> { Capture(int.Parse) }
                };
                try {
                    _ProjectFormatVersion = expr.Render();
                } catch { }
                return _ProjectFormatVersion;
            }
        }

        private ProjectFormat.Version ParseProjectFormatVersion(string text)
        {
            if (string.IsNullOrEmpty(text) || ProjectFormatVersion == null)
                return ProjectFormat.Version.Unknown;
            try {
                return (ProjectFormat.Version) ProjectFormatVersion.Parse(text)
                    .GetValues<int>("VERSION")
                    .First();
            } catch {
                return text.StartsWith(ProjectFormat.KeywordV2, StringComparison.Ordinal)
                    ? ProjectFormat.Version.V1
                    : ProjectFormat.Version.Unknown;
            }
        }

        public ProjectFormat.Version GetProjectFormatVersion()
        {
            var globals = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .FirstOrDefault(x => (string)x.Attribute("Label") == "Globals");
            // Set Qt project format version
            var projKeyword = globals?.Elements(ns + "Keyword")
                .FirstOrDefault(x => x.Value.StartsWith(ProjectFormat.KeywordLatest)
                    || x.Value.StartsWith(ProjectFormat.KeywordV2));
            return ParseProjectFormatVersion(projKeyword?.Value);
        }

        /// <summary>
        /// Converts project format version to the latest version:
        ///  * Set latest project version;
        ///  * Add QtSettings property group;
        ///  * Set QtInstall property;
        ///  * Remove hard-coded macros, include paths and libs related to Qt modules.
        ///  * Set QtModules property;
        /// </summary>
        /// <param name="oldVersion"></param>
        /// <returns>true if successful</returns>
        public bool UpdateProjectFormatVersion(ProjectFormat.Version oldVersion)
        {
            if (ConfigCondition == null)
                return false;

            switch (oldVersion) {
            case ProjectFormat.Version.Latest:
                return true; // Nothing to do!
            case ProjectFormat.Version.Unknown or > ProjectFormat.Version.Latest:
                return false; // Nothing we can do!
            }

            var defaultVersionName = QtVersionManager.The().GetDefaultVersion();
            var defaultVersion = QtVersionManager.The().GetVersionInfo(defaultVersionName);

            // Get project configurations
            var configs = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "ProjectConfiguration")
                .ToList();

            // Get project global properties
            var globals = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .FirstOrDefault(x => (string)x.Attribute("Label") == "Globals");

            // Set Qt project format version
            var projKeyword = globals?.Elements(ns + "Keyword")
                .FirstOrDefault(x => x.Value.StartsWith(ProjectFormat.KeywordLatest)
                    || x.Value.StartsWith(ProjectFormat.KeywordV2));
            if (projKeyword == null)
                return false;

            projKeyword.SetValue($"QtVS_v{(int)ProjectFormat.Version.Latest}");

            // Find import of qt.props
            var qtPropsImport = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ImportGroup")
                .Elements(ns + "Import")
                .FirstOrDefault(x => (string)x.Attribute("Project") == @"$(QtMsBuild)\qt.props");
            if (qtPropsImport == null)
                return false;

            var uncategorizedPropertyGroups = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Where(pg => pg.Attribute("Label") == null)
                .ToList();

            var propertyGroups = new Dictionary<string, XElement>();

            // Upgrading from <= v3.2?
            if (oldVersion < ProjectFormat.Version.V3PropertyEval) {
                // Find import of default Qt properties
                var qtDefaultProps = this[Files.Project].xml
                    .Elements(ns + "Project")
                    .Elements(ns + "ImportGroup")
                    .Elements(ns + "Import")
                    .Where(pg => Path.GetFileName((string)pg.Attribute("Project"))
                        .Equals("qt_defaults.props", IgnoreCase))
                    .Select(pg => pg.Parent)
                    .FirstOrDefault();

                // Create uncategorized property groups
                foreach (var config in configs) {
                    string condition =
                        $"'$(Configuration)|$(Platform)'=='{(string)config.Attribute("Include")}'";
                    var group = new XElement(ns + "PropertyGroup",
                                    new XAttribute("Condition", condition));
                    propertyGroups[condition] = group;
                    // Insert uncategorized groups after Qt defaults, if found
                    qtDefaultProps?.AddAfterSelf(group);
                }

                // Move uncategorized properties to newly created groups
                foreach (var pg in uncategorizedPropertyGroups) {
                    foreach (var p in pg.Elements().ToList()) {
                        var condition = p.Attribute("Condition") ?? pg.Attribute("Condition");
                        if (condition == null || !propertyGroups
                            .TryGetValue((string)condition, out var configPropertyGroup))
                            continue;
                        p.Remove();
                        p.SetAttributeValue("Condition", null);
                        configPropertyGroup.Add(p);
                    }
                    if (!pg.Elements().Any())
                        pg.Remove();
                }
            }

            // Upgrading from <= v3.1?
            if (oldVersion < ProjectFormat.Version.V3GlobalQtMsBuildProperty) {
                // Move Qt/MSBuild path to global property
                var qtMsBuildProperty = globals
                    .ElementsAfterSelf(ns + "PropertyGroup")
                    .Elements(ns + "QtMsBuild")
                    .FirstOrDefault();
                if (qtMsBuildProperty != null) {
                    var qtMsBuildPropertyGroup = qtMsBuildProperty.Parent;
                    qtMsBuildProperty.Remove();
                    qtMsBuildProperty.SetAttributeValue("Condition",
                        (string)qtMsBuildPropertyGroup.Attribute("Condition"));
                    globals.Add(qtMsBuildProperty);
                    qtMsBuildPropertyGroup.Remove();
                }
            }
            if (oldVersion > ProjectFormat.Version.V3) {
                this[Files.Project].isDirty = true;
                Commit();
                return true;
            }

            // Upgrading from v3.0?
            Dictionary<string, XElement> oldQtInstall = null;
            Dictionary<string, XElement> oldQtSettings = null;
            if (oldVersion is ProjectFormat.Version.V3) {
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

            // Find location for import of qt.props and for the QtSettings property group:
            // (cf. ".vcxproj file elements" https://docs.microsoft.com/en-us/cpp/build/reference/vcxproj-file-structure?view=vs-2019#vcxproj-file-elements)
            XElement insertionPoint = null;

            // * After the last UserMacros property group
            insertionPoint = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .LastOrDefault(x => (string)x.Attribute("Label") == "UserMacros");

            // * After the last PropertySheets import group
            insertionPoint ??= this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ImportGroup")
                .LastOrDefault(x => (string)x.Attribute("Label") == "PropertySheets");

            // * Before the first ItemDefinitionGroup
            insertionPoint ??= this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Select(x => x.ElementsBeforeSelf().Last())
                .FirstOrDefault();

            // * Before the first ItemGroup
            insertionPoint ??= this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Select(x => x.ElementsBeforeSelf().Last())
                .FirstOrDefault();

            // * Before the import of Microsoft.Cpp.targets
            insertionPoint ??= this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "Import")
                .Where(x =>
                    (string)x.Attribute("Project") == @"$(VCTargetsPath)\Microsoft.Cpp.targets")
                .Select(x => x.ElementsBeforeSelf().Last())
                .FirstOrDefault();

            // * At the end of the file
            insertionPoint ??= this[Files.Project].xml
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
                        $"'$(Configuration)|$(Platform)'=='{(string)config.Attribute("Include")}'"));
                insertionPoint.AddAfterSelf(configQtSettings);
                qtSettings.Add(configQtSettings);
            }

            // Add uncategorized property groups
            foreach (XElement propertyGroup in propertyGroups.Values)
                insertionPoint.AddAfterSelf(propertyGroup);

            // Add import of default property values
            insertionPoint.AddAfterSelf(
                new XElement(ns + "ImportGroup",
                    new XAttribute("Condition", @"Exists('$(QtMsBuild)\qt_defaults.props')"),
                    new XElement(ns + "Import",
                        new XAttribute("Project", @"$(QtMsBuild)\qt_defaults.props"))));

            //// Upgrading from v3.0: move Qt settings to newly created import groups
            if (oldVersion is ProjectFormat.Version.V3) {
                foreach (var configQtSettings in qtSettings) {
                    var configCondition = (string)configQtSettings.Attribute("Condition");

                    if (oldQtInstall.TryGetValue(configCondition, out var oldConfigQtInstall))
                        configQtSettings.Add(oldConfigQtInstall);
                    if (!oldQtSettings.TryGetValue(configCondition, out var oldConfigQtSettings))
                        continue;

                    foreach (var qtSetting in oldConfigQtSettings.Elements())
                        configQtSettings.Add(qtSetting);
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
                .Where(x => (string)x.Attribute("Label") == Resources.projLabelQtSettings)
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
                            var qtInstallName = $"Qt5Version_x0020_{platform}";
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
                .Elements(ns + "ClCompile")
                .ToList();

            // Get linker properties
            var linker = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Elements(ns + "Link")
                .ToList();

            var resourceCompiler = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Elements(ns + "ResourceCompile")
                .ToList();

            // Qt module names, to copy to QtModules property
            var moduleNames = new HashSet<string>();

            // Qt module macros, to remove from compiler macros property
            var moduleDefines = new HashSet<string>();

            // Qt module includes, to remove from compiler include directories property
            var moduleIncludePaths = new HashSet<string>();

            // Qt module link libraries, to remove from liker dependencies property
            var moduleLibs = new HashSet<string>();

            var qt5Modules = QtModules.Instance.GetAvailableModules(5);
            var qt6Modules = QtModules.Instance.GetAvailableModules(6);
            var modules = new ReadOnlyCollectionBuilder<QtModule>(qt5Modules.Concat(qt6Modules));

            // Go through all known Qt modules and check which ones are currently being used
            foreach (var module in modules.ToReadOnlyCollection()) {
                if (!IsModuleUsed(module, compiler, linker, resourceCompiler))
                    continue;
                // Qt module names, to copy to QtModules property
                if (!string.IsNullOrEmpty(module.proVarQT))
                    moduleNames.UnionWith(module.proVarQT.Split(' '));

                // Qt module macros, to remove from compiler macros property
                moduleDefines.UnionWith(module.Defines);

                // Qt module includes, to remove from compiler include directories property
                moduleIncludePaths.UnionWith(
                    module.IncludePath.Select(Path.GetFileName));

                // Qt module link libraries, to remove from liker dependencies property
                moduleLibs.UnionWith(
                    module.AdditionalLibraries.Select(Path.GetFileName));
                moduleLibs.UnionWith(
                    module.AdditionalLibrariesDebug.Select(Path.GetFileName));
                moduleLibs.Add(module.LibRelease);
                moduleLibs.Add(module.LibDebug);

                if (IsPrivateIncludePathUsed(module, compiler)) {
                    // Qt private module names, to copy to QtModules property
                    moduleNames.UnionWith(module.proVarQT.Split(' ')
                        .Select(x => $"{x}-private"));
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
                    .Select(Unquote)
                    // Exclude paths rooted on $(QTDIR)
                    .Where(x => !x.StartsWith("$(QTDIR)", IgnoreCase))));
            }

            // Remove Qt module libraries from linker properties
            foreach (var libs in linker.Elements(ns + "AdditionalDependencies")) {
                libs.SetValue(string.Join(";", libs.Value.Split(';')
                    .Where(x => !moduleLibs.Contains(Path.GetFileName(Unquote(x)), CaseIgnorer))));
            }

            // Remove Qt lib path from linker properties
            foreach (var libs in linker.Elements(ns + "AdditionalLibraryDirectories")) {
                libs.SetValue(string.Join(";", libs.Value.Split(';')
                    .Select(Unquote)
                    // Exclude paths rooted on $(QTDIR)
                    .Where(x => !x.StartsWith("$(QTDIR)", IgnoreCase))));
            }

            // Remove Qt module macros from resource compiler properties
            foreach (var defines in resourceCompiler.Elements(ns + "PreprocessorDefinitions")) {
                defines.SetValue(string.Join(";", defines.Value.Split(';')
                    .Where(x => !moduleDefines.Contains(x))));
            }

            // Add Qt module names to QtModules project property
            this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Where(x => (string)x.Attribute("Label") == Resources.projLabelQtSettings)
                .ToList()
                .ForEach(x => x.Add(new XElement(ns + "QtModules", string.Join(";", moduleNames))));

            // Remove project user properties (old format)
            userProps?.Attributes().ToList().ForEach(userProp =>
            {
                if (userProp.Name.LocalName.StartsWith("Qt5Version_x0020_")
                    || userProp.Name.LocalName is "lupdateOptions" or "lupdateOnBuild"
                        or "lreleaseOptions" or "MocDir" or "MocOptions" or "RccDir"
                        or "UicDir") {
                    userProp.Remove();
                }
            });

            // Remove old properties from .user file
            if (this[Files.User].xml != null) {
                this[Files.User].xml
                    .Elements(ns + "Project")
                    .Elements(ns + "PropertyGroup")
                    .Elements()
                    .ToList()
                    .ForEach(userProp =>
                    {
                        if (userProp.Name.LocalName is "QTDIR" or "QmlDebug" or "QmlDebugSettings"
                            || (userProp.Name.LocalName == "LocalDebuggerCommandArguments"
                                && (string)userProp == "$(QmlDebug)")
                            || (userProp.Name.LocalName == "LocalDebuggerEnvironment"
                                && (string)userProp == "PATH=$(QTDIR)\\bin%3b$(PATH)")) {
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
                if (outputFile == null)
                    continue;
                var qtTool = qtItem.Name.LocalName;
                var outDir = Path.GetDirectoryName(outputFile.Value);
                var outFileName = Path.GetFileName(outputFile.Value);
                qtItem.Add(new XElement(ns + qtTool + "Dir",
                    string.IsNullOrEmpty(outDir) ? "$(ProjectDir)" : outDir));
                qtItem.Add(new XElement(ns + qtTool + "FileName", outFileName));
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
                        switch (itemName) {
                        case "QtMoc" when oldPropsAny.Contains(propName):
                        case "QtRcc" when oldQtProps.Contains(propName):
                        case "QtUic" when oldQtProps.Contains(propName):
                        case "QtRepc" when oldPropsAny.Contains(propName):
                            itemProp.Remove();
                            break;
                        }
                    });
                });

            this[Files.Project].isDirty = true;
            Commit();
            return true;
        }

        private bool IsModuleUsed(
            QtModule module,
            IEnumerable<XElement> compiler,
            IEnumerable<XElement> linker,
            IEnumerable<XElement> resourceCompiler)
        {
            // Module .lib is present in linker additional dependencies
            if (linker.Elements(ns + "AdditionalDependencies")
                .SelectMany(x => x.Value.Split(';'))
                .Any(x => Path.GetFileName(Unquote(x)).Equals(module.LibRelease, IgnoreCase)
                    || Path.GetFileName(Unquote(x)).Equals(module.LibDebug, IgnoreCase))) {
                return true;
            }

            // Module macro is present in the compiler pre-processor definitions
            if (compiler.Elements(ns + "PreprocessorDefinitions")
                .SelectMany(x => x.Value.Split(';'))
                .Any(x => module.Defines.Contains(x))) {
                return true;
            }

            // true if Module macro is present in resource compiler pre-processor definitions
            return resourceCompiler.Elements(ns + "PreprocessorDefinitions")
                .SelectMany(x => x.Value.Split(';'))
                .Any(x => module.Defines.Contains(x));
        }

        private bool IsPrivateIncludePathUsed(
            QtModule module,
            IEnumerable<XElement> compiler)
        {
            var privateIncludePattern = new Regex(
                $@"^\$\(QTDIR\)[\\\/]include[\\\/]{module.LibraryPrefix}[\\\/]\d+\.\d+\.\d+");

            // true if Module private header path is present in compiler include dirs
            return compiler.Elements(ns + "AdditionalIncludeDirectories")
                .SelectMany(x => x.Value.Split(';'))
                .Any(x => privateIncludePattern.IsMatch(x));
        }

        public bool SetDefaultWindowsSDKVersion(string winSDKVersion)
        {
            var xGlobals = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .FirstOrDefault(x => (string)x.Attribute("Label") == "Globals");
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
            bool isQtMsBuildEnabled;
            isQtMsBuildEnabled = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ImportGroup")
                .Elements(ns + "Import")
                .Any(x => x.Attribute("Project").Value == @"$(QtMsBuild)\qt.props");
            if (isQtMsBuildEnabled)
                return true;

            var xImportCppProps = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "Import")
                .FirstOrDefault(x => x.Attribute("Project").Value == @"$(VCTargetsPath)\Microsoft.Cpp.props");
            if (xImportCppProps == null)
                return false;

            var xImportCppTargets = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "Import")
                .FirstOrDefault(x => x.Attribute("Project").Value == @"$(VCTargetsPath)\Microsoft.Cpp.targets");
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

        private delegate string ItemCommandLineReplacement(string itemName, string cmdLine);

        private bool SetCommandLines(
            QtMsBuildContainer qtMsBuild,
            IEnumerable<XElement> configurations,
            IEnumerable<XElement> customBuilds,
            string toolExec,
            string itemType,
            string workingDir,
            IList<ItemCommandLineReplacement> extraReplacements)
        {
            var query = from customBuild in customBuilds
                        let itemName = customBuild.Attribute("Include").Value
                        from config in configurations
                        from command in customBuild.Elements(ns + "Command")
                        where command.Attribute("Condition").Value
                            == $"'$(Configuration)|$(Platform)'=='{(string)config.Attribute("Include")}'"
                        select new { customBuild, itemName, config, command };

            var projPath = this[Files.Project].filePath;
            bool error = false;
            using (var evaluator = new MSBuildEvaluator(this[Files.Project])) {
                foreach (var row in query) {

                    var configId = (string)row.config.Attribute("Include");
                    if (!row.command.Value.Contains(toolExec)) {
                        Messages.Print($"{projPath}: warning: [{itemType}] converting "
                            + $"\"{row.itemName}\", configuration \"{configId}\": "
                            + $"tool not found: \"{toolExec}\"; applying default options");
                        continue;
                    }

                    XElement item;
                    row.customBuild.Add(item =
                        new XElement(ns + itemType,
                            new XAttribute("Include", row.itemName),
                            new XAttribute("ConfigName", configId)));
                    var configName = (string)row.config.Element(ns + "Configuration");
                    var platformName = (string)row.config.Element(ns + "Platform");

                    ///////////////////////////////////////////////////////////////////////////////
                    // Replace fixed values with VS macros
                    //
                    //   * Filename, e.g. foo.ui --> %(Filename)%(Extension)
                    var commandLine = row.command.Value.Replace(Path.GetFileName(row.itemName),
                        "%(Filename)%(Extension)", IgnoreCase);
                    //
                    //   * Context specific, e.g. ui_foo.h --> ui_%(FileName).h
                    foreach (var replace in extraReplacements)
                        commandLine = replace(row.itemName, commandLine);
                    //
                    //   * Configuration/platform, e.g. x64\Debug --> $(Platform)\$(Configuration)
                    //   * ignore any word other than the expected configuration, e.g. lrelease.exe
                    commandLine = Regex.Replace(commandLine, @"\b" + configName + @"\b",
                            "$(Configuration)", RegexOptions.IgnoreCase)
                        .Replace(platformName, "$(Platform)", IgnoreCase);

                    evaluator.Properties.Clear();
                    foreach (var configProp in row.config.Elements())
                        evaluator.Properties.Add(configProp.Name.LocalName, (string)configProp);
                    if (qtMsBuild.SetCommandLine(itemType, item, commandLine, evaluator))
                        continue;

                    var lineNumber = 1;
                    if (row.command is IXmlLineInfo errorLine && errorLine.HasLineInfo())
                        lineNumber = errorLine.LineNumber;

                    Messages.Print($"{projPath}({lineNumber}): error: [{itemType}] "
                      + $"converting \"{row.itemName}\", configuration \"{configId}\": "
                      + "failed to convert custom build command");

                    item.Remove();
                    error = true;
                }
            }

            return !error;
        }

        private List<XElement> GetCustomBuilds(string toolExecName)
        {
            return this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + "CustomBuild")
                .Where(x => x.Elements(ns + "Command")
                    .Any(y => y.Value.Contains(toolExecName)))
                .ToList();
        }

        private void FinalizeProjectChanges(List<XElement> customBuilds, string itemTypeName)
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
                        $"'$(Configuration)|$(Platform)'=='{configName}'");
                    prop.Remove();
                    item.Parent.Add(prop);
                });
                item.Remove();
            });

            customBuilds.ForEach(customBuild =>
            {
                var filterCustomBuild = (this[Files.Filters]?.xml
                        .Elements(ns + "Project")
                        .Elements(ns + "ItemGroup")
                        .Elements(ns + "CustomBuild"))
                    .FirstOrDefault(
                        filterItem => filterItem.Attribute("Include").Value
                         == customBuild.Attribute("Include").Value);
                if (filterCustomBuild != null) {
                    filterCustomBuild.Name = ns + itemTypeName;
                    this[Files.Filters].isDirty = true;
                }

                customBuild.Name = ns + itemTypeName;
                this[Files.Project].isDirty = true;
            });
        }

        private static string AddGeneratedFilesPath(string includePathList)
        {
            var includes = new HashSet<string> {
                GetDirectory("MocDir"),
                GetDirectory("UicDir"),
                GetDirectory("RccDir")
            };
            foreach (var includePath in includePathList.Split(';'))
                includes.Add(includePath);
            return string.Join<string>(";", includes);
        }

        private const string RegistryPath = "SOFTWARE\\" + Resources.registryPackagePath;
        private static string GetDirectory(string type)
        {
            try {
                if (Registry.CurrentUser.OpenSubKey(RegistryPath) is {} key) {
                    if (key.GetValue(type, null) is string path)
                        return NormalizeRelativeFilePath(path);
                }
            } catch (Exception exception) {
                exception.Log();
            }
            return type == "MocDir" ? "GeneratedFiles\\$(ConfigurationName)" : "GeneratedFiles";
        }

        private string CustomBuildMocInput(XElement cbt)
        {
            var commandLine = (string)cbt.Element(ns + "Command");
            Dictionary<QtMoc.Property, string> properties;
            using (var evaluator = new MSBuildEvaluator(this[Files.Project])) {
                if (!QtMsBuildContainer.QtMocInstance.ParseCommandLine(
                    commandLine, evaluator, out properties)) {
                    return (string)cbt.Attribute("Include");
                }
            }
            if (!properties.TryGetValue(QtMoc.Property.InputFile, out string ouputFile))
                return (string)cbt.Attribute("Include");
            return ouputFile;
        }

        private bool RemoveGeneratedFiles(
            string projDir,
            List<CustomBuildEval> cbEvals,
            string configName,
            string itemName,
            Dictionary<string, List<XElement>> projItemsByPath,
            Dictionary<string, List<XElement>> filterItemsByPath)
        {
            //remove items with generated files
            var cbEval = cbEvals
                .FirstOrDefault(x => x.ProjectConfig == configName && x.Identity == itemName);
            if (cbEval == null)
                return false;

            var hasGeneratedFiles = false;
            var outputFiles = cbEval.Outputs
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => CanonicalPath(
                    Path.IsPathRooted(x) ? x : Path.Combine(projDir, x)));
            var outputItems = new List<XElement>();
            foreach (var outputFile in outputFiles) {
                if (projItemsByPath.TryGetValue(outputFile, out var mocOutput)) {
                    outputItems.AddRange(mocOutput);
                    hasGeneratedFiles |= hasGeneratedFiles || mocOutput
                        .Any(x => !x.Elements(ns + "ExcludedFromBuild")
                            .Any(y => (string)y.Attribute("Condition") == $"'$(Configuration)|$(Platform)'=='{configName}'"
                             && y.Value == "true"));
                }
                if (filterItemsByPath.TryGetValue(outputFile, out mocOutput))
                    outputItems.AddRange(mocOutput);
            }
            foreach (var item in outputItems.Where(x => x.Parent != null))
                item.Remove();
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
                .Elements(ns + "ProjectConfiguration")
                .ToList();

            var projItemsByPath = this[Files.Project].xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements()
                .Where(x => ((string)x.Attribute("Include"))
                    .IndexOfAny(Path.GetInvalidPathChars()) == -1)
                .GroupBy(x => CanonicalPath(
                    Path.Combine(projDir, (string)x.Attribute("Include"))), CaseIgnorer)
                .ToDictionary(x => x.Key, x => new List<XElement>(x));

            var filterItemsByPath = this[Files.Filters].xml != null
                ? this[Files.Filters].xml
                    .Elements(ns + "Project")
                    .Elements(ns + "ItemGroup")
                    .Elements()
                    .Where(x => ((string)x.Attribute("Include"))
                        .IndexOfAny(Path.GetInvalidPathChars()) == -1)
                    .GroupBy(x => HelperFunctions.CanonicalPath(
                        Path.Combine(projDir, (string)x.Attribute("Include"))), CaseIgnorer)
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
                ((string)x.Attribute("Include")).EndsWith(".cbt", IgnoreCase)
                || ((string)x.Attribute("Include")).EndsWith(".moc", IgnoreCase))
                .GroupBy(CustomBuildMocInput);

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
                    "Outputs"
                };
                foreach (var cbt in cbtGroup) {
                    var enabledProperties = cbt.Elements().Where(x =>
                        cbtPropertyNames.Contains(x.Name.LocalName)
                        && x.Parent.Elements(ns + "ExcludedFromBuild")
                            .All(y => (string)x.Attribute("Condition") != (string)y.Attribute("Condition")));
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
                        cbtGroup.Key.Equals((string)x.Attribute("Include"), IgnoreCase));
                foreach (var cppMocItem in cppMocItems)
                    cppMocItem.Remove();

                //change type of item in filter
                cppMocItems = this[Files.Filters]?.xml
                    .Elements(ns + "Project")
                    .Elements(ns + "ItemGroup")
                    .Elements(ns + "ClCompile")
                    .Where(x =>
                        cbtGroup.Key.Equals((string)x.Attribute("Include"), IgnoreCase));
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
                            $@"\moc_{Path.GetFileNameWithoutExtension(item)}.cpp",
                        @"\moc_%(Filename).cpp", IgnoreCase)
                    .Replace($" -o moc_{Path.GetFileNameWithoutExtension(item)}.cpp",
                        @" -o $(ProjectDir)\moc_%(Filename).cpp", IgnoreCase),

                    (item, cmdLine) => cmdLine.Replace(
                            $@"\{Path.GetFileNameWithoutExtension(item)}.moc",
                        @"\%(Filename).moc", IgnoreCase)
                    .Replace($" -o {Path.GetFileNameWithoutExtension(item)}.moc",
                        @" -o $(ProjectDir)\%(Filename).moc", IgnoreCase)
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
                if (!IsSourceFile(itemName)) {
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
                        $@"\qrc_{Path.GetFileNameWithoutExtension(item)}.cpp",
                        @"\qrc_%(Filename).cpp", IgnoreCase)
                    .Replace(
                        $" -o qrc_{Path.GetFileNameWithoutExtension(item)}.cpp",
                        @" -o $(ProjectDir)\qrc_%(Filename).cpp", IgnoreCase)
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
                        $@"\ui_{Path.GetFileNameWithoutExtension(item)}.h",
                        @"\ui_%(Filename).h", IgnoreCase)
                    .Replace(
                        $" -o ui_{Path.GetFileNameWithoutExtension(item)}.h",
                        @" -o $(ProjectDir)\ui_%(Filename).h", IgnoreCase)
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

            FinalizeProjectChanges(mocCustomBuilds, QtMoc.ItemTypeName);
            FinalizeProjectChanges(rccCustomBuilds, QtRcc.ItemTypeName);
            FinalizeProjectChanges(repcCustomBuilds, QtRepc.ItemTypeName);
            FinalizeProjectChanges(uicCustomBuilds, QtUic.ItemTypeName);

            this[Files.Project].isDirty = this[Files.Filters].isDirty = true;
            Commit();
            return true;
        }

        private bool TryReplaceTextInPlace(ref string text, Regex findWhat, string newText)
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

        private void ReplaceText(XElement xElem, Regex findWhat, string newText)
        {
            var elemValue = (string)xElem;
            if (!string.IsNullOrEmpty(elemValue)
                && TryReplaceTextInPlace(ref elemValue, findWhat, newText)) {
                xElem.Value = elemValue;
            }
        }

        private void ReplaceText(XAttribute xAttr, Regex findWhat, string newText)
        {
            var attrValue = (string)xAttr;
            if (!string.IsNullOrEmpty(attrValue)
                && TryReplaceTextInPlace(ref attrValue, findWhat, newText)) {
                xAttr.Value = attrValue;
            }
        }

        /// <summary>
        /// All path separators
        /// </summary>
        private static readonly char[] slashChars = {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };

        /// <summary>
        /// Pattern that matches one path separator char
        /// </summary>
        private static readonly RegExpr slash = CharSet[slashChars];

        /// <summary>
        /// Gets a RegExpr that matches a given path, regardless
        /// of case and varying directory separators
        /// </summary>
        private static RegExpr GetPathPattern(string findWhatPath)
        {
            return
                // Make pattern case-insensitive
                CaseInsensitive &
                // Split path string by directory separators
                findWhatPath.Split(slashChars, StringSplitOptions.RemoveEmptyEntries)
                // Convert path parts to RegExpr (escapes regex special chars)
                .Select(dirName => (RegExpr)dirName)
                // Join all parts, separated by path separator pattern
                .Aggregate((path, dirName) => path & slash & dirName);
        }

        public void ReplacePath(string oldPath, string newPath)
        {
            Uri srcUri = new Uri(Path.GetFullPath(oldPath));
            Uri projUri = new Uri(this[Files.Project].filePath);

            RegExpr absolutePath = GetPathPattern(srcUri.AbsolutePath);
            RegExpr relativePath = GetPathPattern(projUri.MakeRelativeUri(srcUri).OriginalString);

            Regex findWhat = (absolutePath | relativePath).Render().Regex;

            foreach (var xElem in this[Files.Project].xml.Descendants()) {
                if (!xElem.HasElements)
                    ReplaceText(xElem, findWhat, newPath);
                foreach (var xAttr in xElem.Attributes())
                    ReplaceText(xAttr, findWhat, newPath);
            }
            this[Files.Project].isDirty = true;
            Commit();
        }

        private class MSBuildEvaluator : IVsMacroExpander, IDisposable
        {
            private readonly MsBuildXmlFile projFile;
            private string tempProjFilePath;
            private XElement evaluateTarget;
            private XElement evaluateProperty;
            private ProjectRootElement projRoot;
            private readonly Dictionary<string, string> expansionCache;

            public Dictionary<string, string> Properties
            {
                get;
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
                if (evaluateTarget == null)
                    return;
                evaluateTarget.Remove();
                if (File.Exists(tempProjFilePath))
                    File.Delete(tempProjFilePath);
            }

            private string ExpansionCacheKey(string stringToExpand)
            {
                var key = new StringBuilder();
                foreach (var property in Properties)
                    key.AppendFormat("{0};{1};", property.Key, property.Value);
                key.Append(stringToExpand);
                return key.ToString();
            }

            private bool TryExpansionCache(string stringToExpand, out string expandedString)
            {
                return expansionCache.TryGetValue(
                    ExpansionCacheKey(stringToExpand), out expandedString);
            }

            private void AddToExpansionCache(string stringToExpand, string expandedString)
            {
                expansionCache[ExpansionCacheKey(stringToExpand)] = expandedString;
            }

            public string ExpandString(string stringToExpand)
            {
                if (TryExpansionCache(stringToExpand, out string expandedString))
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
                    projInst, new[] { "MSBuildEvaluatorTarget" },
                    null, BuildRequestDataFlags.ProvideProjectStateAfterBuild);
                var buildResult = BuildManager.DefaultBuildManager.Build(
                    new BuildParameters(), buildRequest);
                expandedString = buildResult.ProjectStateAfterBuild
                    .GetPropertyValue("MSBuildEvaluatorProperty");

                AddToExpansionCache(stringToExpand, expandedString);
                return expandedString;
            }
        }

        private class CustomBuildEval
        {
            public string ProjectConfig { get; set; }
            public string Identity { get; set; }
            public string AdditionalInputs { get; set; }
            public string Outputs { get; set; }
            public string Message { get; set; }
            public string Command { get; set; }
        }

        private List<CustomBuildEval> EvaluateCustomBuild()
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
                            Command = cbEval.Groups[5].Value
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
                        { { "QtVSToolsBuild", "true" } },
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

        private static readonly Regex ConditionParser =
            new(@"\'\$\(Configuration[^\)]*\)\|\$\(Platform[^\)]*\)\'\=\=\'([^\']+)\'");

        private class MsBuildConverterProvider : IPropertyStorageProvider
        {
            public string GetProperty(object propertyStorage, string itemType, string propertyName)
            {
                if (propertyStorage is not XElement xmlPropertyStorage)
                    return "";

                var item = xmlPropertyStorage;
                if (xmlPropertyStorage.Name.LocalName != "ItemDefinitionGroup")
                    return item.Element(ns + propertyName)?.Value;

                item = xmlPropertyStorage.Element(ns + itemType);
                return item == null ? "" : item.Element(ns + propertyName)?.Value;
            }

            public bool SetProperty(
                object propertyStorage,
                string itemType,
                string propertyName,
                string propertyValue)
            {
                if (propertyStorage is not XElement xmlPropertyStorage)
                    return false;

                var item = xmlPropertyStorage;
                if (xmlPropertyStorage.Name.LocalName == "ItemDefinitionGroup") {
                    item = xmlPropertyStorage.Element(ns + itemType);
                    if (item == null)
                        xmlPropertyStorage.Add(item = new XElement(ns + itemType));
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
                if (propertyStorage is not XElement xmlPropertyStorage)
                    return false;

                var item = xmlPropertyStorage;
                if (xmlPropertyStorage.Name.LocalName == "ItemDefinitionGroup") {
                    item = xmlPropertyStorage.Element(ns + itemType);
                    if (item == null)
                        return true;
                }

                item.Element(ns + propertyName)?.Remove();
                return true;
            }

            public string GetConfigName(object propertyStorage)
            {
                if (propertyStorage is not XElement xmlPropertyStorage)
                    return "";

                if (xmlPropertyStorage.Name.LocalName != "ItemDefinitionGroup")
                    return xmlPropertyStorage.Attribute("ConfigName")?.Value;

                var configName = ConditionParser
                    .Match(xmlPropertyStorage.Attribute("Condition").Value);
                if (!configName.Success || configName.Groups.Count <= 1)
                    return "";
                return configName.Groups[1].Value;
            }

            public string GetItemType(object propertyStorage)
            {
                if (propertyStorage is not XElement xmlPropertyStorage)
                    return "";

                if (xmlPropertyStorage.Name.LocalName == "ItemDefinitionGroup")
                    return "";
                return xmlPropertyStorage.Name.LocalName;
            }

            public string GetItemName(object propertyStorage)
            {
                if (propertyStorage is not XElement xmlPropertyStorage)
                    return "";
                if (xmlPropertyStorage.Name.LocalName == "ItemDefinitionGroup")
                    return "";
                return xmlPropertyStorage.Attribute("Include")?.Value;
            }

            public object GetParentProject(object propertyStorage)
            {
                if (propertyStorage is XElement xmlPropertyStorage)
                    return xmlPropertyStorage.Document?.Root;
                return "";
            }

            public object GetProjectConfiguration(object project, string configName)
            {
                if (project is not XElement xmlProject)
                    return null;
                return xmlProject.Elements(ns + "ItemDefinitionGroup")
                    .FirstOrDefault(config => config.Attribute("Condition").Value.Contains(configName));
            }

            public IEnumerable<object> GetItems(
                object project,
                string itemType,
                string configName = "")
            {
                if (project is not XElement xmlProject)
                    return new List<object>();
                return xmlProject.Elements(ns + "ItemGroup")
                    .Elements(ns + "CustomBuild")
                    .Elements(ns + itemType)
                    .Where(item =>
                        configName == "" || item.Attribute("ConfigName").Value == configName)
                    .GroupBy(item => item.Attribute("Include").Value)
                    .Select(item => item.First());
            }
        }

    }
}
