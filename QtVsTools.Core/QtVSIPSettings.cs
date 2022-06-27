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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using QtVsTools.Common;

namespace QtVsTools.Core
{
    public interface IQtVsToolsOptions
    {
        string QtMsBuildPath { get; }
        bool QmlDebuggerEnabled { get; }
        int QmlDebuggerTimeout { get; }
        bool HelpPreferenceOnline { get; }
    }

    public static class QtVSIPSettings
    {
        public static IQtVsToolsOptions Options { get; set; }

        public static bool GetDisableAutoMocStepsUpdate()
        {
            return QtVSIPSettingsShared.GetBoolValue(Resources.disableAutoMocStepsUpdateKeyword, false);
        }

        public static string GetUicDirectory(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return QtVSIPSettingsShared.GetDirectory(project, Resources.uicDirKeyword);
        }

        public static string GetMocDirectory()
        {
            return QtVSIPSettingsShared.GetDirectory(Resources.mocDirKeyword);
        }

        public static string GetMocDirectory(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return QtVSIPSettingsShared.GetDirectory(project, Resources.mocDirKeyword);
        }

        public static string GetMocDirectory(
            EnvDTE.Project project,
            string configName,
            string platformName, VCFile vCFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string filePath = null;
            if (vCFile != null)
                filePath = vCFile.FullPath;
            return GetMocDirectory(project, configName, platformName, filePath);
        }

        public static string GetMocDirectory(
            EnvDTE.Project project,
            string configName,
            string platformName,
            string filePath = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dir = QtVSIPSettingsShared.GetDirectory(project, Resources.mocDirKeyword);
            if (!string.IsNullOrEmpty(configName)
                && !string.IsNullOrEmpty(platformName))
                HelperFunctions.ExpandString(ref dir, project, configName, platformName, filePath);
            return dir;
        }

        public static bool HasDifferentMocFilePerConfig(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var mocDir = GetMocDirectory(project);
            return mocDir.Contains("$(ConfigurationName)");
        }

        public static bool HasDifferentMocFilePerPlatform(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var mocDir = GetMocDirectory(project);
            return mocDir.Contains("$(PlatformName)");
        }

        public static string GetMocOptions(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return QtVSIPSettingsShared.GetOption(project, Resources.mocOptionsKeyword);
        }

        public static bool GetLUpdateOnBuild(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (QtVSIPSettingsShared.GetProjectQtSetting(project, "QtRunLUpdateOnBuild") == "true")
                return true;
            return QtVSIPSettingsShared.GetBoolValue(project, Resources.lupdateKeyword);
        }

        public static string GetRccDirectory(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return QtVSIPSettingsShared.GetDirectory(project, Resources.rccDirKeyword);
        }

        public static string GetRccDirectory()
        {
            return QtVSIPSettingsShared.GetDirectory(Resources.rccDirKeyword);
        }

        public static string GetUicDirectory()
        {
            return QtVSIPSettingsShared.GetDirectory(Resources.uicDirKeyword);
        }

        public static bool AutoUpdateUicSteps()
        {
            if (QtVSIPSettingsShared.ValueExists("AutoUpdateUicSteps"))
                return QtVSIPSettingsShared.GetBoolValue("AutoUpdateUicSteps", true);
            return QtVSIPSettingsShared.GetBoolValue("AutoUpdateBuildSteps", true);
        }
    }
}
