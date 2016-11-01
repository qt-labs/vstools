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

using EnvDTE;
using QtProjectLib;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace QtVsTools
{
    public partial class FormProjectQtSettings : Form
    {
        private Project project;
        private QtProject qtProject;
        private ProjectQtSettings qtSettings;

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
            tabControl1.TabPages[0].Text = Text = SR.GetString("ActionDialog_Properties");
            tabControl1.TabPages[1].Text = Text = SR.GetString("QtModules");
            activeQtCLib.Text = SR.GetString("ActiveQtContainerLibrary");
            activeQtSLib.Text = SR.GetString("ActiveQtServerLibrary");
            testLib.Text = SR.GetString("TestLibrary");
            svgLib.Text = SR.GetString("SVGLibrary");
            xmlLib.Text = SR.GetString("XMLLibrary");
            networkLib.Text = SR.GetString("NetworkLibrary");
            openGLLib.Text = SR.GetString("OpenGLLibrary");
            sqlLib.Text = SR.GetString("SQLLibrary");
            guiLib.Text = SR.GetString("GUILibrary");
            multimediaLib.Text = SR.GetString("MultimediaLibrary");
            coreLib.Text = SR.GetString("CoreLibrary");
            Text = SR.GetString("ProjectQtSettingsButtonText");
            scriptLib.Text = SR.GetString("ScriptLibrary");
            helpLib.Text = SR.GetString("HelpLibrary");
            webKitLib.Text = SR.GetString("WebKitLibrary");
            xmlPatternsLib.Text = SR.GetString("XmlPatternsLibrary");
            scriptToolsLib.Text = SR.GetString("ScriptToolsLibrary");
            uiToolsLib.Text = SR.GetString("UiToolsLibrary");

            threeDLib.Text = SR.GetString("ThreeDLibrary");
            locationLib.Text = SR.GetString("LocationLibrary");
            qmlLib.Text = SR.GetString("QmlLibrary");
            quickLib.Text = SR.GetString("QuickLibrary");
            bluetoothLib.Text = SR.GetString("BluetoothLibrary");
            printSupportLib.Text = SR.GetString("PrintSupportLibrary");
            declarativeLib.Text = SR.GetString("DeclarativeLibrary");
            sensorsLib.Text = SR.GetString("SensorsLibrary");
            webkitWidgetsLib.Text = SR.GetString("WebkitWidgetsLibrary");
            widgetsLib.Text = SR.GetString("WidgetsLibrary");

            concurrentLib.Text = SR.GetString("ConcurrentLibrary");
            multimediaWidgetsLib.Text = SR.GetString("MultimediaWidgetsLibrary");

            enginioLib.Text = SR.GetString("EnginioLibrary");
            nfcLib.Text = SR.GetString("NfcLibrary");
            positioningLib.Text = SR.GetString("PositioningLibrary");
            serialPortLib.Text = SR.GetString("SerialPortLibrary");
            webChannelLib.Text = SR.GetString("WebChannelLibrary");
            webSocketsLib.Text = SR.GetString("WebSocketsLibrary");
            windowsExtrasLib.Text = SR.GetString("WindowsExtrasLibrary");
            quickWidgetsLib.Text = SR.GetString("QuickWidgetsLibrary");

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
            AddMapping(helpLib, QtModule.Help);
            AddMapping(openGLLib, QtModule.OpenGL);
            AddMapping(scriptToolsLib, QtModule.ScriptTools);
            AddMapping(uiToolsLib, QtModule.UiTools);
            AddMapping(printSupportLib, QtModule.PrintSupport);
            AddMapping(declarativeLib, QtModule.Declarative);
            AddMapping(scriptLib, QtModule.Script);
            AddMapping(sensorsLib, QtModule.Sensors);
            AddMapping(svgLib, QtModule.Svg);
            AddMapping(webkitWidgetsLib, QtModule.WebkitWidgets);
            AddMapping(widgetsLib, QtModule.Widgets);
            AddMapping(xmlLib, QtModule.Xml);
            AddMapping(xmlPatternsLib, QtModule.XmlPatterns);

            AddMapping(concurrentLib, QtModule.Concurrent);
            AddMapping(multimediaWidgetsLib, QtModule.MultimediaWidgets);

            AddMapping(enginioLib, QtModule.Enginio);
            AddMapping(nfcLib, QtModule.Nfc);
            AddMapping(positioningLib, QtModule.Positioning);
            AddMapping(serialPortLib, QtModule.SerialPort);
            AddMapping(webChannelLib, QtModule.WebChannel);
            AddMapping(webSocketsLib, QtModule.WebSockets);
            AddMapping(windowsExtrasLib, QtModule.WindowsExtras);
            AddMapping(quickWidgetsLib, QtModule.QuickWidgets);

            FormBorderStyle = FormBorderStyle.FixedDialog;
            KeyPress += FormProjectQtSettings_KeyPress;
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
            if (e.KeyChar == 27) {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            // Disable the buttons since some operations are quite expensive (e.g. changing
            // the Qt version) and take some to finish. Keeping the buttons enables allows to hit
            // the buttons several times resulting in successive executions of these operations.
            okButton.Enabled = false;
            cancelButton.Enabled = false;

            qtSettings.SaveSettings();
            saveModules();
            okButton.DialogResult = DialogResult.OK;
            Close();
        }

        private void InitModules()
        {
            var versionManager = QtVersionManager.The();
            var qtVersion = qtProject.GetQtVersion();
            var install_path = versionManager.GetInstallPath(qtVersion) ?? string.Empty;

            for (var i = 0; i < moduleMap.Count; ++i) {
                ModuleMapItem item = moduleMap[i];
                item.initialValue = qtProject.HasModule(item.moduleId);
                item.checkbox.Checked = item.initialValue;
                moduleMap[i] = item;

                // Disable if module not installed
                var info = QtModules.Instance.ModuleInformation(item.moduleId);
                var libraryPrefix = info.LibraryPrefix;
                if (libraryPrefix.StartsWith("Qt", StringComparison.Ordinal)) {
                    libraryPrefix = "Qt5" + libraryPrefix.Substring(2);
                }
                var full_path = install_path + "\\lib\\" + libraryPrefix + ".lib";
                var fi = new System.IO.FileInfo(full_path);
                item.checkbox.Enabled = fi.Exists;
                if (fi.Exists == false) {
                    // Don't disable item if qtVersion not available
                    if (qtVersion != null)
                        item.checkbox.Checked = false;
                }
            }
        }

        private void saveModules()
        {
            qtProject = QtProject.Create(project);
            for (var i = 0; i < moduleMap.Count; ++i) {
                ModuleMapItem item = moduleMap[i];
                var isModuleChecked = item.checkbox.Checked;
                if (isModuleChecked != item.initialValue) {
                    if (isModuleChecked)
                        qtProject.AddModule(item.moduleId);
                    else
                        qtProject.RemoveModule(item.moduleId);
                }
            }
        }

    }
}
