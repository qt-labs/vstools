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

using System.Drawing;
using System.Windows.Forms;

namespace QtProjectLib
{
    /// <summary>
    /// Summary description for ExportProjectDialog.
    /// </summary>
    internal class ExportProjectDialog : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label projLabel;
        private System.Windows.Forms.ListBox optionListBox;
        private System.Windows.Forms.Label optionLabel;
        private System.Windows.Forms.ComboBox optionComboBox;
        private System.Windows.Forms.Label commentLabel;
        private System.Windows.Forms.TextBox optionTextBox;
        private System.Windows.Forms.CheckBox openCheckBox;
        private System.Windows.Forms.CheckedListBox projListBox;
        private System.Windows.Forms.GroupBox lineBox;
        private System.Windows.Forms.CheckBox createPriFileCheckBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button newButton;
        private System.Windows.Forms.Button delButton;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components;

        public ExportProjectDialog()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            cancelButton.Text = SR.GetString("ExportProjectDialog_Cancel");
            okButton.Text = SR.GetString("ExportProjectDialog_OK");
            projLabel.Text = SR.GetString("ExportProjectDialog_CreatePro");
            optionLabel.Text = SR.GetString("ExportProjectDialog_Project");
            optionTextBox.Text = "";
            openCheckBox.Text = SR.GetString("ExportProjectDialog_Open");
            createPriFileCheckBox.Text = SR.GetString("ExportProjectDialog_CreatePri");
            Text = SR.GetString("ExportProjectDialog_Title");

            if (SR.LanguageName == "de")
                Size = new Size(470, 300);
            else
                Size = new Size(400, 300);

            ShowInTaskbar = false;
            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                if (components != null) {
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
            cancelButton = new System.Windows.Forms.Button();
            okButton = new System.Windows.Forms.Button();
            projLabel = new System.Windows.Forms.Label();
            optionListBox = new System.Windows.Forms.ListBox();
            optionLabel = new System.Windows.Forms.Label();
            optionComboBox = new System.Windows.Forms.ComboBox();
            commentLabel = new System.Windows.Forms.Label();
            optionTextBox = new System.Windows.Forms.TextBox();
            projListBox = new System.Windows.Forms.CheckedListBox();
            openCheckBox = new System.Windows.Forms.CheckBox();
            lineBox = new System.Windows.Forms.GroupBox();
            createPriFileCheckBox = new System.Windows.Forms.CheckBox();
            panel1 = new System.Windows.Forms.Panel();
            panel2 = new System.Windows.Forms.Panel();

            newButton = new ImageButton(HelperFunctions.GetSharedImage("QtProjectLib.Resources.newitem.png"),
                HelperFunctions.GetSharedImage("QtProjectLib.Resources.newitem_d.png"));
            delButton = new ImageButton(HelperFunctions.GetSharedImage("QtProjectLib.Resources.delete.png"),
                HelperFunctions.GetSharedImage("QtProjectLib.Resources.delete_d.png"));
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            //
            // cancelButton
            //
            cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(352, 232);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(72, 24);
            cancelButton.TabIndex = 5;
            cancelButton.Text = "Cancel";
            //
            // okButton
            //
            okButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            okButton.Location = new System.Drawing.Point(272, 232);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(72, 24);
            okButton.TabIndex = 4;
            okButton.Text = "OK";
            //
            // projLabel
            //
            projLabel.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            projLabel.Location = new System.Drawing.Point(0, 8);
            projLabel.Name = "projLabel";
            projLabel.Size = new System.Drawing.Size(200, 16);
            projLabel.TabIndex = 3;
            projLabel.Text = "Create .pro files for:";
            //
            // optionListBox
            //
            optionListBox.Location = new System.Drawing.Point(0, 72);
            optionListBox.Name = "optionListBox";
            optionListBox.Size = new System.Drawing.Size(200, 82);
            optionListBox.TabIndex = 3;
            optionListBox.SelectedIndexChanged += optionListBox_SelectedIndexChanged;
            //
            // optionLabel
            //
            optionLabel.Location = new System.Drawing.Point(0, 8);
            optionLabel.Name = "optionLabel";
            optionLabel.Size = new System.Drawing.Size(200, 16);
            optionLabel.TabIndex = 5;
            optionLabel.Text = "Project &tag:";
            //
            // optionComboBox
            //
            optionComboBox.Location = new System.Drawing.Point(0, 24);
            optionComboBox.Name = "optionComboBox";
            optionComboBox.Size = new System.Drawing.Size(200, 21);
            optionComboBox.TabIndex = 2;
            optionComboBox.SelectedIndexChanged += optionComboBox_SelectedIndexChanged;
            //
            // commentLabel
            //
            commentLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            commentLabel.Location = new System.Drawing.Point(0, 160);
            commentLabel.Name = "commentLabel";
            commentLabel.Size = new System.Drawing.Size(200, 48);
            commentLabel.TabIndex = 6;
            //
            // optionTextBox
            //
            optionTextBox.Enabled = false;
            optionTextBox.Location = new System.Drawing.Point(0, 48);
            optionTextBox.Name = "optionTextBox";
            optionTextBox.Size = new System.Drawing.Size(136, 20);
            optionTextBox.TabIndex = 7;
            optionTextBox.Text = "";
            optionTextBox.TextChanged += optionTextBox_TextChanged;
            //
            // projListBox
            //
            projListBox.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            projListBox.Location = new System.Drawing.Point(0, 24);
            projListBox.Name = "projListBox";
            projListBox.Size = new System.Drawing.Size(200, 124);
            projListBox.TabIndex = 10;
            projListBox.SelectedIndexChanged += projListBox_SelectedIndexChanged;
            projListBox.ItemCheck += projListBox_ItemCheck;
            //
            // openCheckBox
            //
            openCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            openCheckBox.Checked = true;
            openCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            openCheckBox.Location = new System.Drawing.Point(0, 184);
            openCheckBox.Name = "openCheckBox";
            openCheckBox.Size = new System.Drawing.Size(208, 24);
            openCheckBox.TabIndex = 11;
            openCheckBox.Text = "Open Created Files";
            //
            // lineBox
            //
            lineBox.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            lineBox.Location = new System.Drawing.Point(-8, 216);
            lineBox.Name = "lineBox";
            lineBox.Size = new System.Drawing.Size(536, 8);
            lineBox.TabIndex = 12;
            lineBox.TabStop = false;
            //
            // createPriFileCheckBox
            //
            createPriFileCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            createPriFileCheckBox.Checked = true;
            createPriFileCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            createPriFileCheckBox.Location = new System.Drawing.Point(0, 160);
            createPriFileCheckBox.Name = "createPriFileCheckBox";
            createPriFileCheckBox.Size = new System.Drawing.Size(200, 24);
            createPriFileCheckBox.TabIndex = 13;
            createPriFileCheckBox.Text = "Create .pri File";
            //
            // panel1
            //
            panel1.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            panel1.Controls.Add(projLabel);
            panel1.Controls.Add(projListBox);
            panel1.Controls.Add(createPriFileCheckBox);
            panel1.Controls.Add(openCheckBox);
            panel1.Location = new System.Drawing.Point(8, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(200, 208);
            panel1.TabIndex = 14;
            //
            // panel2
            //
            panel2.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            panel2.Controls.Add(newButton);
            panel2.Controls.Add(delButton);
            panel2.Controls.Add(optionLabel);
            panel2.Controls.Add(optionComboBox);
            panel2.Controls.Add(optionTextBox);
            panel2.Controls.Add(optionListBox);
            panel2.Controls.Add(commentLabel);
            panel2.Location = new System.Drawing.Point(216, 0);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(208, 208);
            panel2.TabIndex = 15;
            //
            // newButton
            //
            newButton.Location = new System.Drawing.Point(144, 48);
            newButton.Name = "button1";
            newButton.Size = new System.Drawing.Size(24, 23);
            newButton.TabIndex = 8;
            newButton.Click += newButton_Click;

            //
            // delButton
            //
            delButton.Location = new System.Drawing.Point(176, 48);
            delButton.Name = "button2";
            delButton.Size = new System.Drawing.Size(24, 23);
            delButton.TabIndex = 9;
            delButton.Click += delButton_Click;

            //
            // ExportProjectDialog
            //
            AcceptButton = okButton;
            CancelButton = cancelButton;
            AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            ClientSize = new System.Drawing.Size(432, 262);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(lineBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ExportProjectDialog";
            SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            Text = "Export Project";
            Load += ExportProjectDialog_Load;
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ResumeLayout(false);

        }
        #endregion

        public ProSolution ProFileSolution
        {
            set
            {
                proSln = value;
                InitProSolution();
            }
        }

        private void InitProSolution()
        {
            projListBox.DataSource = proSln.ProFiles;
            projListBox.SelectedIndex = 0;
        }

        private ProSolution proSln;
        private ProFileContent currentPro;
        private ProFileOption currentOpt;

        private void optionComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            currentOpt = (ProFileOption) currentPro.Options[optionComboBox.SelectedIndex];
            UpdateCurrentListItem();

            optionTextBox.Text = "";

            // update comment field
            commentLabel.Text = currentOpt.Comment;
            UpdateButtons();
        }

        private void optionListBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            int idx = optionListBox.SelectedIndex;
            if (idx < 0)
                return;
            optionTextBox.Text = (string) currentOpt.List[optionListBox.SelectedIndex];
            optionTextBox.Focus();
            UpdateButtons();
        }

        private void optionTextBox_TextChanged(object sender, System.EventArgs e)
        {
            if (optionListBox.SelectedIndex < 0) {
                optionTextBox.Enabled = false;
            } else {
                optionTextBox.Enabled = true;
                currentOpt.List[optionListBox.SelectedIndex] = optionTextBox.Text;
                int index = optionListBox.SelectedIndex;
                UpdateCurrentListItem();
                optionListBox.SelectedIndex = index;
            }
        }

        private void UpdateCurrentListItem()
        {
            optionListBox.BeginUpdate();
            optionListBox.Items.Clear();
            foreach (string tag in currentOpt.List) {
                optionListBox.Items.Add(tag);
            }
            optionListBox.EndUpdate();
        }

        private void newButton_Click(object sender, System.EventArgs e)
        {
            currentOpt.List.Add("{New}");
            int index = currentOpt.List.Count - 1;
            UpdateCurrentListItem();
            optionListBox.SelectedIndex = index;
            optionTextBox.SelectAll();
        }

        private void UpdateButtons()
        {
            bool delEnabled = true;
            bool addEnabled = true;

            if (optionListBox.SelectedIndex < 0)
                delEnabled = false;

            if (optionListBox.Items.Count <= 0)
                delEnabled = false;

            if ((optionListBox.Items.Count > 0) && (currentOpt.NewOption == null))
                addEnabled = false;

            delButton.Enabled = delEnabled;
            newButton.Enabled = addEnabled;
        }

        private void delButton_Click(object sender, System.EventArgs e)
        {
            int index = optionListBox.SelectedIndex;
            currentOpt.List.RemoveAt(optionListBox.SelectedIndex);
            UpdateCurrentListItem();

            if (index > (optionListBox.Items.Count - 1))
                index--;

            optionListBox.SelectedIndex = index;

            if (index < 0) {
                optionTextBox.Text = "";
                UpdateButtons();
            }
        }

        private void projListBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            currentPro = (ProFileContent) proSln.ProFiles[projListBox.SelectedIndex];
            optionComboBox.DataSource = currentPro.Options;
        }

        private void projListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
                ((ProFileContent) proSln.ProFiles[e.Index]).Export = true;
            else
                ((ProFileContent) proSln.ProFiles[e.Index]).Export = false;
        }

        private void ExportProjectDialog_Load(object sender, System.EventArgs e)
        {
            for (int i = 0; i < projListBox.Items.Count; i++) {
                projListBox.SetItemChecked(i, true);
            }
        }

        public bool OpenFiles
        {
            get { return openCheckBox.Checked; }
        }

        public bool CreatePriFile
        {
            get { return createPriFileCheckBox.Checked; }
        }
    }
}
