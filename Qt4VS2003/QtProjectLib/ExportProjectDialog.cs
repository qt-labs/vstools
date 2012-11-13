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

using System.Drawing;
using System.Windows.Forms;

namespace Digia.Qt5ProjectLib
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
        private System.ComponentModel.Container components = null;

        public ExportProjectDialog()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            this.cancelButton.Text = SR.GetString("ExportProjectDialog_Cancel");
            this.okButton.Text = SR.GetString("ExportProjectDialog_OK");
            this.projLabel.Text = SR.GetString("ExportProjectDialog_CreatePro");
            this.optionLabel.Text = SR.GetString("ExportProjectDialog_Project");
            this.optionTextBox.Text = "";
            this.openCheckBox.Text = SR.GetString("ExportProjectDialog_Open");
            this.createPriFileCheckBox.Text = SR.GetString("ExportProjectDialog_CreatePri");
            this.Text = SR.GetString("ExportProjectDialog_Title");

            if (SR.LanguageName == "de")
                this.Size = new Size(470, 300);
            else
                this.Size = new Size(400, 300);

            ShowInTaskbar = false;
            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.projLabel = new System.Windows.Forms.Label();
            this.optionListBox = new System.Windows.Forms.ListBox();
            this.optionLabel = new System.Windows.Forms.Label();
            this.optionComboBox = new System.Windows.Forms.ComboBox();
            this.commentLabel = new System.Windows.Forms.Label();
            this.optionTextBox = new System.Windows.Forms.TextBox();
            this.projListBox = new System.Windows.Forms.CheckedListBox();
            this.openCheckBox = new System.Windows.Forms.CheckBox();
            this.lineBox = new System.Windows.Forms.GroupBox();
            this.createPriFileCheckBox = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();

            this.newButton = new ImageButton(HelperFunctions.GetSharedImage("Qt5ProjectLib.Images.newitem.png"),
                HelperFunctions.GetSharedImage("Qt5ProjectLib.Images.newitem_d.png"));
            this.delButton = new ImageButton(HelperFunctions.GetSharedImage("Qt5ProjectLib.Images.delete.png"),
                HelperFunctions.GetSharedImage("Qt5ProjectLib.Images.delete_d.png"));
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(352, 232);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(72, 24);
            this.cancelButton.TabIndex = 5;
            this.cancelButton.Text = "Cancel";
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(272, 232);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(72, 24);
            this.okButton.TabIndex = 4;
            this.okButton.Text = "OK";
            // 
            // projLabel
            // 
            this.projLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.projLabel.Location = new System.Drawing.Point(0, 8);
            this.projLabel.Name = "projLabel";
            this.projLabel.Size = new System.Drawing.Size(200, 16);
            this.projLabel.TabIndex = 3;
            this.projLabel.Text = "Create .pro files for:";
            // 
            // optionListBox
            // 
            this.optionListBox.Location = new System.Drawing.Point(0, 72);
            this.optionListBox.Name = "optionListBox";
            this.optionListBox.Size = new System.Drawing.Size(200, 82);
            this.optionListBox.TabIndex = 3;
            this.optionListBox.SelectedIndexChanged += new System.EventHandler(this.optionListBox_SelectedIndexChanged);
            // 
            // optionLabel
            // 
            this.optionLabel.Location = new System.Drawing.Point(0, 8);
            this.optionLabel.Name = "optionLabel";
            this.optionLabel.Size = new System.Drawing.Size(200, 16);
            this.optionLabel.TabIndex = 5;
            this.optionLabel.Text = "Project &tag:";
            // 
            // optionComboBox
            // 
            this.optionComboBox.Location = new System.Drawing.Point(0, 24);
            this.optionComboBox.Name = "optionComboBox";
            this.optionComboBox.Size = new System.Drawing.Size(200, 21);
            this.optionComboBox.TabIndex = 2;
            this.optionComboBox.SelectedIndexChanged += new System.EventHandler(this.optionComboBox_SelectedIndexChanged);
            // 
            // commentLabel
            // 
            this.commentLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.commentLabel.Location = new System.Drawing.Point(0, 160);
            this.commentLabel.Name = "commentLabel";
            this.commentLabel.Size = new System.Drawing.Size(200, 48);
            this.commentLabel.TabIndex = 6;
            // 
            // optionTextBox
            // 
            this.optionTextBox.Enabled = false;
            this.optionTextBox.Location = new System.Drawing.Point(0, 48);
            this.optionTextBox.Name = "optionTextBox";
            this.optionTextBox.Size = new System.Drawing.Size(136, 20);
            this.optionTextBox.TabIndex = 7;
            this.optionTextBox.Text = "";
            this.optionTextBox.TextChanged += new System.EventHandler(this.optionTextBox_TextChanged);
            // 
            // projListBox
            // 
            this.projListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.projListBox.Location = new System.Drawing.Point(0, 24);
            this.projListBox.Name = "projListBox";
            this.projListBox.Size = new System.Drawing.Size(200, 124);
            this.projListBox.TabIndex = 10;
            this.projListBox.SelectedIndexChanged += new System.EventHandler(this.projListBox_SelectedIndexChanged);
            this.projListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.projListBox_ItemCheck);
            // 
            // openCheckBox
            // 
            this.openCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.openCheckBox.Checked = true;
            this.openCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.openCheckBox.Location = new System.Drawing.Point(0, 184);
            this.openCheckBox.Name = "openCheckBox";
            this.openCheckBox.Size = new System.Drawing.Size(208, 24);
            this.openCheckBox.TabIndex = 11;
            this.openCheckBox.Text = "Open Created Files";
            // 
            // lineBox
            // 
            this.lineBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lineBox.Location = new System.Drawing.Point(-8, 216);
            this.lineBox.Name = "lineBox";
            this.lineBox.Size = new System.Drawing.Size(536, 8);
            this.lineBox.TabIndex = 12;
            this.lineBox.TabStop = false;
            // 
            // createPriFileCheckBox
            // 
            this.createPriFileCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.createPriFileCheckBox.Checked = true;
            this.createPriFileCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.createPriFileCheckBox.Location = new System.Drawing.Point(0, 160);
            this.createPriFileCheckBox.Name = "createPriFileCheckBox";
            this.createPriFileCheckBox.Size = new System.Drawing.Size(200, 24);
            this.createPriFileCheckBox.TabIndex = 13;
            this.createPriFileCheckBox.Text = "Create .pri File";
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.projLabel);
            this.panel1.Controls.Add(this.projListBox);
            this.panel1.Controls.Add(this.createPriFileCheckBox);
            this.panel1.Controls.Add(this.openCheckBox);
            this.panel1.Location = new System.Drawing.Point(8, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(200, 208);
            this.panel1.TabIndex = 14;
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.Controls.Add(this.newButton);
            this.panel2.Controls.Add(this.delButton);
            this.panel2.Controls.Add(this.optionLabel);
            this.panel2.Controls.Add(this.optionComboBox);
            this.panel2.Controls.Add(this.optionTextBox);
            this.panel2.Controls.Add(this.optionListBox);
            this.panel2.Controls.Add(this.commentLabel);
            this.panel2.Location = new System.Drawing.Point(216, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(208, 208);
            this.panel2.TabIndex = 15;
            // 
            // newButton
            // 
            this.newButton.Location = new System.Drawing.Point(144, 48);
            this.newButton.Name = "button1";
            this.newButton.Size = new System.Drawing.Size(24, 23);
            this.newButton.TabIndex = 8;
            this.newButton.Click += new System.EventHandler(this.newButton_Click);
            
            // 
            // delButton
            // 
            this.delButton.Location = new System.Drawing.Point(176, 48);
            this.delButton.Name = "button2";
            this.delButton.Size = new System.Drawing.Size(24, 23);
            this.delButton.TabIndex = 9;
            this.delButton.Click += new System.EventHandler(this.delButton_Click);
            
            // 
            // ExportProjectDialog
            // 
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(432, 262);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.lineBox);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExportProjectDialog";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Export Project";
            this.Load += new System.EventHandler(this.ExportProjectDialog_Load);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

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
            currentOpt = (ProFileOption)currentPro.Options[optionComboBox.SelectedIndex];
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
            optionTextBox.Text = (string)currentOpt.List[optionListBox.SelectedIndex];
            optionTextBox.Focus();
            UpdateButtons();
        }

        private void optionTextBox_TextChanged(object sender, System.EventArgs e)
        {
            if (optionListBox.SelectedIndex < 0)
            {
                optionTextBox.Enabled = false;
            }
            else
            {
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
			foreach(string tag in currentOpt.List)
            {
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

            if (index > (optionListBox.Items.Count-1))
                index--;

            optionListBox.SelectedIndex = index;

            if (index < 0)
            {
                optionTextBox.Text = "";
                UpdateButtons();
            }
        }

        private void projListBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            currentPro = (ProFileContent)proSln.ProFiles[projListBox.SelectedIndex];
            optionComboBox.DataSource = currentPro.Options;
        }

        private void projListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
                ((ProFileContent)proSln.ProFiles[e.Index]).Export = true;
            else
                ((ProFileContent)proSln.ProFiles[e.Index]).Export = false;
        }

        private void ExportProjectDialog_Load(object sender, System.EventArgs e)
        {
            for (int i=0; i<projListBox.Items.Count; i++)
            {
                projListBox.SetItemChecked(i,true);
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
