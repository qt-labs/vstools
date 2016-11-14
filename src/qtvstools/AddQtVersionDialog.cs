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
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace QtVsTools
{
    public class AddQtVersionDialog : Form
    {
        private Label label1;
        private Label label2;
        private Button okButton;
        private Button cancelButton;
        private TextBox nameBox;
        private TextBox pathBox;
        private Button browseButton;
        private bool nameBoxDirty;
        private Label errorLabel;

        public AddQtVersionDialog()
        {
            InitializeComponent();

            label1.Text = SR.GetString("AddQtVersionDialog_VersionName");
            label2.Text = SR.GetString("AddQtVersionDialog_Path");
            okButton.Text = SR.GetString(SR.OK);
            cancelButton.Text = SR.GetString(SR.Cancel);

            okButton.Click += okButton_Click;
            nameBox.TextChanged += DataChanged;
            pathBox.TextChanged += DataChanged;
            browseButton.Click += browseButton_Click;

            Shown += AddQtVersionDialog_Shown;
            KeyPress += AddQtVersionDialog_KeyPress;
        }

        private void AddQtVersionDialog_Shown(object sender, EventArgs e)
        {
            Text = SR.GetString("AddQtVersionDialog_Title");
        }

        void AddQtVersionDialog_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != (char) Keys.Escape)
                return;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            okButton = new System.Windows.Forms.Button();
            cancelButton = new System.Windows.Forms.Button();
            nameBox = new System.Windows.Forms.TextBox();
            pathBox = new System.Windows.Forms.TextBox();
            browseButton = new System.Windows.Forms.Button();
            errorLabel = new System.Windows.Forms.Label();
            SuspendLayout();
            //
            // label1
            //
            label1.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            label1.Location = new System.Drawing.Point(8, 16);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(88, 24);
            label1.TabIndex = 6;
            label1.Text = "Version name:";
            //
            // label2
            //
            label2.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            label2.Location = new System.Drawing.Point(8, 48);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(88, 23);
            label2.TabIndex = 5;
            label2.Text = "Path:";
            //
            // okButton
            //
            okButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            okButton.Enabled = false;
            okButton.Location = new System.Drawing.Point(143, 104);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(75, 23);
            okButton.TabIndex = 3;
            okButton.Text = "&OK";
            //
            // cancelButton
            //
            cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(224, 104);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(75, 23);
            cancelButton.TabIndex = 4;
            cancelButton.Text = "&Cancel";
            //
            // nameBox
            //
            nameBox.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            nameBox.Location = new System.Drawing.Point(96, 16);
            nameBox.Name = "nameBox";
            nameBox.Size = new System.Drawing.Size(200, 20);
            nameBox.TabIndex = 0;
            //
            // pathBox
            //
            pathBox.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            pathBox.Location = new System.Drawing.Point(96, 48);
            pathBox.Name = "pathBox";
            pathBox.Size = new System.Drawing.Size(176, 20);
            pathBox.TabIndex = 1;
            //
            // browseButton
            //
            browseButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            browseButton.Location = new System.Drawing.Point(272, 48);
            browseButton.Name = "browseButton";
            browseButton.Size = new System.Drawing.Size(24, 20);
            browseButton.TabIndex = 2;
            browseButton.Text = "...";
            //
            // errorLabel
            //
            errorLabel.AutoSize = true;
            errorLabel.ForeColor = System.Drawing.Color.Red;
            errorLabel.Location = new System.Drawing.Point(8, 71);
            errorLabel.Name = "errorLabel";
            errorLabel.Size = new System.Drawing.Size(0, 13);
            errorLabel.TabIndex = 0;
            //
            // AddQtVersionDialog
            //
            AcceptButton = okButton;
            AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(304, 134);
            Controls.Add(errorLabel);
            Controls.Add(browseButton);
            Controls.Add(pathBox);
            Controls.Add(nameBox);
            Controls.Add(cancelButton);
            Controls.Add(okButton);
            Controls.Add(label2);
            Controls.Add(label1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            KeyPreview = true;
            Name = "AddQtVersionDialog";
            SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            Text = "Add New Qt Version";
            ResumeLayout(false);
            PerformLayout();

        }
        #endregion

        private void okButton_Click(object sender, EventArgs e)
        {
            try {
                var versionInfo = new VersionInformation(pathBox.Text);
                var generator = versionInfo.GetQMakeConfEntry("MAKEFILE_GENERATOR");
                if (generator != "MSVC.NET" && generator != "MSBUILD")
                    throw new Exception(SR.GetString("AddQtVersionDialog_IncorrectMakefileGenerator", generator));
                QtVersionManager.The().SaveVersion(nameBox.Text, pathBox.Text);
                DialogResult = DialogResult.OK;
                Close();
            } catch (Exception exception) {
                Messages.DisplayErrorMessage(exception.Message);
            }
        }

        private void DataChanged(object sender, EventArgs e)
        {
            if (sender == nameBox)
                nameBoxDirty = true;

            var path = pathBox.Text;
            var name = nameBox.Text.Trim();

            if (!nameBoxDirty) {
                var str = path.TrimEnd('\\');
                name = str.Substring(str.LastIndexOf('\\') + 1);

                nameBox.TextChanged -= DataChanged;
                nameBox.Text = name;
                nameBox.TextChanged += DataChanged;
            }

            pathBox.Enabled = name != "$(QTDIR)";
            browseButton.Enabled = pathBox.Enabled;

            if (name == "$(QTDIR)") {
                path = Environment.GetEnvironmentVariable("QTDIR");
                pathBox.TextChanged -= DataChanged;
                pathBox.Text = path;
                pathBox.TextChanged += DataChanged;
            }

            errorLabel.Text = string.Empty;
            if (string.IsNullOrWhiteSpace(name)) {
                errorLabel.Text = SR.GetString("AddQtVersionDialog_InvalidName");
            } else if (string.IsNullOrWhiteSpace(path) && name == "$(QTDIR)") {
                errorLabel.Text = SR.GetString("AddQtVersionDialog_RestartVisualStudio");
            } else if (!Directory.Exists(path)) {
                errorLabel.Text = SR.GetString("AddQtVersionDialog_InvalidDirectory");
            } else if (File.Exists(Path.Combine(path, "lib", "libqtmain.a"))
                || File.Exists(Path.Combine(path, "lib", "libqtmaind.a"))) {
                errorLabel.Text = SR.GetString("AddQtVersionDialog_MingwQt");
            } else if (!File.Exists(Path.Combine(path, "bin", "qmake.exe"))) {
                errorLabel.Text = SR.GetString("AddQtVersionDialog_NotFound",
                    Path.Combine(path, "bin", "qmake.exe"));
            } else if (QtVersionManager.The().GetVersions().Any(s => name == s)) {
                errorLabel.Text = SR.GetString("AddQtVersionDialog_VersionAlreadyPresent");
            }
            okButton.Enabled = string.IsNullOrEmpty(errorLabel.Text);
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            using (var fd = new FolderBrowserDialog()) {
                var settingsManager = new ShellSettingsManager(Vsix.Instance);
                var store = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                fd.Description = SR.GetString("SelectQtPath");
                fd.SelectedPath = store.GetString(Statics.AddQtVersionDialogPath,
                    Statics.AddQtVersionDialogKey, string.Empty);
                SendKeys.Send("{TAB}{TAB}{RIGHT}");

                if (fd.ShowDialog() == DialogResult.OK) {
                    store.CreateCollection(Statics.AddQtVersionDialogPath);
                    store.SetString(Statics.AddQtVersionDialogPath, Statics.AddQtVersionDialogKey,
                        (pathBox.Text = fd.SelectedPath));
                }
            }
        }
    }
}
