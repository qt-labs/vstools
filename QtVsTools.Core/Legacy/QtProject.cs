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
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core.Legacy
{
    using Core;

    public static class QtProject
    {
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
