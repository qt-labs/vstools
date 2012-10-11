/****************************************************************************
**
** Copyright (C) 2012 Digia Plc and/or its subsidiary(-ies).
** Contact: http://www.qt-project.org/legal
**
** This file is part of the Qt VS Add-in.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia. For licensing terms and
** conditions see http://qt.digia.com/licensing. For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file. Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights. These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file. Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.Windows.Forms;
using System.Collections.Generic;
using EnvDTE;


using Digia.Qt5ProjectLib;
namespace Qt5VSAddin
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

            threeDLib.Text = SR.GetString("3DLibrary");
            locationLib.Text = SR.GetString("LocationLibrary");
            qmlLib.Text = SR.GetString("QmlLibrary");
            quickLib.Text = SR.GetString("QuickLibrary");
            bluetoothLib.Text = SR.GetString("BluetoothLibrary");
            contactsLib.Text = SR.GetString("ContactsLibrary");
            organizerLib.Text = SR.GetString("OrganizerLibrary");
            printSupportLib.Text = SR.GetString("PrintSupportLibrary");
            pubSubLib.Text = SR.GetString("PubSubLibrary");
            quick1Lib.Text = SR.GetString("Quick1Library");
            sensorsLib.Text = SR.GetString("SensorsLibrary");
            serviceFrameworkLib.Text = SR.GetString("ServiceFwLibrary");
            systemInfoLib.Text = SR.GetString("SystemInfoLibrary");
            webkitWidgetsLib.Text = SR.GetString("WebkitWidgetsLibrary");
            versitLib.Text = SR.GetString("VersitLibrary");
            widgetsLib.Text = SR.GetString("WidgetsLibrary");

            concurrentLib.Text = SR.GetString("ConcurrentLibrary");
            multimediaWidgetsLib.Text = SR.GetString("MultimediaWidgetsLibrary");

            // essentials
            AddMapping(threeDLib, QtModule.ThreeD);
            AddMapping(coreLib, QtModule.Core);
            AddMapping(guiLib, QtModule.Gui);
            AddMapping(locationLib, QtModule.Location);
            AddMapping(multimediaLib, QtModule.Multimedia);
            AddMapping(networkLib, QtModule.Network);
            AddMapping(qmlLib, QtModule.Qml);
            AddMapping(quickLib, QtModule.Quick);
            AddMapping(sqlLib, QtModule.Sql);
            AddMapping(testLib, QtModule.Test);
            AddMapping(webKitLib, QtModule.WebKit);

            // add-ons
            AddMapping(activeQtCLib, QtModule.ActiveQtC);
            AddMapping(activeQtSLib, QtModule.ActiveQtS);
            AddMapping(bluetoothLib, QtModule.Bluetooth);
            AddMapping(contactsLib, QtModule.Contacts);
            AddMapping(helpLib, QtModule.Help);
            AddMapping(openGLLib, QtModule.OpenGL);
            AddMapping(organizerLib, QtModule.Organizer);
            AddMapping(phononLib, QtModule.Phonon);
            AddMapping(printSupportLib, QtModule.PrintSupport);
            AddMapping(pubSubLib, QtModule.PublishSubscribe);
            AddMapping(quick1Lib, QtModule.Quick1);
            AddMapping(scriptLib, QtModule.Script);
            AddMapping(sensorsLib, QtModule.Sensors);
            AddMapping(serviceFrameworkLib, QtModule.ServiceFramework);
            AddMapping(svgLib, QtModule.Svg);
            AddMapping(systemInfoLib, QtModule.SystemInfo);
            AddMapping(webkitWidgetsLib, QtModule.WebkitWidgets);
            AddMapping(versitLib, QtModule.Versit);
            AddMapping(widgetsLib, QtModule.Widgets);
            AddMapping(xmlLib, QtModule.Xml);
            AddMapping(xmlPatternsLib, QtModule.XmlPatterns);

            AddMapping(concurrentLib, QtModule.Concurrent);
            AddMapping(multimediaWidgetsLib, QtModule.MultimediaWidgets);

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
            QtVersionManager versionManager = QtVersionManager.The();
            string qtVersion = qtProject.GetQtVersion();
            string install_path = versionManager.GetInstallPath(qtVersion);

            for (int i = 0; i < moduleMap.Count; ++i)
            {
                ModuleMapItem item = moduleMap[i];
                item.initialValue = qtProject.HasModule(item.moduleId);
                item.checkbox.Checked = item.initialValue;
                moduleMap[i] = item;

                // Disable if module not installed
                QtModuleInfo info = QtModules.Instance.ModuleInformation(item.moduleId);
                string full_path = install_path + "\\lib\\" + info.LibraryPrefix;
                if (!info.LibraryPrefix.StartsWith("QAx"))
                {
                    full_path += "5.lib";
                }
                else
                {
                    full_path += ".lib";
                }
                System.IO.FileInfo fi = new System.IO.FileInfo(full_path);
                item.checkbox.Enabled = fi.Exists;
                if (fi.Exists == false)
                {
                    item.checkbox.Checked = false;
                }
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
