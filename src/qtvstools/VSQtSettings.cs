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

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using QtProjectLib;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace QtVsTools
{
    public class VSQtSettings : Observable
    {
        public VSQtSettings()
        {
            newMocDir = QtVSIPSettings.GetMocDirectory();
            newMocOptions = QtVSIPSettings.GetMocOptions();
            newRccDir = QtVSIPSettings.GetRccDirectory();
            newUicDir = QtVSIPSettings.GetUicDirectory();
            newLUpdateOnBuild = QtVSIPSettings.GetLUpdateOnBuild();
            newLUpdateOptions = QtVSIPSettings.GetLUpdateOptions();
            newLReleaseOptions = QtVSIPSettings.GetLReleaseOptions();
            newAskBeforeCheckoutFile = QtVSIPSettings.GetAskBeforeCheckoutFile();
            newDisableCheckoutFiles = QtVSIPSettings.GetDisableCheckoutFiles();
            newDisableAutoMOCStepsUpdate = QtVSIPSettings.GetDisableAutoMocStepsUpdate();

            var settingsManager = new ShellSettingsManager(Vsix.Instance);
            var store = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

#if VS2013
            EnableQmlClassifier = store.GetBoolean(Statics.QmlClassifierPath,
                Statics.QmlClassifierKey, true);
#else
            EnableQmlTextMate = store.GetBoolean(Statics.QmlTextMatePath,
                Statics.QmlTextMateKey, true);
#endif
        }

        private string newMocDir;
        private string newMocOptions;
        private string newRccDir;
        private string newUicDir;
        private bool newLUpdateOnBuild;
        private string newLUpdateOptions;
        private string newLReleaseOptions;
        private bool newAskBeforeCheckoutFile = true;
        private bool newDisableCheckoutFiles = true;
        private bool newDisableAutoMOCStepsUpdate;

        public void SaveSettings()
        {
            QtVSIPSettings.SaveMocDirectory(newMocDir);
            QtVSIPSettings.SaveMocOptions(newMocOptions);
            QtVSIPSettings.SaveUicDirectory(newUicDir);
            QtVSIPSettings.SaveRccDirectory(newRccDir);
            QtVSIPSettings.SaveLUpdateOnBuild(newLUpdateOnBuild);
            QtVSIPSettings.SaveLUpdateOptions(newLUpdateOptions);
            QtVSIPSettings.SaveLReleaseOptions(newLReleaseOptions);
            QtVSIPSettings.SaveAskBeforeCheckoutFile(newAskBeforeCheckoutFile);
            QtVSIPSettings.SaveDisableCheckoutFiles(newDisableCheckoutFiles);
            QtVSIPSettings.SaveDisableAutoMocStepsUpdate(newDisableAutoMOCStepsUpdate);

            var settingsManager = new ShellSettingsManager(Vsix.Instance);
            var store = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

#if VS2013
            store.CreateCollection(Statics.QmlClassifierPath);
            store.SetBoolean(Statics.QmlClassifierPath, Statics.QmlClassifierKey,
                EnableQmlClassifier);
#else
            store.CreateCollection(Statics.QmlTextMatePath);
            store.SetBoolean(Statics.QmlTextMatePath, Statics.QmlTextMateKey, EnableQmlTextMate);
#endif
        }

        public string MocDirectory
        {
            get
            {
                return newMocDir;
            }
            set
            {
                var tmp = HelperFunctions.NormalizeRelativeFilePath(value);
                if (tmp.ToLower() == newMocDir.ToLower())
                    return;

                string badMacros = ProjectQtSettings.IncompatibleMacros(tmp);
                if (!string.IsNullOrEmpty(badMacros))
                    Messages.DisplayErrorMessage(SR.GetString("IncompatibleMacros", badMacros));
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
                var tmp = HelperFunctions.NormalizeRelativeFilePath(value);
                if (tmp.ToLower() == newUicDir.ToLower())
                    return;

                string badMacros = ProjectQtSettings.IncompatibleMacros(tmp);
                if (!string.IsNullOrEmpty(badMacros))
                    Messages.DisplayErrorMessage(SR.GetString("IncompatibleMacros", badMacros));
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
                var tmp = HelperFunctions.NormalizeRelativeFilePath(value);
                if (tmp.ToLower() == newRccDir.ToLower())
                    return;

                string badMacros = ProjectQtSettings.IncompatibleMacros(tmp);
                if (!string.IsNullOrEmpty(badMacros))
                    Messages.DisplayErrorMessage(SR.GetString("IncompatibleMacros", badMacros));
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

        [DisplayName("Ask before checkout files")]
        public bool ask_before_checkout_file
        {
            get
            {
                return newAskBeforeCheckoutFile;
            }
            set
            {
                newAskBeforeCheckoutFile = value;
            }
        }

        [DisplayName("Disable checkout files")]
        public bool disable_checkout_files
        {
            get
            {
                return newDisableCheckoutFiles;
            }

            set
            {
                newDisableCheckoutFiles = value;
            }
        }

        [DisplayName("Disable auto MOC steps update")]
        public bool disable_auto_MOC_steps_update
        {
            get
            {
                return newDisableAutoMOCStepsUpdate;
            }

            set
            {
                newDisableAutoMOCStepsUpdate = value;
            }
        }

#if VS2013
        [DisplayName("Use QML classifier")]
        public bool EnableQmlClassifier { get; set; }
#else
        private bool _enableQmlTextMate = true;
        [DisplayName("Use QML TextMate language file")]
        public bool EnableQmlTextMate
        {
            get { return _enableQmlTextMate; }
            set { SetValue(ref _enableQmlTextMate, value); }
        }
#endif

        private static bool ContainsInvalidVariable(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                return false;

            var matches = Regex.Matches(directory, "\\$\\([^\\)]+\\)");
            foreach (var m in matches) {
                if (m.ToString() != "$(ConfigurationName)" && m.ToString() != "$(PlatformName)")
                    return true;
            }
            return false;
        }
    }
}

