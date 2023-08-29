/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.VCProjectEngine;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core.MsBuild
{
    using Common;
    using Options;
    using VisualStudio;
    using static Instances;
    using static Utils;

    /// <summary>
    /// QtProject holds the Qt specific properties for a Visual Studio project.
    /// There exists at most one QtProject perVCProject. Use QtProject.GetOrAdd
    /// to get the QtProject for a VCProject.
    /// </summary>
    public partial class MsBuildProject : Concurrent<MsBuildProject>
    {
        private static LazyFactory StaticLazy { get; } = new();

        private static ConcurrentDictionary<string, MsBuildProject> Instances => StaticLazy.Get(() =>
            Instances, () => new ConcurrentDictionary<string, MsBuildProject>());

        private static IVsTaskStatusCenterService StatusCenter => StaticLazy.Get(() =>
            StatusCenter, VsServiceProvider
            .GetService<SVsTaskStatusCenterService, IVsTaskStatusCenterService>);

        public static MsBuildProject GetOrAdd(VCProject vcProject)
        {
            if (vcProject == null)
                return null;
            lock (StaticCriticalSection) {
                if (MsBuildProjectFormat.GetVersion(vcProject) >= MsBuildProjectFormat.Version.V3) {
                    if (Instances.TryGetValue(vcProject.ProjectFile, out var project))
                        return project;
                    project = new MsBuildProject(vcProject);
                    Instances[vcProject.ProjectFile] = project;
                    if (Options.Get() is { ProjectTracking: true }) {
                        InitQueue.Enqueue(project);
                        _ = Task.Run(InitDispatcherLoopAsync);
                    }
                    return project;
                }

                if (MsBuildProjectFormat.GetVersion(vcProject) >= MsBuildProjectFormat.Version.V1)
                    ShowUpdateFormatMessage();

                return null; // ignore old or unknown projects
            }
        }

        public static void Remove(string projectPath)
        {
            lock (StaticCriticalSection)
                Instances.TryRemove(projectPath, out _);
        }

        public static void Reset()
        {
            lock (StaticCriticalSection) {
                Instances.Clear();
                InitQueue.Clear();
            }
            CloseUpdateFormatMessage();
            CloseProjectFormatUpdated();
        }

        private MsBuildProject(VCProject vcProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VcProject = vcProject;
            VcProjectPath = vcProject.ProjectFile;
            VcProjectDirectory = vcProject.ProjectDirectory;
            Initialized = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        public VCProject VcProject { get; }
        public string VcProjectPath { get; }
        public string VcProjectDirectory { get; }

        public string SolutionPath { get; set; } = "";
        public bool IsTracked =>
            Options.Get() is { ProjectTracking: true } && Instances.ContainsKey(VcProjectPath);

        public string QtVersion
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return GetPropertyValue("QtInstall");
            }
        }

        public MsBuildProjectFormat.Version FormatVersion
        {
            get
            {
                return MsBuildProjectFormat.GetVersion(VcProject);
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
    }
}
