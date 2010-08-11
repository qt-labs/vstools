/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2010 Nokia Corporation and/or its subsidiary(-ies).
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
using System.Windows.Forms;
using System.Collections.Generic;
using EnvDTE;
using Nokia.QtProjectLib;

namespace Qt4VSAddin
{
    public partial class FormProjectQtSettings : Form
    {
        private Project project;
        private QtProject qtProject;
        private ProjectQtSettings qtSettings = null;

        private struct ModuleMapItem
        {
            public CheckBox checkbox;
            public QtModule moduleId;
            public bool initialValue;

            public ModuleMapItem(CheckBox cb, QtModule mid)
            {
                checkbox = cb;
                moduleId = mid;
                initialValue = false;
            }
        }

        private List<ModuleMapItem> moduleMap = new List<ModuleMapItem>();

        public FormProjectQtSettings()
        {
            InitializeComponent();
            okButton.Text = SR.GetString("OK");
            cancelButton.Text = SR.GetString("Cancel");
            tabControl1.TabPages[0].Text = this.Text = SR.GetString("ActionDialog_Properties");
            tabControl1.TabPages[1].Text = this.Text = SR.GetString("QtModules");
            this.activeQtCLib.Text = SR.GetString("ActiveQtContainerLibrary");
            this.activeQtSLib.Text = SR.GetString("ActiveQtServerLibrary");
            this.testLib.Text = SR.GetString("TestLibrary");
            this.svgLib.Text = SR.GetString("SVGLibrary");
            this.xmlLib.Text = SR.GetString("XMLLibrary");
            this.qt3Lib.Text = SR.GetString("Qt3SupportLibrary");
            this.networkLib.Text = SR.GetString("NetworkLibrary");
            this.openGLLib.Text = SR.GetString("OpenGLLibrary");
            this.sqlLib.Text = SR.GetString("SQLLibrary");
            this.guiLib.Text = SR.GetString("GUILibrary");
            this.multimediaLib.Text = SR.GetString("MultimediaLibrary");
            this.coreLib.Text = SR.GetString("CoreLibrary");
            this.Text = SR.GetString("ProjectQtSettingsButtonText");
            this.scriptLib.Text = SR.GetString("ScriptLibrary");
            this.helpLib.Text = SR.GetString("HelpLibrary");
            this.webKitLib.Text = SR.GetString("WebKitLibrary");
            this.xmlPatternsLib.Text = SR.GetString("XmlPatternsLibrary");
            this.phononLib.Text = SR.GetString("PhononLibrary");

            AddMapping(coreLib, QtModule.Core);
            AddMapping(guiLib, QtModule.Gui);
            AddMapping(multimediaLib, QtModule.Multimedia);
            AddMapping(sqlLib, QtModule.Sql);
            AddMapping(openGLLib, QtModule.OpenGL);
            AddMapping(networkLib, QtModule.Network);
            AddMapping(qt3Lib, QtModule.Compat);
            AddMapping(xmlLib, QtModule.Xml);
            AddMapping(svgLib, QtModule.Svg);
            AddMapping(testLib, QtModule.Test);
            AddMapping(scriptLib, QtModule.Script);
            AddMapping(helpLib, QtModule.Help);
            AddMapping(webKitLib, QtModule.WebKit);
            AddMapping(xmlPatternsLib, QtModule.XmlPatterns);
            AddMapping(activeQtSLib, QtModule.ActiveQtS);
            AddMapping(activeQtCLib, QtModule.ActiveQtC);
            AddMapping(phononLib, QtModule.Phonon);

            FormBorderStyle = FormBorderStyle.FixedDialog;
            this.KeyPress += new KeyPressEventHandler(this.FormProjectQtSettings_KeyPress);
        }

        private void AddMapping(CheckBox checkbox, QtModule moduleId)
        {
            moduleMap.Add(new ModuleMapItem(checkbox, moduleId));
        }

        public void SetProject(Project pro)
        {
            project = pro;
            qtProject = QtProject.Create(project);
            InitModules();
            qtSettings = new ProjectQtSettings(project);
            OptionsPropertyGrid.SelectedObject = qtSettings;
        }

        private void FormProjectQtSettings_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            qtSettings.SaveSettings();
            saveModules();
            this.okButton.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void InitModules()
        {
            for (int i=0; i < moduleMap.Count; ++i)
            {
                ModuleMapItem item = moduleMap[i];
                item.initialValue = qtProject.HasModule(item.moduleId);
                item.checkbox.Checked = item.initialValue;
                moduleMap[i] = item;
            }
        }

        private void saveModules()
        {
            qtProject = QtProject.Create(project);
            for (int i = 0; i < moduleMap.Count; ++i)
            {
                ModuleMapItem item = moduleMap[i];
                bool isModuleChecked = item.checkbox.Checked;
                if (isModuleChecked != item.initialValue)
                {
                    if (isModuleChecked)
                        qtProject.AddModule(item.moduleId);
                    else
                        qtProject.RemoveModule(item.moduleId);
                }
            }
        }
    }
}
