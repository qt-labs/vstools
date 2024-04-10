/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace QtVsTools.Core.MsBuild
{
    using static HelperFunctions;
    using static Common.Utils;

    public partial class MsBuildProjectReaderWriter
    {
        private bool UpgradeFromV2()
        {
            var qtInstallValue = QtVersionManager.GetDefaultVersion();

            // Get project user properties (old format)
            XElement refreshUserProps() => this[Files.Project].Xml
                .Elements(ns + "Project")
                .Elements(ns + "ProjectExtensions")
                .Elements(ns + "VisualStudio")
                .Elements(ns + "UserProperties")
                .FirstOrDefault();
            var userProps = refreshUserProps();

            // Copy Qt build reference to QtInstall project property
            this[Files.Project].Xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Where(x => (string)x.Attribute("Label") == "QtSettings")
                .ToList()
                .ForEach(config =>
                {
                    if (userProps != null) {
                        string platform = null;
                        try {
                            platform = ConfigCondition
                                .Parse((string)config.Attribute("Condition"))
                                .GetValues<string>("Platform")
                                .FirstOrDefault();
                        } catch (Exception e) {
                            e.Log();
                        }

                        if (!string.IsNullOrEmpty(platform)) {
                            var qtInstallName = $"Qt5Version_x0020_{platform}";
                            qtInstallValue = (string)userProps.Attribute(qtInstallName);
                        }
                    }
                    if (!string.IsNullOrEmpty(qtInstallValue))
                        config.Add(new XElement(ns + "QtInstall", qtInstallValue));
                });
            Commit("Copying Qt build reference to QtInstall project property");

            // Get C++ compiler properties
            List<XElement> refreshCompiler() => this[Files.Project].Xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Elements(ns + "ClCompile")
                .ToList();
            var compiler = refreshCompiler();

            // Get linker properties
            List<XElement> refreshLinker() => this[Files.Project].Xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Elements(ns + "Link")
                .ToList();
            var linker = refreshLinker();

            List<XElement> refreshResourceCompiler() => this[Files.Project].Xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Elements(ns + "ResourceCompile")
                .ToList();
            var resourceCompiler = refreshResourceCompiler();

            // Qt module names, to copy to QtModules property
            var moduleNames = new HashSet<string>();

            // Qt module macros, to remove from compiler macros property
            var moduleDefines = new HashSet<string>();

            // Qt module includes, to remove from compiler include directories property
            var moduleIncludePaths = new HashSet<string>();

            // Qt module link libraries, to remove from liker dependencies property
            var moduleLibs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
            Commit("Removing Qt module macros from compiler properties");

            // Remove Qt module include paths from compiler properties
            compiler = refreshCompiler();
            foreach (var inclPath in compiler.Elements(ns + "AdditionalIncludeDirectories")) {
                inclPath.SetValue(string.Join(";", inclPath.Value.Split(';')
                    .Select(Unquote)
                    // Exclude paths rooted on $(QTDIR)
                    .Where(x => !x.StartsWith("$(QTDIR)", IgnoreCase))));
            }
            Commit("Removing Qt module include paths from compiler properties");

            // Remove Qt module libraries from linker properties
            linker = refreshLinker();
            foreach (var libs in linker.Elements(ns + "AdditionalDependencies")) {
                libs.SetValue(string.Join(";", libs.Value.Split(';')
                    .Where(x => !moduleLibs.Contains(Path.GetFileName(Unquote(x))))));
            }
            Commit("Removing Qt module libraries from linker properties");

            // Remove Qt lib path from linker properties
            linker = refreshLinker();
            foreach (var libs in linker.Elements(ns + "AdditionalLibraryDirectories")) {
                libs.SetValue(string.Join(";", libs.Value.Split(';')
                    .Select(Unquote)
                    // Exclude paths rooted on $(QTDIR)
                    .Where(x => !x.StartsWith("$(QTDIR)", IgnoreCase))));
            }
            Commit("Removing Qt lib path from linker properties");

            // Remove Qt module macros from resource compiler properties
            resourceCompiler = refreshResourceCompiler();
            foreach (var defines in resourceCompiler.Elements(ns + "PreprocessorDefinitions")) {
                defines.SetValue(string.Join(";", defines.Value.Split(';')
                    .Where(x => !moduleDefines.Contains(x))));
            }
            Commit("Removing Qt module macros from resource compiler properties");

            if (VersionInformation.GetOrAddByName(qtInstallValue) is {} qtVersion) {
                moduleNames = QtModules.Instance.GetAvailableModules(qtVersion.Major)
                    // remove proVarQT values not provided by the used Qt version
                    .SelectMany(x => x.proVarQT?.Split(' ') ?? Array.Empty<string>())
                    .SelectMany(x => x.EndsWith("-private") ? new[] { x } : new[] { x, $"{x}-private" })
                    .Intersect(moduleNames)
                    .ToHashSet();
            }

            this[Files.Project].Xml
                .Elements(ns + "Project")
                .Elements(ns + "PropertyGroup")
                .Where(x => (string)x.Attribute("Label") == "QtSettings")
                .ToList()
                .ForEach(x => x.Add(new XElement(ns + "QtModules", string.Join(";", moduleNames))));
            Commit("Adding Qt module names to QtModules project property");

            // Remove project user properties (old format)
            userProps = refreshUserProps();
            userProps?.Attributes().ToList().ForEach(userProp =>
            {
                if (userProp.Name.LocalName.StartsWith("Qt5Version_x0020_")
                    || userProp.Name.LocalName is "lupdateOptions" or "lupdateOnBuild"
                        or "lreleaseOptions" or "MocDir" or "MocOptions" or "RccDir"
                        or "UicDir") {
                    userProp.Remove();
                }
            });
            Commit("Removing project user properties (format version 2)");

            // Remove old properties from .user file
            if (this[Files.User].Xml != null) {
                this[Files.User].Xml
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
                Commit("Removing old properties from .user file");
            }

            // Convert OutputFile --> <tool>Dir + <tool>FileName
            var qtItems = this[Files.Project].Xml
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
            Commit("Converting OutputFile to <tool>Dir and <tool>FileName");

            // Remove old properties from project items
            var oldQtProps = new[] { "QTDIR", "InputFile", "OutputFile" };
            var oldCppProps = new[] { "IncludePath", "Define", "Undefine" };
            var oldPropsAny = oldQtProps.Union(oldCppProps);
            this[Files.Project].Xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemDefinitionGroup")
                .Union(this[Files.Project].Xml
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
            Commit("Removing old properties from project items");

            return true;
        }
    }
}
