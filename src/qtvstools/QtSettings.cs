/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

using QtProjectLib;
using System;
using System.ComponentModel;

namespace QtVsTools
{
    public class ProjectQtSettings
    {

        public ProjectQtSettings(EnvDTE.Project proj)
        {
            versionManager = QtVersionManager.The();
            project = proj;
            newMocDir = oldMocDir = QtVSIPSettings.GetMocDirectory(project);
            newMocOptions = oldMocOptions = QtVSIPSettings.GetMocOptions(project);
            newRccDir = oldRccDir = QtVSIPSettings.GetRccDirectory(project);
            newUicDir = oldUicDir = QtVSIPSettings.GetUicDirectory(project);
            newLUpdateOnBuild = oldLUpdateOnBuild = QtVSIPSettings.GetLUpdateOnBuild(project);
            newLUpdateOptions = oldLUpdateOptions = QtVSIPSettings.GetLUpdateOptions(project);
            newLReleaseOptions = oldLReleaseOptions = QtVSIPSettings.GetLReleaseOptions(project);
            newQtVersion = oldQtVersion = versionManager.GetProjectQtVersion(project);
        }

        private QtVersionManager versionManager;
        private EnvDTE.Project project;

        private string oldMocDir = null;
        private string oldMocOptions = null;
        private string oldRccDir = null;
        private string oldUicDir = null;
        private string oldQtVersion = null;
        private bool oldLUpdateOnBuild = false;
        private string oldLUpdateOptions = null;
        private string oldLReleaseOptions = null;

        private string newMocDir = null;
        private string newMocOptions = null;
        private string newRccDir = null;
        private string newUicDir = null;
        private string newQtVersion = null;
        private bool newLUpdateOnBuild = false;
        private string newLUpdateOptions = null;
        private string newLReleaseOptions = null;

        public void SaveSettings()
        {
            bool updateMoc = false;
            QtProject qtPro = QtProject.Create(project);

            if (oldMocDir != newMocDir) {
                QtVSIPSettings.SaveMocDirectory(project, newMocDir);
                updateMoc = true;
            }
            if (oldMocOptions != newMocOptions) {
                QtVSIPSettings.SaveMocOptions(project, newMocOptions);
                updateMoc = true;
            }
            if (updateMoc)
                qtPro.UpdateMocSteps(oldMocDir);

            if (oldUicDir != newUicDir) {
                QtVSIPSettings.SaveUicDirectory(project, newUicDir);
                qtPro.UpdateUicSteps(oldUicDir, true);
            }

            if (oldRccDir != newRccDir) {
                QtVSIPSettings.SaveRccDirectory(project, newRccDir);
                qtPro.RefreshRccSteps(oldRccDir);
            }

            if (oldLUpdateOnBuild != newLUpdateOnBuild)
                QtVSIPSettings.SaveLUpdateOnBuild(project, newLUpdateOnBuild);

            if (oldLUpdateOptions != newLUpdateOptions)
                QtVSIPSettings.SaveLUpdateOptions(project, newLUpdateOptions);

            if (oldLReleaseOptions != newLReleaseOptions)
                QtVSIPSettings.SaveLReleaseOptions(project, newLReleaseOptions);

            if (oldQtVersion != newQtVersion) {
                bool newProjectCreated = false;
                bool versionChanged = qtPro.ChangeQtVersion(oldQtVersion, newQtVersion, ref newProjectCreated);
                if (versionChanged && newProjectCreated)
                    project = qtPro.Project;
            }
        }

        public string MocDirectory
        {
            get
            {
                return newMocDir;
            }
            set
            {
                string tmp = HelperFunctions.NormalizeRelativeFilePath(value);
                if (tmp.ToLower() == oldMocDir.ToLower())
                    return;

                if (ContainsInvalidVariable(tmp))
                    Messages.DisplayErrorMessage(SR.GetString("OnlyVariableInDir"));
                else
                    newMocDir = tmp;
            }
        }

        public string MocOptions
        {
            get
            {
                return newMocOptions;
            }

            set
            {
                newMocOptions = value;
            }
        }

        public string UicDirectory
        {
            get
            {
                return newUicDir;
            }
            set
            {
                string tmp = HelperFunctions.NormalizeRelativeFilePath(value);
                if (tmp.ToLower() == oldUicDir.ToLower())
                    return;

                if (ContainsInvalidVariable(tmp))
                    Messages.DisplayErrorMessage(SR.GetString("OnlyVariableInDir"));
                else
                    newUicDir = tmp;
            }
        }

        public string RccDirectory
        {
            get
            {
                return newRccDir;
            }
            set
            {
                string tmp = HelperFunctions.NormalizeRelativeFilePath(value);
                if (tmp.ToLower() == oldRccDir.ToLower())
                    return;

                if (ContainsInvalidVariable(tmp))
                    Messages.DisplayErrorMessage(SR.GetString("OnlyVariableInDir"));
                else
                    newRccDir = tmp;
            }
        }

        public bool lupdateOnBuild
        {
            get
            {
                return newLUpdateOnBuild;
            }

            set
            {
                newLUpdateOnBuild = value;
            }
        }

        public string LUpdateOptions
        {
            get
            {
                return newLUpdateOptions;
            }

            set
            {
                newLUpdateOptions = value;
            }
        }

        public string LReleaseOptions
        {
            get
            {
                return newLReleaseOptions;
            }

            set
            {
                newLReleaseOptions = value;
            }
        }

        [TypeConverter(typeof(VersionConverter))]
        public string Version
        {
            get
            {
                return newQtVersion;
            }
            set
            {
                newQtVersion = value;
            }
        }

        internal class VersionConverter : StringConverter
        {
            private QtVersionManager versionManager;

            public VersionConverter()
            {
                versionManager = QtVersionManager.The();
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                string[] versions = versionManager.GetVersions();
                Array.Resize(ref versions, versions.Length + 1);
                versions[versions.Length - 1] = "$(DefaultQtVersion)";
                return new StandardValuesCollection(versions);
            }

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                return true;
            }
        }

        private static bool ContainsInvalidVariable(string directory)
        {
            if (String.IsNullOrEmpty(directory))
                return false;

            string pattern = "\\$\\([^\\)]+\\)";
            System.Text.RegularExpressions.Regex regExp = new System.Text.RegularExpressions.Regex(pattern);
            System.Text.RegularExpressions.MatchCollection matchList = regExp.Matches(directory);
            for (int i = 0; i < matchList.Count; i++) {
                if (matchList[i].ToString() != "$(ConfigurationName)"
                    && matchList[i].ToString() != "$(PlatformName)")
                    return true;
            }
            return false;
        }
    }
}
