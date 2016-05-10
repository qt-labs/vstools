/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Add-in.
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

using System;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;

using Digia.Qt5ProjectLib;
namespace Qt5VSAddin
{
    public class AddQtVersionDialog : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TextBox nameBox;
        private System.Windows.Forms.TextBox pathBox;
        private System.Windows.Forms.Button browseButton;
        private bool nameBoxDirty = false;
        private Timer errorTimer;
        private Label errorLabel;
        private string lastErrorString = "";

        private System.ComponentModel.Container components = null;

        public AddQtVersionDialog()
        {
            InitializeComponent();

            this.nameBox.TabIndex = 0;
            this.pathBox.TabIndex = 1;
            this.browseButton.TabIndex = 2;

            this.label1.Text = SR.GetString("AddQtVersionDialog_VersionName");
            this.label2.Text = SR.GetString("AddQtVersionDialog_Path");
            this.okButton.Text = SR.GetString(SR.OK);
            this.cancelButton.Text = SR.GetString(SR.Cancel);
            this.Text = SR.GetString("AddQtVersionDialog_Title");

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;

            this.errorTimer = new Timer();
            this.errorTimer.Tick += new EventHandler(errorTimer_Tick);
            this.errorTimer.Interval = 3000;

            this.KeyPress += new KeyPressEventHandler(this.AddQtVersionDialog_KeyPress);
        }

        void errorTimer_Tick(object sender, EventArgs e)
        {
            errorLabel.Text = lastErrorString;
        }

        void AddQtVersionDialog_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.nameBox = new System.Windows.Forms.TextBox();
            this.pathBox = new System.Windows.Forms.TextBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.errorLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(8, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 24);
            this.label1.TabIndex = 0;
            this.label1.Text = "Version name:";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(8, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 23);
            this.label2.TabIndex = 1;
            this.label2.Text = "Path:";
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Enabled = false;
            this.okButton.Location = new System.Drawing.Point(144, 104);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "&OK";
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(224, 104);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "&Cancel";
            // 
            // nameBox
            // 
            this.nameBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nameBox.Location = new System.Drawing.Point(96, 16);
            this.nameBox.Name = "nameBox";
            this.nameBox.Size = new System.Drawing.Size(200, 20);
            this.nameBox.TabIndex = 4;
            this.nameBox.TextChanged += new System.EventHandler(this.DataChanged);
            // 
            // pathBox
            // 
            this.pathBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pathBox.Location = new System.Drawing.Point(96, 48);
            this.pathBox.Name = "pathBox";
            this.pathBox.Size = new System.Drawing.Size(176, 20);
            this.pathBox.TabIndex = 5;
            this.pathBox.TextChanged += new System.EventHandler(this.DataChanged);
            // 
            // browseButton
            // 
            this.browseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseButton.Location = new System.Drawing.Point(272, 48);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(24, 20);
            this.browseButton.TabIndex = 6;
            this.browseButton.Text = "...";
            this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
            // 
            // errorLabel
            // 
            this.errorLabel.AutoSize = true;
            this.errorLabel.ForeColor = System.Drawing.Color.Red;
            this.errorLabel.Location = new System.Drawing.Point(8, 71);
            this.errorLabel.Name = "errorLabel";
            this.errorLabel.Size = new System.Drawing.Size(0, 13);
            this.errorLabel.TabIndex = 7;
            // 
            // AddQtVersionDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(304, 134);
            this.Controls.Add(this.errorLabel);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.pathBox);
            this.Controls.Add(this.nameBox);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.KeyPreview = true;
            this.Name = "AddQtVersionDialog";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Add New Qt Version";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private void okButton_Click(object sender, EventArgs e)
        {
            QtVersionManager vm = QtVersionManager.The();
            VersionInformation versionInfo = null;
            try
            {
                versionInfo = new VersionInformation(pathBox.Text);
            }
            catch (Exception exception)
            {
                if (nameBox.Text == "$(QTDIR)")
                {
                    string defaultVersion = vm.GetDefaultVersion();
                    versionInfo = vm.GetVersionInfo(defaultVersion);
                }
                else
                {
                    Messages.DisplayErrorMessage(exception.Message);
                    return;
                }
            }

            string makefileGenerator = versionInfo.GetQMakeConfEntry("MAKEFILE_GENERATOR");
            if (makefileGenerator != "MSVC.NET" && makefileGenerator != "MSBUILD")
            {
                MessageBox.Show(SR.GetString("AddQtVersionDialog_IncorrectMakefileGenerator", makefileGenerator),
                                            null, MessageBoxButtons.OK,
                                            MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }
            vm.SaveVersion(nameBox.Text, pathBox.Text);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void DataChanged(object sender, EventArgs e)
        {
            errorLabel.Text = "";
            errorTimer.Stop();
            errorTimer.Start();
            string name = nameBox.Text.Trim();
            string path = pathBox.Text;

            if (sender == nameBox)
                nameBoxDirty = true;

            if (!nameBoxDirty)
            {
                string str;
                if (path.EndsWith("\\"))
                    str = path.Substring(0, path.Length - 1);
                else
                    str = path;

                int pos = str.LastIndexOf('\\');
                name = str.Substring(pos + 1);
                nameBox.TextChanged -= new System.EventHandler(this.DataChanged);
                nameBox.Text = name;
                nameBox.TextChanged += new System.EventHandler(this.DataChanged);
            }

            pathBox.Enabled = name != "$(QTDIR)";
            browseButton.Enabled = pathBox.Enabled;

            if (name.Length < 1 || (name != "$(QTDIR)" && path.Length < 1))
            {
                okButton.Enabled = false;
                return;
            }

            if (name != "$(QTDIR)")
            {
                try
                {
                    System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(pathBox.Text);
                    if (!di.Exists)
                    {
                        lastErrorString = "";
                        okButton.Enabled = false;
                        return;
                    }
                }
                catch
                {
                    lastErrorString = SR.GetString("AddQtVersionDialog_InvalidDirectory");
                    okButton.Enabled = false;
                    return;
                }

                FileInfo fi = new FileInfo(pathBox.Text + "\\lib\\libqtmain.a");
                if (!fi.Exists)
                    fi = new FileInfo(pathBox.Text + "\\lib\\libqtmaind.a");
                if (fi.Exists)
                {
                    lastErrorString = SR.GetString("AddQtVersionDialog_MingwQt");
                    okButton.Enabled = false;
                    return;
                }

                fi = new FileInfo(pathBox.Text + "\\bin\\qmake.exe");
                if (!fi.Exists)
                {
                    lastErrorString = SR.GetString("AddQtVersionDialog_NotFound", fi.FullName);
                    okButton.Enabled = false;
                    return;
                }
            }

            bool found = false;
            foreach (string s in QtVersionManager.The().GetVersions())
            {
                if (nameBox.Text == s)
                {
                    lastErrorString = SR.GetString("AddQtVersionDialog_VersionAlreadyPresent");
                    found = true;
                    break;
                }
            }
            okButton.Enabled = !found;
            if (!found)
                lastErrorString = "";
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fd = new FolderBrowserDialog();
            fd.Description = SR.GetString("SelectQtPath");
            fd.SelectedPath = RestoreLastSelectedPath();
            if (fd.ShowDialog() == DialogResult.OK)
            {
                pathBox.Text = fd.SelectedPath;
                SaveLastSelectedPath(fd.SelectedPath);
            }
            fd.Dispose();
        }

        private static string RestoreLastSelectedPath()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath, false);
                if (key != null)
                    return (string)key.GetValue("QtVersionLastSelectedPath");
            }
            catch
            {
            }

            return "";
        }

        private static void SaveLastSelectedPath(string path)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (key != null)
                key.SetValue("QtVersionLastSelectedPath", path);
        }
    }
}
