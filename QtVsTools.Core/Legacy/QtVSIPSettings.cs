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

using System;
using Microsoft.VisualStudio.Shell;
using QtVsTools.Common;

namespace QtVsTools.Core.Legacy
{
    public static class QtVSIPSettings
    {
        #region UIC

        public static string GetUicDirectory(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return QtVSIPSettingsShared.GetDirectory(project, Resources.uicDirKeyword);
        }

        public static void SaveUicDirectory(EnvDTE.Project project, string directory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (directory == null)
                directory = QtVSIPSettingsShared.GetDirectory(project, Resources.uicDirKeyword);
            SaveDirectory(project, Resources.uicDirKeyword, directory);
        }
        #endregion

        #region MOC

        public static string GetMocDirectory(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return QtVSIPSettingsShared.GetDirectory(project, Resources.mocDirKeyword);
        }

        public static void SaveMocDirectory(EnvDTE.Project project, string directory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (directory == null)
                directory = QtVSIPSettingsShared.GetDirectory(project, Resources.mocDirKeyword);
            SaveDirectory(project, Resources.mocDirKeyword, directory);
        }

        public static string GetMocOptions()
        {
            return QtVSIPSettingsShared.GetOption(Resources.mocOptionsKeyword);
        }

        public static string GetMocOptions(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return QtVSIPSettingsShared.GetOption(project, Resources.mocOptionsKeyword);
        }

        public static void SaveMocOptions(EnvDTE.Project project, string options)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (options == null)
                options = GetMocOptions();

            SaveOption(project, Resources.mocOptionsKeyword, options);
        }
        #endregion

        #region LUpdate

        public static bool GetLUpdateOnBuild(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (QtVSIPSettingsShared.GetProjectQtSetting(project, "QtRunLUpdateOnBuild") == "true")
                return true;
            return QtVSIPSettingsShared.GetBoolValue(project, Resources.lupdateKeyword);
        }

        public static void SaveLUpdateOnBuild(EnvDTE.Project project, bool value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SetBoolValue(project, Resources.lupdateKeyword, value);
        }

        public static string GetLUpdateOptions()
        {
            return QtVSIPSettingsShared.GetOption(Resources.lupdateOptionsKeyword);
        }

        public static string GetLUpdateOptions(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string qtLUpdateOptions = QtVSIPSettingsShared.GetProjectQtSetting(project, "QtLUpdateOptions");
            if (!string.IsNullOrEmpty(qtLUpdateOptions))
                return qtLUpdateOptions;
            return QtVSIPSettingsShared.GetOption(project, Resources.lupdateOptionsKeyword);
        }

        public static void SaveLUpdateOptions(EnvDTE.Project project, string options)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (options == null)
                options = GetLUpdateOptions();

            SaveOption(project, Resources.lupdateOptionsKeyword, options);
        }
        #endregion

        #region LRelease

        public static string GetLReleaseOptions()
        {
            return QtVSIPSettingsShared.GetOption(Resources.lreleaseOptionsKeyword);
        }

        public static string GetLReleaseOptions(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string qtLReleaseOptions = QtVSIPSettingsShared.GetProjectQtSetting(project, "QtLReleaseOptions");
            if (!string.IsNullOrEmpty(qtLReleaseOptions))
                return qtLReleaseOptions;
            return QtVSIPSettingsShared.GetOption(project, Resources.lreleaseOptionsKeyword);
        }

        public static void SaveLReleaseOptions(EnvDTE.Project project, string options)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (options == null)
                options = GetLReleaseOptions();

            SaveOption(project, Resources.lreleaseOptionsKeyword, options);
        }
        #endregion

        #region RCC

        public static string GetRccDirectory(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return QtVSIPSettingsShared.GetDirectory(project, Resources.rccDirKeyword);
        }

        public static void SaveRccDirectory(EnvDTE.Project project, string directory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (directory == null)
                directory = QtVSIPSettingsShared.GetDirectory(project, Resources.rccDirKeyword);
            SaveDirectory(project, Resources.rccDirKeyword, directory);
        }
        #endregion

        #region QML

        public static bool GetQmlDebug(EnvDTE.Project project)
        {
            return QtProject.Create(project).QmlDebug;
        }

        public static void SaveQmlDebug(EnvDTE.Project project, bool enabled)
        {
            QtProject.Create(project).QmlDebug = enabled;
        }
        #endregion

        private static void SaveDirectory(EnvDTE.Project project, string type, string dir)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            dir = HelperFunctions.NormalizeRelativeFilePath(dir);
            project.Globals[type] = dir;
            if (!project.Globals.get_VariablePersists(type))
                project.Globals.set_VariablePersists(type, true);

            QtVSIPSettingsShared.CleanUpCache(project);
        }

        private static void SaveOption(EnvDTE.Project project, string type, string option)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            project.Globals[type] = option;
            if (!project.Globals.get_VariablePersists(type))
                project.Globals.set_VariablePersists(type, true);
        }

        private static void SetBoolValue(EnvDTE.Project project, string type, bool value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            project.Globals[type] = Convert.ToInt32(value).ToString();
            if (!project.Globals.get_VariablePersists(type))
                project.Globals.set_VariablePersists(type, true);
        }
    }
}
