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
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace QtVsTools
{
    public partial class FormVSQtSettings : Form
    {
        private QtVersionManager versionManager;
        private VSQtSettings vsQtSettings;

        public FormVSQtSettings()
        {
            InitializeComponent();
            versionManager = QtVersionManager.The();

            columnVersionName.Text = SR.GetString("BuildOptionsPage_Name");
            columnVersionPath.Text = SR.GetString("BuildOptionsPage_Path");
            addButton.Text = SR.GetString("Add");
            deleteButton.Text = SR.GetString("Delete");
            label2.Text = SR.GetString("BuildOptionsPage_DefaultQtVersion");
            okButton.Text = SR.GetString("OK");
            cancelButton.Text = SR.GetString("Cancel");
            tabControl1.TabPages[0].Text = SR.GetString("BuildOptionsPage_Title");
            tabControl1.TabPages[1].Text = SR.GetString("QtDefaultSettings");

            SetupDefaultVersionComboBox(null);
            UpdateListBox();

            vsQtSettings = new VSQtSettings();
            optionsPropertyGrid.SelectedObject = vsQtSettings;
            vsQtSettings.PropertyChanged += OnSettingsChanged;

            KeyPress += FormQtVersions_KeyPress;
            Shown += FormQtVersions_Shown;
        }

        void FormQtVersions_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27) {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        void FormQtVersions_Shown(object sender, EventArgs args)
        {
            Text = SR.GetString("VSQtOptionsButtonText");
            string error = null;
            if (versionManager.HasInvalidVersions(out error))
                Messages.DisplayErrorMessage(error);
        }

        public void LoadSettings()
        {
            SetupDefaultVersionComboBox(versionManager.GetDefaultVersion());
        }

        public void SaveSettings()
        {
            versionManager.SaveDefaultVersion(defaultCombo.Text);
        }

        private void UpdateListBox()
        {
            UpdateListBox(null);
        }

        private void UpdateListBox(string defaultQtVersionDir)
        {
            listView.Items.Clear();
            foreach (var version in QtVersionManager.The().GetVersions()) {
                string path = null;
                string compiler = "msvc";
                if (defaultQtVersionDir != null && version == "$(DefaultQtVersion)")
                    path = defaultQtVersionDir;
                else
                    path = versionManager.GetInstallPath(version);
                if (path == null && version != "$(QTDIR)")
                    continue;
                if (path.StartsWith("SSH:") || path.StartsWith("WSL:")) {
                    var linuxPaths = path.Split(':');
                    compiler = "g++";
                    path = string.Format("[{0}] {1}", linuxPaths[0], linuxPaths[1]);
                    if (linuxPaths.Length > 2 && !string.IsNullOrEmpty(linuxPaths[2]))
                        compiler = linuxPaths[2];
                }
                var itm = new ListViewItem();
                itm.Tag = version;
                itm.Text = version;
                itm.SubItems.Add(path);
                itm.SubItems.Add(compiler);
                listView.Items.Add(itm);
            }
        }

        private void SetupDefaultVersionComboBox(string version)
        {
            var currentItem = defaultCombo.Text;
            if (version != null)
                currentItem = version;
            defaultCombo.Items.Clear();

            foreach (var v in QtVersionManager.The().GetVersions()) {
                if (v == "$(DefaultQtVersion)")
                    continue;
                try {
                    Path.GetFullPath(QtVersionManager.The().GetInstallPath(v));
                } catch {
                    continue;
                }
                defaultCombo.Items.Add(v);
            }

            if (defaultCombo.Items.Count > 0) {
                if (defaultCombo.Items.Contains(currentItem))
                    defaultCombo.Text = currentItem;
                else
                    defaultCombo.Text = (string) defaultCombo.Items[0];
            } else {
                defaultCombo.Text = string.Empty;
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            VersionInformation.Clear();
            QtVersionManager.The().ClearVersionCache();
            foreach (ListViewItem itm in listView.SelectedItems) {
                var name = itm.Text;
                versionManager.RemoveVersion(name);
                listView.Items.Remove(itm);
                SetupDefaultVersionComboBox(null);
            }
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            VersionInformation.Clear();
            QtVersionManager.The().ClearVersionCache();
            using (var dia = new AddQtVersionDialog()) {
                dia.StartPosition = FormStartPosition.CenterParent;
                var ww = new MainWinWrapper(Vsix.Instance.Dte);
                if (dia.ShowDialog(ww) == DialogResult.OK) {
                    UpdateListBox();
                    SetupDefaultVersionComboBox(null);
                }
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            vsQtSettings.SaveSettings();
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender == null)
                return;

            if (e.PropertyName == "EnableQmlTextMate") {
                var qttmlanguage = Environment
                    .ExpandEnvironmentVariables("%USERPROFILE%\\.vs\\Extensions\\qttmlanguage");
                if (Observable.GetPropertyValue<bool>(sender, e.PropertyName)) {
                    var assembly = Assembly.GetExecutingAssembly().Location;
                    HelperFunctions.CopyDirectory(Path.Combine(Path.GetDirectoryName(assembly),
                        "qttmlanguage"), qttmlanguage);
                } else {
                    Directory.Delete(qttmlanguage, true);
                }
            }
        }

        private void listView_DoubleClick(object sender, EventArgs e)
        {
            if (listView.SelectedItems == null)
                return;
            if (listView.SelectedItems.Count == 0)
                return;

            VersionInformation.Clear();
            QtVersionManager.The().ClearVersionCache();
            using (var dia = new AddQtVersionDialog()) {
                dia.SetEdit(listView.SelectedItems[0].Text);
                dia.StartPosition = FormStartPosition.CenterParent;
                var ww = new MainWinWrapper(Vsix.Instance.Dte);
                if (dia.ShowDialog(ww) == DialogResult.OK) {
                    UpdateListBox();
                    SetupDefaultVersionComboBox(null);
                }
            }
        }
    }
}
