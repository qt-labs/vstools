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

using System.Windows.Forms;
using System.Globalization;
using System.Collections.Generic;

namespace Qt5VSAddin
{
    /// <summary>
    /// Summary description for AddTranslationDialog.
    /// </summary>
    public class AddTranslationDialog : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label langLabel;
        private System.Windows.Forms.ComboBox langComboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TextBox fileTextBox;
        private EnvDTE.Project project;
        private System.Windows.Forms.Panel panel1;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public AddTranslationDialog(EnvDTE.Project pro)
        {
            project = pro;
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            ShowInTaskbar = false;

            this.langLabel.Text = SR.GetString("AddTranslationDialog_Language");
            this.cancelButton.Text = SR.GetString(SR.Cancel);
            this.okButton.Text = SR.GetString(SR.OK);
            this.label1.Text = SR.GetString("AddTranslationDialog_FileName");
            this.Text = SR.GetString("AddTranslationDialog_Title");
        
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;

            //if (SR.LanguageName == "ja")
            //{
            //    this.cancelButton.Location = new System.Drawing.Point(188, 72);
            //    this.cancelButton.Size = new System.Drawing.Size(84, 24);
            //    this.okButton.Location = new System.Drawing.Point(100, 72);
            //    this.okButton.Size = new System.Drawing.Size(84, 24);
            //}
            this.KeyPress += new KeyPressEventHandler(this.AddTranslationDialog_KeyPress);
        }

        void AddTranslationDialog_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        public string TranslationFile
        {
            get { return fileTextBox.Text; }
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
            this.langLabel = new System.Windows.Forms.Label();
            this.langComboBox = new System.Windows.Forms.ComboBox();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.fileTextBox = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // langLabel
            // 
            this.langLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.langLabel.Location = new System.Drawing.Point(8, 8);
            this.langLabel.Name = "langLabel";
            this.langLabel.Size = new System.Drawing.Size(72, 21);
            this.langLabel.TabIndex = 0;
            this.langLabel.Text = "Language";
            // 
            // langComboBox
            // 
            this.langComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.langComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.langComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.langComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.langComboBox.Location = new System.Drawing.Point(80, 8);
            this.langComboBox.Name = "langComboBox";
            this.langComboBox.Size = new System.Drawing.Size(192, 21);
            this.langComboBox.Sorted = true;
            this.langComboBox.TabIndex = 1;
            this.langComboBox.SelectedIndexChanged += new System.EventHandler(this.langComboBox_SelectedIndexChanged);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(200, 72);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(72, 24);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(120, 72);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(72, 24);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(8, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 24);
            this.label1.TabIndex = 3;
            this.label1.Text = "Filename";
            // 
            // fileTextBox
            // 
            this.fileTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.fileTextBox.Location = new System.Drawing.Point(80, 32);
            this.fileTextBox.Name = "fileTextBox";
            this.fileTextBox.Size = new System.Drawing.Size(192, 20);
            this.fileTextBox.TabIndex = 4;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.langLabel);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.langComboBox);
            this.panel1.Controls.Add(this.fileTextBox);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(280, 64);
            this.panel1.TabIndex = 5;
            // 
            // AddTranslationDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(282, 104);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddTranslationDialog";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add Translation";
            this.Load += new System.EventHandler(this.AddTranslationDialog_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        private void AddTranslationDialog_Load(object sender, System.EventArgs e)
        {
            TranslationItem[] cultures = TranslationItem.GetTranslationItems();
            langComboBox.Items.AddRange(cultures);
            langComboBox.SelectedItem = TranslationItem.SystemLanguage();
        }

        private void langComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            string country = ((TranslationItem)langComboBox.SelectedItem).TwoLetterISOLanguageName;
            fileTextBox.Text = project.Name.ToLower() + "_" + country + ".ts";
        }        
    }

    public class TranslationItem : System.Globalization.CultureInfo
    {
        public TranslationItem(int culture) : base(culture) { }

        public override string ToString()
        {
            CultureInfo currentCulture = CultureInfo.GetCultureInfo(Connect.Instance.Dte.LocaleID);
            if (NativeName != DisplayName ||
                currentCulture.TwoLetterISOLanguageName == this.TwoLetterISOLanguageName)
                return DisplayName;
            else
                return EnglishName;
        }

        public static TranslationItem SystemLanguage()
        {
            return new TranslationItem(CultureInfo.CurrentCulture.LCID);
        }

        public static TranslationItem[] GetTranslationItems()
        {
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures
                & ~CultureTypes.UserCustomCulture & ~CultureTypes.ReplacementCultures);
            List<TranslationItem> transItems = new List<TranslationItem>();
            for(int i=0; i<cultures.Length; i++)
            {
                // Locales without a LCID are given LCID 0x1000 (http://msdn.microsoft.com/en-us/library/dn363603.aspx)
                // Trying to create a TranslationItem for these will cause an exception to be thrown.
                int lcid = cultures[i].LCID;
                if (lcid != 0x1000)
                    transItems.Add(new TranslationItem(lcid));
            }
            return transItems.ToArray();
        }
    }
}
