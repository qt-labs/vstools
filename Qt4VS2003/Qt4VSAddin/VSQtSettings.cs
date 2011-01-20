/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2011 Nokia Corporation and/or its subsidiary(-ies).
**
** Contact: Nokia Corporation (qt-info@nokia.com)
**
** Commercial Usage
**
** Licensees holding valid Qt Commercial licenses may use this file in
** accordance with the Qt Commercial License Agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Nokia.
**
** GNU Lesser General Public License Usage
**
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** If you are unsure which license is appropriate for your use, please
** contact the sales department at http://qt.nokia.com/contact.
**
**************************************************************************/

using System;
using Nokia.QtProjectLib;

namespace Qt4VSAddin
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
            newAskBeforeCheckoutFile = QtVSIPSettings.GetAskBeforeCheckoutFile();
            newDisableCheckoutFiles = QtVSIPSettings.GetDisableCheckoutFiles();
            newDisableAutoMOCStepsUpdate = QtVSIPSettings.GetDisableAutoMocStepsUpdate();
        }

        private string newMocDir = null;
        private string newMocOptions = null;
        private string newRccDir = null;
        private string newUicDir = null;
        private bool newLUpdateOnBuild = false;
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
            for (int i = 0; i < matchList.Count; i++)
            {
                if (matchList[i].ToString() != "$(ConfigurationName)"
                    && matchList[i].ToString() != "$(PlatformName)")
                    return true;
            }
            return false;
        }
    }
}

