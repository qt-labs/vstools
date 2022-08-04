/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core.Legacy
{
    using Core;

    public static class QtProject
    {
        public static void MarkAsDesignerPluginProject(Core.QtProject qtPro)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            qtPro.Project.Globals["IsDesignerPlugin"] = true.ToString();
            if (!qtPro.Project.Globals.get_VariablePersists("IsDesignerPlugin"))
                qtPro.Project.Globals.set_VariablePersists("IsDesignerPlugin", true);
        }

        public static bool PromptChangeQtVersion(Project project, string oldVersion, string newVersion)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var versionManager = Core.QtVersionManager.The();
            var viOld = versionManager.GetVersionInfo(oldVersion);
            var viNew = versionManager.GetVersionInfo(newVersion);

            if (viOld == null || viNew == null)
                return true;

            var oldIsWinRt = viOld.isWinRT();
            var newIsWinRt = viNew.isWinRT();

            if (newIsWinRt == oldIsWinRt || newIsWinRt == IsWinRT(project))
                return true;

            var caption = string.Format("Change Qt Version ({0})", project.Name);
            var text = string.Format(
                "Changing Qt version from {0} to {1}.\r\n" +
                "Project might not build. Are you sure?",
                newIsWinRt ? "Win32" : "WinRT",
                newIsWinRt ? "WinRT" : "Win32"
            );

            return MessageBox.Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                == DialogResult.Yes;
        }

        public static bool HasModule(Project project, int id, string qtVersion = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var foundInIncludes = false;
            var foundInLibs = false;

            var vm = Core.QtVersionManager.The();
            var versionInfo = qtVersion != null ? vm.GetVersionInfo(qtVersion)
                : vm.GetVersionInfo(project);
            if (versionInfo == null)
                versionInfo = vm.GetVersionInfo(vm.GetDefaultVersion());
            if (versionInfo == null)
                return false; // neither a default or project Qt version
            var info = QtModules.Instance.Module(id, versionInfo.qtMajor);
            if (info == null)
                return false;

            var vcPro = project.Object as VCProject;
            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations) {
                var compiler = CompilerToolWrapper.Create(config);
                var linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");

                if (compiler != null) {
                    if (compiler.GetAdditionalIncludeDirectories() == null)
                        continue;
                    var incPathList = info.GetIncludePath();
                    var includeDirs = compiler.GetAdditionalIncludeDirectoriesList();
                    foundInIncludes = (incPathList.Count > 0);
                    foreach (var incPath in incPathList) {
                        var fixedIncludeDir = FixFilePathForComparison(incPath);
                        if (!includeDirs.Any(dir =>
                            FixFilePathForComparison(dir) == fixedIncludeDir)) {
                            foundInIncludes = false;
                            break;
                        }
                    }
                }

                if (foundInIncludes)
                    break;

                List<string> libs = null;
                if (linker != null) {
                    var linkerWrapper = new LinkerToolWrapper(linker);
                    libs = linkerWrapper.AdditionalDependencies;
                }

                if (libs != null) {
                    var moduleLibs = info.GetLibs(IsDebugConfiguration(config), versionInfo);
                    foundInLibs = moduleLibs.All(moduleLib => libs.Contains(moduleLib));
                }
            }
            return foundInIncludes || foundInLibs;
        }

        public static void AddModule(Project project, int id)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (HasModule(project, id))
                return;

            var vm = Core.QtVersionManager.The();
            var versionInfo = vm.GetVersionInfo(project);
            if (versionInfo == null)
                versionInfo = vm.GetVersionInfo(vm.GetDefaultVersion());

            var vcPro = project.Object as VCProject;
            foreach (VCConfiguration config in vcPro.Configurations as IVCCollection) {

                var info = QtModules.Instance.Module(id, versionInfo.qtMajor);
                if (Core.QtProject.GetFormatVersion(project) >= Resources.qtMinFormatVersion_Settings) {
                    var config3 = config as VCConfiguration3;
                    if (config3 == null)
                        continue;
                    if (!string.IsNullOrEmpty(info.proVarQT)) {
                        var qtModulesValue = config.GetUnevaluatedPropertyValue("QtModules");
                        var qtModules = new HashSet<string>(
                            !string.IsNullOrEmpty(qtModulesValue)
                                ? qtModulesValue.Split(';')
                                : new string[] { });
                        qtModules.UnionWith(info.proVarQT.Split(' '));
                        config3.SetPropertyValue(Resources.projLabelQtSettings, true,
                            "QtModules", string.Join(";", qtModules));
                    }
                    // In V3 project format, compiler and linker options
                    // required by modules are set by Qt/MSBuild.
                    continue;
                }

                var compiler = CompilerToolWrapper.Create(config);
                var linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");

                if (compiler != null) {
                    foreach (var define in info.Defines)
                        compiler.AddPreprocessorDefinition(define);

                    var incPathList = info.GetIncludePath();
                    foreach (var incPath in incPathList)
                        compiler.AddAdditionalIncludeDirectories(incPath);
                }
                if (linker != null) {
                    var moduleLibs = info.GetLibs(IsDebugConfiguration(config), versionInfo);
                    var linkerWrapper = new LinkerToolWrapper(linker);
                    var additionalDeps = linkerWrapper.AdditionalDependencies;
                    var dependenciesChanged = false;
                    if (additionalDeps == null || additionalDeps.Count == 0) {
                        additionalDeps = moduleLibs;
                        dependenciesChanged = true;
                    } else {
                        foreach (var moduleLib in moduleLibs) {
                            if (!additionalDeps.Contains(moduleLib)) {
                                additionalDeps.Add(moduleLib);
                                dependenciesChanged = true;
                            }
                        }
                    }
                    if (dependenciesChanged)
                        linkerWrapper.AdditionalDependencies = additionalDeps;
                }
            }
        }

        public static void RemoveModule(Project project, int id)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var vm = Core.QtVersionManager.The();
            var versionInfo = vm.GetVersionInfo(project);
            if (versionInfo == null)
                versionInfo = vm.GetVersionInfo(vm.GetDefaultVersion());

            var vcPro = project.Object as VCProject;
            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations) {
                var compiler = CompilerToolWrapper.Create(config);
                var linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");

                var info = QtModules.Instance.Module(id, versionInfo.qtMajor);
                if (compiler != null) {
                    foreach (var define in info.Defines)
                        compiler.RemovePreprocessorDefinition(define);
                    var additionalIncludeDirs = compiler.AdditionalIncludeDirectories;
                    if (additionalIncludeDirs != null) {
                        var lst = new List<string>(additionalIncludeDirs);
                        foreach (var includePath in info.IncludePath) {
                            lst.Remove(includePath);
                            lst.Remove('\"' + includePath + '\"');
                        }
                        compiler.AdditionalIncludeDirectories = lst;
                    }
                }
                if (linker != null && linker.AdditionalDependencies != null) {
                    var linkerWrapper = new LinkerToolWrapper(linker);
                    var moduleLibs = info.GetLibs(IsDebugConfiguration(config), versionInfo);
                    var additionalDependencies = linkerWrapper.AdditionalDependencies;
                    var dependenciesChanged = false;
                    foreach (var moduleLib in moduleLibs)
                        dependenciesChanged |= additionalDependencies.Remove(moduleLib);
                    if (dependenciesChanged)
                        linkerWrapper.AdditionalDependencies = additionalDependencies;
                }
            }
        }
        
        internal static bool IsDesignerPluginProject(Core.QtProject qtPro)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var b = false;
            if (qtPro.Project.Globals.get_VariablePersists("IsDesignerPlugin")) {
                var s = qtPro.Project.Globals["IsDesignerPlugin"] as string;
                try {
                    b = bool.Parse(s);
                } catch { }
            }
            return b;
        }


        private static bool IsWinRT(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try {
                var vcProject = project.Object as VCProject;
                var vcConfigs = vcProject.Configurations as IVCCollection;
                var vcConfig = vcConfigs.Item(1) as VCConfiguration;
                var appType = vcConfig.GetEvaluatedPropertyValue("ApplicationType");
                if (appType == "Windows Store")
                    return true;
            } catch { }
            return false;
        }

        private static string FixFilePathForComparison(string path)
        {
            return HelperFunctions.NormalizeRelativeFilePath(path).ToLower();
        }

        private static bool IsDebugConfiguration(VCConfiguration conf)
        {
            var tool = CompilerToolWrapper.Create(conf);
            if (tool != null) {
                return tool.RuntimeLibrary == runtimeLibraryOption.rtMultiThreadedDebug
                    || tool.RuntimeLibrary == runtimeLibraryOption.rtMultiThreadedDebugDLL;
            }
            return false;
        }
    }
}
