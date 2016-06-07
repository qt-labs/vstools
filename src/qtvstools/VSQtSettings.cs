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
    public class VSQtSettings
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
        }

        private string newMocDir = null;
        private string newMocOptions = null;
        private string newRccDir = null;
        private string newUicDir = null;
        private bool newLUpdateOnBuild = false;
        private string newLUpdateOptions = null;
        private string newLReleaseOptions = null;
        private bool newAskBeforeCheckoutFile = true;
        private bool newDisableCheckoutFiles = true;
        private bool newDisableAutoMOCStepsUpdate = false;

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
                if (tmp.ToLower() == newMocDir.ToLower())
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
                if (tmp.ToLower() == newUicDir.ToLower())
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
                if (tmp.ToLower() == newRccDir.ToLower())
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

