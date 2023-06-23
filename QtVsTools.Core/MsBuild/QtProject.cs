/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core.MsBuild
{
    using static Instances;
    using static Utils;

    /// <summary>
    /// QtProject holds the Qt specific properties for a Visual Studio project.
    /// There exists at most one QtProject per EnvDTE.Project.
    /// Use QtProject.GetOrAdd to get the QtProject for a Project or VCProject.
    /// </summary>
    public partial class QtProject : Concurrent<QtProject>
    {
        private static readonly Dictionary<VCProject, QtProject> Instances = new();
        private readonly QtMsBuildContainer qtMsBuild = new(new VcPropertyStorageProvider());

        public static QtProject GetOrAdd(VCProject vcProject)
        {
            if (vcProject == null)
                return null;
            lock (StaticCriticalSection) {
                if (ProjectFormat.GetVersion(vcProject) >= ProjectFormat.Version.V3) {
                    if (Instances.TryGetValue(vcProject, out var qtProject))
                        return qtProject;
                    qtProject = new QtProject(vcProject);
                    Instances.Add(vcProject, qtProject);
                    return qtProject;
                }

                if (ProjectFormat.GetVersion(vcProject) >= ProjectFormat.Version.V1)
                    ShowUpdateFormatMessage();

                return null; // ignore old or unknown projects
            }
        }

        public static void Reset()
        {
            lock (StaticCriticalSection) {
                Instances.Clear();
            }
        }

        private QtProject(VCProject vcProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VcProject = vcProject;
            VcProjectPath = vcProject.ProjectFile;
            VcProjectDirectory = vcProject.ProjectDirectory;
        }

        public VCProject VcProject { get; }
        public string VcProjectPath { get; }
        public string VcProjectDirectory { get; }

        public static string GetRuleName(VCConfiguration config, string itemType)
        {
            if (config == null)
                return string.Empty;
            try {
                return config.GetEvaluatedPropertyValue(itemType + "RuleName");
            } catch (Exception exception) {
                exception.Log();
                return string.Empty;
            }
        }

        public string QtVersion
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return GetPropertyValue("QtInstall");
            }
        }

        public ProjectFormat.Version FormatVersion
        {
            get
            {
                return ProjectFormat.GetVersion(VcProject);
            }
        }

        public string InstallPath
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return VersionManager.GetInstallPath(this);
            }
        }

        public VersionInformation VersionInfo
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return VersionManager.GetVersionInfo(QtVersion);
            }
        }

        public string GetPropertyValue(string propertyName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return VcProject.ActiveConfiguration is {} activeConfiguration
                ? activeConfiguration.GetEvaluatedPropertyValue(propertyName)
                : null;
        }

        /// <summary>
        /// Returns the files specified by the file name from a given project as list of VCFile
        /// objects.
        /// </summary>
        /// <param name="fileName">file name (relative path)</param>
        /// <returns></returns>
        public IEnumerable<VCFile> GetFilesFromProject(string fileName)
        {
            var fi = new FileInfo(HelperFunctions.NormalizeRelativeFilePath(fileName));
            foreach (VCFile f in (IVCCollection)VcProject.Files) {
                if (f.Name.Equals(fi.Name, IgnoreCase))
                    yield return f;
            }
        }

        /// <summary>
        /// Removes a file from the filter.
        /// This file will be deleted!
        /// </summary>
        /// <param name="file">file</param>
        private void RemoveFileFromFilter(VCFile file, FakeFilter filter)
        {
            try {
                var vfilt = FindFilterFromGuid(filter.UniqueIdentifier)
                          ?? FindFilterFromName(filter.Name);

                if (vfilt == null)
                    return;

                RemoveFileFromFilter(file, vfilt);
            } catch {
                throw new QtVSException($"Cannot remove file {file.Name} from filter.");
            }
        }

        /// <summary>
        /// Removes a file from the filter.
        /// This file will be deleted!
        /// </summary>
        /// <param name="file">file</param>
        private void RemoveFileFromFilter(VCFile file, VCFilter filter)
        {
            try {
                filter.RemoveFile(file);
                var fi = new FileInfo(file.FullPath);
                if (fi.Exists)
                    fi.Delete();
            } catch {
            }

            var subfilters = (IVCCollection)filter.Filters;
            for (var i = subfilters.Count; i > 0; i--) {
                try {
                    var subfilter = (VCFilter)subfilters.Item(i);
                    RemoveFileFromFilter(file, subfilter);
                } catch {
                }
            }
        }

        public VCFilter FindFilterFromName(string filtername)
        {
            try {
                foreach (VCFilter vcfilt in (IVCCollection)VcProject.Filters) {
                    if (vcfilt.Name.ToLower() == filtername.ToLower())
                        return vcfilt;
                }
                return null;
            } catch {
                throw new QtVSException("Cannot find filter.");
            }
        }

        public VCFilter FindFilterFromGuid(string filterguid)
        {
            try {
                foreach (VCFilter vcfilt in (IVCCollection)VcProject.Filters) {
                    if (vcfilt.UniqueIdentifier != null
                        && vcfilt.UniqueIdentifier.ToLower() == filterguid.ToLower()) {
                        return vcfilt;
                    }
                }
                return null;
            } catch {
                throw new QtVSException("Cannot find filter.");
            }
        }

        public void MarkAsQtPlugin()
        {
            if (VcProject.Configurations is not IVCCollection configurations)
                return;

            foreach (VCConfiguration config in configurations) {
                if (config.Rules.Item("QtRule10_Settings") is IVCRulePropertyStorage rule)
                    rule.SetPropertyValue("QtPlugin", "true");
            }
        }

        public void AddActiveQtBuildStep(string version, string defFile = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (VCConfiguration config in (IVCCollection)VcProject.Configurations) {
                var idlFile = "\"$(IntDir)/" + VcProject.Name + ".idl\"";
                var tblFile = "\"$(IntDir)/" + VcProject.Name + ".tlb\"";

                var tool = (VCPostBuildEventTool)((IVCCollection)config.Tools).Item("VCPostBuildEventTool");
                var idc = "$(QTDIR)\\bin\\idc.exe \"$(TargetPath)\" /idl " + idlFile + " -version " + version;
                var midl = "midl " + idlFile + " /tlb " + tblFile;
                var idc2 = "$(QTDIR)\\bin\\idc.exe \"$(TargetPath)\" /tlb " + tblFile;
                var idc3 = "$(QTDIR)\\bin\\idc.exe \"$(TargetPath)\" /regserver";

                tool.CommandLine = idc + "\r\n" + midl + "\r\n" + idc2 + "\r\n" + idc3;
                tool.Description = string.Empty;

                var linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");
                var librarian = (VCLibrarianTool)((IVCCollection)config.Tools).Item("VCLibrarianTool");

                if (linker != null) {
                    linker.Version = version;
                    linker.ModuleDefinitionFile = defFile ?? VcProject.Name + ".def";
                } else {
                    librarian.ModuleDefinitionFile = defFile ?? VcProject.Name + ".def";
                }
            }
        }

        public bool UsesPrecompiledHeaders()
        {
            if (VcProject.Configurations is not IVCCollection configurations)
                return false;

            const pchOption pchNone = pchOption.pchNone;
            return configurations.Cast<VCConfiguration>()
                .Select(CompilerToolWrapper.Create)
                .All(compiler => (compiler?.GetUsePrecompiledHeader() ?? pchNone) != pchNone);
        }

        public string GetPrecompiledHeaderThrough()
        {
            if (VcProject.Configurations is not IVCCollection configurations)
                return null;

            return configurations.Cast<VCConfiguration>()
                .Select(CompilerToolWrapper.Create)
                .Select(compiler => compiler?.GetPrecompiledHeaderThrough() ?? "")
                .Where(header => !string.IsNullOrEmpty(header))
                .Select(header => header.ToLower())
                .FirstOrDefault();
        }

        public static void SetPCHOption(VCFile vcFile, pchOption option)
        {
            if (vcFile.FileConfigurations is not IVCCollection fileConfigurations)
                return;

            foreach (VCFileConfiguration config in fileConfigurations)
                CompilerToolWrapper.Create(config)?.SetUsePrecompiledHeader(option);
        }

        public void RemoveGeneratedFiles(string fileName)
        {
            var fi = new FileInfo(fileName);
            var lastIndex = fileName.LastIndexOf(fi.Extension, StringComparison.Ordinal);
            var baseName = fi.Name.Remove(lastIndex, fi.Extension.Length);
            string delName = null;
            if (HelperFunctions.IsHeaderFile(fileName))
                delName = "moc_" + baseName + ".cpp";
            else if (HelperFunctions.IsSourceFile(fileName) && !fileName.StartsWith("moc_", IgnoreCase))
                delName = baseName + ".moc";
            else if (HelperFunctions.IsUicFile(fileName))
                delName = "ui_" + baseName + ".h";
            else if (HelperFunctions.IsQrcFile(fileName))
                delName = "qrc_" + baseName + ".cpp";

            if (delName != null) {
                foreach (var delFile in GetFilesFromProject(delName))
                    RemoveFileFromFilter(delFile, Filters.GeneratedFiles());
            }
        }

        private class CppConfig
        {
            public VCConfiguration Config;
            public IVCRulePropertyStorage Cpp;

            public string GetUserPropertyValue(string pszPropName)
            {
                try {
                    var storage = (Config.project as VCProject) as IVCBuildPropertyStorage;
                    return storage.GetPropertyValue(pszPropName, Config.Name, "UserFile");
                } catch (Exception exception) {
                    exception.Log();
                    return string.Empty;
                }
            }

            public void SetUserPropertyValue(string pszPropName, string pszPropValue)
            {
                try {
                    var storage = (Config.project as VCProject) as IVCBuildPropertyStorage;
                    storage.SetPropertyValue(pszPropName, Config.Name, "UserFile", pszPropValue);
                } catch (Exception exception) {
                    exception.Log();
                }
            }

            public void RemoveUserProperty(string pszPropName)
            {
                try {
                    var storage = (Config.project as VCProject) as IVCBuildPropertyStorage;
                    storage.RemoveProperty(pszPropName, Config.Name, "UserFile");
                } catch (Exception exception) {
                    exception.Log();
                }
            }
        }

        private static IEnumerable<CppConfig> GetCppConfigs(VCProject vcPro)
        {
            return ((IVCCollection)vcPro.Configurations).Cast<VCConfiguration>()
                .Select(x => new CppConfig
                {
                    Config = x,
                    Cpp = x.Rules.Item("CL") as IVCRulePropertyStorage
                })
                .Where(x => x.Cpp != null
                    && x.Config.GetEvaluatedPropertyValue("ApplicationType") != "Linux");
        }

        private static IEnumerable<CppConfig> GetCppDebugConfigs(VCProject vcPro)
        {
            var cppConfigs = GetCppConfigs(vcPro)
                .Select(x => new { Self = x, x.Cpp });
            var cppConfigMacros = cppConfigs
                .Select(x => new
                {
                    x.Self,
                    Macros = x.Cpp.GetEvaluatedPropertyValue("PreprocessorDefinitions")
                })
                .Where(x => !string.IsNullOrEmpty(x.Macros));
            var cppDebugConfigs = cppConfigMacros
                .Where(x => !x.Macros.Split(';').Contains("QT_NO_DEBUG"))
                .Select(x => x.Self);
            return cppDebugConfigs;
        }

        private static bool IsQtQmlDebugDefined(VCProject vcPro)
        {
            var cppConfigs = GetCppConfigs(vcPro)
                .Select(x => new { Self = x, x.Cpp });
            var cppConfigMacros = cppConfigs
                .Select(x => new
                {
                    x.Self,
                    Macros = x.Cpp.GetEvaluatedPropertyValue("PreprocessorDefinitions")
                })
                .Where(x => !string.IsNullOrEmpty(x.Macros));
            return cppConfigMacros
                .Any(x => x.Macros.Split(';').Contains("QT_QML_DEBUG"));
        }

        private static void DefineQtQmlDebug(VCProject vcPro)
        {
            var configs = GetCppDebugConfigs(vcPro).Where(x => x.Cpp
                .GetEvaluatedPropertyValue("PreprocessorDefinitions").Split(';')
                .Contains("QT_QML_DEBUG") == false)
                .Select(x => new
                {
                    x.Cpp,
                    Macros = x.Cpp.GetUnevaluatedPropertyValue("PreprocessorDefinitions")
                });

            foreach (var config in configs) {
                config.Cpp.SetPropertyValue("PreprocessorDefinitions",
                    $"QT_QML_DEBUG;{config.Macros}");
            }
        }

        private static void UndefineQtQmlDebug(VCProject vcPro)
        {
            var configs = GetCppDebugConfigs(vcPro)
                .Where(x => x.Cpp.GetEvaluatedPropertyValue("PreprocessorDefinitions").Split(';')
                    .Contains("QT_QML_DEBUG"))
                .Select(x => new
                {
                    x.Cpp,
                    Macros = x.Cpp.GetUnevaluatedPropertyValue("PreprocessorDefinitions")
                        .Split(';').ToList()
                });

            foreach (var config in configs) {
                config.Macros.Remove("QT_QML_DEBUG");
                config.Cpp.SetPropertyValue("PreprocessorDefinitions",
                    string.Join(";", config.Macros));
            }
        }

        private static bool IsQmlJsDebuggerDefined(VCProject vcPro)
        {
            foreach (var config in GetCppDebugConfigs(vcPro)) {
                var qmlDebug = config.GetUserPropertyValue("QmlDebug");
                if (string.IsNullOrEmpty(qmlDebug))
                    return false;
                var debugArgs = config.GetUserPropertyValue("LocalDebuggerCommandArguments");
                if (string.IsNullOrEmpty(debugArgs))
                    return false;
                if (!debugArgs.Contains(qmlDebug))
                    return false;
            }
            return true;
        }

        private static void DefineQmlJsDebugger(VCProject vcPro)
        {
            var configs = GetCppDebugConfigs(vcPro)
                .Select(x => new
                {
                    Self = x,
                    QmlDebug = x.GetUserPropertyValue("QmlDebug"),
                    Args = x.GetUserPropertyValue("LocalDebuggerCommandArguments")
                })
                .Where(x => string.IsNullOrEmpty(x.QmlDebug) || !x.Args.Contains(x.QmlDebug));

            foreach (var config in configs) {

                config.Self.RemoveUserProperty("LocalDebuggerCommandArguments");
                config.Self.RemoveUserProperty("QmlDebug");
                config.Self.RemoveUserProperty("QmlDebugSettings");

                config.Self.SetUserPropertyValue("QmlDebugSettings", "file:$(ProjectGuid),block");
                config.Self.SetUserPropertyValue("QmlDebug", "-qmljsdebugger=$(QmlDebugSettings)");

                config.Self.SetUserPropertyValue("LocalDebuggerCommandArguments",
                    string.Join(" ", config.Args, "$(QmlDebug)").Trim());
            }
        }

        private static void UndefineQmlJsDebugger(VCProject vcPro)
        {
            var configs = GetCppDebugConfigs(vcPro)
                .Select(x => new
                {
                    Self = x,
                    QmlDebug = x.GetUserPropertyValue("QmlDebug"),
                    Args = x.GetUserPropertyValue("LocalDebuggerCommandArguments")
                })
                .Where(x => !string.IsNullOrEmpty(x.QmlDebug) && x.Args.Contains(x.QmlDebug));

            foreach (var config in configs) {

                config.Self.SetUserPropertyValue("QmlDebug", "##QMLDEBUG##");
                var args = config.Self.GetUserPropertyValue("LocalDebuggerCommandArguments");

                var newArgs = args.Replace("##QMLDEBUG##", "").Trim();
                if (string.IsNullOrEmpty(newArgs))
                    config.Self.RemoveUserProperty("LocalDebuggerCommandArguments");
                else
                    config.Self.SetUserPropertyValue("LocalDebuggerCommandArguments", newArgs);

                config.Self.RemoveUserProperty("QmlDebug");
                config.Self.SetUserPropertyValue("QmlDebugSettings", "false");
            }
        }

        public bool QmlDebug
        {
            get => IsQtQmlDebugDefined(VcProject) && IsQmlJsDebuggerDefined(VcProject);
            set
            {
                bool enabled = IsQtQmlDebugDefined(VcProject) && IsQmlJsDebuggerDefined(VcProject);
                if (value == enabled)
                    return;

                if (value) {
                    DefineQtQmlDebug(VcProject);
                    DefineQmlJsDebugger(VcProject);
                } else {
                    UndefineQtQmlDebug(VcProject);
                    UndefineQmlJsDebugger(VcProject);
                }
            }
        }
    }
}
