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
using System.Windows.Forms;

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

            Text = SR.GetString("VSQtOptionsButtonText");
            listView.Columns.Add(SR.GetString("BuildOptionsPage_Name"), 100,
                HorizontalAlignment.Left);
            listView.Columns.Add(SR.GetString("BuildOptionsPage_Path"), 180,
                HorizontalAlignment.Left);
            addButton.Text = SR.GetString(SR.Add);
            deleteButton.Text = SR.GetString(SR.Delete);
            label2.Text = SR.GetString("BuildOptionsPage_DefaultQtVersion");
            okButton.Text = SR.GetString("OK");
            cancelButton.Text = SR.GetString("Cancel");
            tabControl1.TabPages[0].Text = SR.GetString("BuildOptionsPage_Title");
            tabControl1.TabPages[1].Text = SR.GetString("QtDefaultSettings");

            SetupDefaultVersionComboBox(null);
            UpdateListBox();
            FormBorderStyle = FormBorderStyle.FixedDialog;

            vsQtSettings = new VSQtSettings();
            optionsPropertyGrid.SelectedObject = vsQtSettings;

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

        void FormQtVersions_Shown(object sender, System.EventArgs args)
        {
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
            foreach (string version in versionManager.GetVersions()) {
                string path = null;
                if (defaultQtVersionDir != null && version == "$(DefaultQtVersion)")
                    path = defaultQtVersionDir;
                else
                    path = versionManager.GetInstallPath(version);
                if (path == null && version != "$(QTDIR)")
                    continue;
                var itm = new ListViewItem();
                itm.Tag = version;
                itm.Text = version;
                itm.SubItems.Add(path);
                listView.Items.Add(itm);
            }
        }

        private void SetupDefaultVersionComboBox(string version)
        {
            string currentItem = defaultCombo.Text;
            if (version != null)
                currentItem = version;
            defaultCombo.Items.Clear();

            foreach (string v in versionManager.GetVersions()) {
                if (v == "$(DefaultQtVersion)")
                    continue;
                defaultCombo.Items.Add(v);
            }

            if (defaultCombo.Items.Count > 0) {
                if (defaultCombo.Items.Contains(currentItem))
                    defaultCombo.Text = currentItem;
                else
                    defaultCombo.Text = (string) defaultCombo.Items[0];
            } else {
                defaultCombo.Text = "";
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            QtVersionManager.The().ClearVersionCache();
            foreach (ListViewItem itm in listView.SelectedItems) {
                string name = itm.Text;
                versionManager.RemoveVersion(name);
                listView.Items.Remove(itm);
                SetupDefaultVersionComboBox(null);
            }
        }

        private void addButton_Click(object sender, EventArgs e)
        {
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
    }
}
