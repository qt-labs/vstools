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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using QtProjectLib;
using QtVsTools.SyntaxAnalysis;
using QtVsTools.VisualStudio;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace QtVsTools
{
    using static RegExpr;

    public class AddQtVersionDialog : Form
    {
        private Button okButton;
        private Button cancelButton;
        private bool nameBoxDirty;
        private TextBox nameBox;
        private TableLayoutPanel tableLayoutPanel1;
        private FlowLayoutPanel flowLayoutPanel1;
        private Label label1;
        private Button browseButton;
        private TextBox pathBox;
        private Label label3;
        private Label label2;
        private ComboBox comboBoxHost;
        private Label label4;
        private TextBox compilerBox;
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

            skipDataChanged = true;
            comboBoxHost.SelectedIndex = 0;
            skipDataChanged = false;
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
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.errorLabel = new System.Windows.Forms.Label();
            this.nameBox = new System.Windows.Forms.TextBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBoxHost = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.pathBox = new System.Windows.Forms.TextBox();
            this.compilerBox = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Enabled = false;
            this.okButton.Location = new System.Drawing.Point(137, 3);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 5;
            this.okButton.Text = "&OK";
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(218, 3);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "&Cancel";
            // 
            // errorLabel
            // 
            this.errorLabel.AutoEllipsis = true;
            this.tableLayoutPanel1.SetColumnSpan(this.errorLabel, 3);
            this.errorLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.errorLabel.ForeColor = System.Drawing.Color.Red;
            this.errorLabel.Location = new System.Drawing.Point(3, 108);
            this.errorLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.errorLabel.Name = "errorLabel";
            this.errorLabel.Size = new System.Drawing.Size(378, 18);
            this.errorLabel.TabIndex = 99;
            this.errorLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // nameBox
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.nameBox, 2);
            this.nameBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nameBox.Location = new System.Drawing.Point(85, 30);
            this.nameBox.Name = "nameBox";
            this.nameBox.Size = new System.Drawing.Size(296, 20);
            this.nameBox.TabIndex = 1;
            // 
            // browseButton
            // 
            this.browseButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.browseButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.browseButton.Location = new System.Drawing.Point(347, 56);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(34, 20);
            this.browseButton.TabIndex = 3;
            this.browseButton.Text = "...";
            // 
            // label2
            // 
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(3, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(76, 26);
            this.label2.TabIndex = 99;
            this.label2.Text = "Path:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(3, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(76, 27);
            this.label3.TabIndex = 99;
            this.label3.Text = "Build Host:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // comboBoxHost
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.comboBoxHost, 2);
            this.comboBoxHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxHost.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxHost.FormattingEnabled = true;
            this.comboBoxHost.Items.AddRange(new object[] {
            "Windows",
            "Linux SSH",
            "Linux WSL"});
            this.comboBoxHost.Location = new System.Drawing.Point(85, 3);
            this.comboBoxHost.Name = "comboBoxHost";
            this.comboBoxHost.Size = new System.Drawing.Size(296, 21);
            this.comboBoxHost.TabIndex = 0;
            this.comboBoxHost.SelectedIndexChanged += new System.EventHandler(this.comboBoxHost_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 26);
            this.label1.TabIndex = 99;
            this.label1.Text = "Version Name:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 82F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.Controls.Add(this.errorLabel, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.browseButton, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.pathBox, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.compilerBox, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxHost, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.nameBox, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 5);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 6;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(384, 161);
            this.tableLayoutPanel1.TabIndex = 99;
            // 
            // label4
            // 
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(3, 79);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(76, 26);
            this.label4.TabIndex = 99;
            this.label4.Text = "Compiler:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pathBox
            // 
            this.pathBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pathBox.Location = new System.Drawing.Point(85, 56);
            this.pathBox.Name = "pathBox";
            this.pathBox.Size = new System.Drawing.Size(256, 20);
            this.pathBox.TabIndex = 2;
            // 
            // compilerBox
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.compilerBox, 2);
            this.compilerBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.compilerBox.Location = new System.Drawing.Point(85, 82);
            this.compilerBox.Name = "compilerBox";
            this.compilerBox.Size = new System.Drawing.Size(296, 20);
            this.compilerBox.TabIndex = 4;
            // 
            // flowLayoutPanel1
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 2);
            this.flowLayoutPanel1.Controls.Add(this.cancelButton);
            this.flowLayoutPanel1.Controls.Add(this.okButton);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(85, 129);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(296, 29);
            this.flowLayoutPanel1.TabIndex = 99;
            // 
            // AddQtVersionDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(384, 161);
            this.Controls.Add(this.tableLayoutPanel1);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(800, 200);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(400, 200);
            this.Name = "AddQtVersionDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Add New Qt Version";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private void okButton_Click(object sender, EventArgs e)
        {
            try {
                if (comboBoxHost.Text == "Windows") {
                    var versionInfo = VersionInformation.Get(pathBox.Text);
                    var generator = versionInfo.GetQMakeConfEntry("MAKEFILE_GENERATOR");
                    if (generator != "MSVC.NET" && generator != "MSBUILD")
                        throw new Exception(SR.GetString("AddQtVersionDialog_IncorrectMakefileGenerator", generator));
                    QtVersionManager.The().SaveVersion(nameBox.Text, pathBox.Text);
                } else {
                    string name = nameBox.Text;
                    string access = comboBoxHost.Text == "Linux SSH" ? "SSH" : "WSL";
                    string path = pathBox.Text;
                    string compiler = compilerBox.Text;
                    if (compiler == "g++")
                        compiler = string.Empty;
                    path = string.Format("{0}:{1}:{2}", access, path, compiler);
                    QtVersionManager.The().SaveVersion(name, path, checkPath: false);
                }
                DialogResult = DialogResult.OK;
                Close();
            } catch (Exception exception) {
                Messages.DisplayErrorMessage(exception.Message);
            }
        }

        Parser _InvalidName;
        Parser InvalidName => _InvalidName
            ?? (_InvalidName = Char[Path.DirectorySeparatorChar].Render());

        bool skipDataChanged = false;
        private void DataChanged(object sender, EventArgs e)
        {
            if (skipDataChanged)
                return;

            bool isWindows = comboBoxHost.Text == "Windows";

            if (sender == nameBox)
                nameBoxDirty = true;

            var path = pathBox.Text;
            var name = nameBox.Text.Trim();

            if (!nameBoxDirty && nameBox.Enabled) {
                if (isWindows) {
                    var str = path.TrimEnd('\\');
                    name = str.Substring(str.LastIndexOf('\\') + 1);
                } else if (string.IsNullOrEmpty(name)) {
                    name = path;
                }
                if (nameBox.Text != name) {
                    nameBox.TextChanged -= DataChanged;
                    nameBox.Text = name;
                    nameBox.TextChanged += DataChanged;
                }
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
            } else if (InvalidName.Regex.IsMatch(name)) {
                errorLabel.Text = SR.GetString("AddQtVersionDialog_InvalidName");
            }
            if (isWindows) {
                if (string.IsNullOrWhiteSpace(name)) {
                    errorLabel.Text = SR.GetString("AddQtVersionDialog_InvalidName");
                } else if (InvalidName.Regex.IsMatch(name)) {
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
            }
            okButton.Enabled = string.IsNullOrEmpty(errorLabel.Text);
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            using (var fd = new FolderBrowserDialog()) {
                var settingsManager = VsShellSettings.Manager;
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

        bool skipHostChanged = false;
        private void comboBoxHost_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (skipHostChanged)
                return;

            bool oldIsWindows = browseButton.Visible;
            bool newIsWindows = comboBoxHost.Text == "Windows";
            if (oldIsWindows == newIsWindows)
                return;

            if (oldIsWindows)
                EnableLinuxMode();
            else
                EnableWindowsMode();
        }

        void EnableWindowsMode()
        {
            skipDataChanged = true;
            browseButton.Visible = true;
            tableLayoutPanel1.SetColumnSpan(pathBox, 1);
            pathBox.Text = "";
            compilerBox.Text = "msvc";
            compilerBox.Enabled = false;
            errorLabel.Text = "";
            skipDataChanged = false;
        }

        void EnableLinuxMode()
        {
            skipDataChanged = true;
            browseButton.Visible = false;
            tableLayoutPanel1.SetColumnSpan(pathBox, 2);
            pathBox.Text = "";
            compilerBox.Text = "g++";
            compilerBox.Enabled = true;
            errorLabel.Text = "";
            skipDataChanged = false;
        }
    }
}
