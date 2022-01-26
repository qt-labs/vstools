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

using QtVsTools.Core;
using System;
using System.ComponentModel;
using System.Globalization;

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
            QmlDebug = oldQmlDebug = QtVSIPSettings.GetQmlDebug(project);
        }

        private QtVersionManager versionManager;
        private EnvDTE.Project project;

        private string oldMocDir;
        private string oldMocOptions;
        private string oldRccDir;
        private string oldUicDir;
        private string oldQtVersion;
        private bool oldLUpdateOnBuild;
        private string oldLUpdateOptions;
        private string oldLReleaseOptions;
        private bool oldQmlDebug;

        private string newMocDir;
        private string newMocOptions;
        private string newRccDir;
        private string newUicDir;
        private string newQtVersion;
        private bool newLUpdateOnBuild;
        private string newLUpdateOptions;
        private string newLReleaseOptions;

        public void SaveSettings()
        {
            var updateMoc = false;
            var qtPro = QtProject.Create(project);

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

            if (oldQmlDebug != QmlDebug)
                QtVSIPSettings.SaveQmlDebug(project, QmlDebug);

            if (oldQtVersion != newQtVersion) {
                if (qtPro.PromptChangeQtVersion(oldQtVersion, newQtVersion)) {
                    var newProjectCreated = false;
                    var versionChanged = qtPro.ChangeQtVersion(
                        oldQtVersion, newQtVersion, ref newProjectCreated);
                    if (versionChanged && newProjectCreated)
                        project = qtPro.Project;
                }
            }
        }

        [DisplayName("QML Debug")]
        [TypeConverter(typeof(QmlDebugConverter))]
        public bool QmlDebug { get; set; }

        internal class QmlDebugConverter : BooleanConverter
        {
            public override object ConvertTo(
                ITypeDescriptorContext context,
                CultureInfo culture,
                object value,
                Type destinationType)
            {
                return (bool)value ? "Enabled" : "Disabled";
            }

            public override object ConvertFrom(
                ITypeDescriptorContext context,
                CultureInfo culture,
                object value)
            {
                return (string)value == "Enabled";
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
                var versions = versionManager.GetVersions();
                Array.Resize(ref versions, versions.Length + 1);
                versions[versions.Length - 1] = "$(DefaultQtVersion)";
                return new StandardValuesCollection(versions);
            }

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                return true;
            }
        }
    }
}
