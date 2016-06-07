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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace QtVsTools
{
    /// <summary>
    /// Summary description for AddTranslationDialog.
    /// </summary>
    public class AddTranslationDialog : Form
    {
        private Label langLabel;
        private ComboBox langComboBox;
        private Label label1;
        private Button okButton;
        private Button cancelButton;
        private TextBox fileTextBox;
        private EnvDTE.Project project;
        private Panel panel1;
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

            langLabel.Text = SR.GetString("AddTranslationDialog_Language");
            cancelButton.Text = SR.GetString(SR.Cancel);
            okButton.Text = SR.GetString(SR.OK);
            label1.Text = SR.GetString("AddTranslationDialog_FileName");
            Text = SR.GetString("AddTranslationDialog_Title");

            FormBorderStyle = FormBorderStyle.FixedDialog;
            KeyPress += new KeyPressEventHandler(AddTranslationDialog_KeyPress);
        }

        void AddTranslationDialog_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27) {
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
            langLabel = new Label();
            langComboBox = new ComboBox();
            cancelButton = new Button();
            okButton = new Button();
            label1 = new Label();
            fileTextBox = new TextBox();
            panel1 = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();
            //
            // langLabel
            //
            langLabel.Anchor = ((AnchorStyles) (((AnchorStyles.Top | AnchorStyles.Left)
                        | AnchorStyles.Right)));
            langLabel.Location = new System.Drawing.Point(8, 8);
            langLabel.Name = "langLabel";
            langLabel.Size = new System.Drawing.Size(72, 21);
            langLabel.TabIndex = 0;
            langLabel.Text = "Language";
            //
            // langComboBox
            //
            langComboBox.Anchor = ((AnchorStyles) ((AnchorStyles.Top | AnchorStyles.Right)));
            langComboBox.AutoCompleteMode = AutoCompleteMode.Suggest;
            langComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
            langComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            langComboBox.Location = new System.Drawing.Point(80, 8);
            langComboBox.Name = "langComboBox";
            langComboBox.Size = new System.Drawing.Size(192, 21);
            langComboBox.Sorted = true;
            langComboBox.TabIndex = 1;
            langComboBox.SelectedIndexChanged += new EventHandler(langComboBox_SelectedIndexChanged);
            //
            // cancelButton
            //
            cancelButton.Anchor = ((AnchorStyles) ((AnchorStyles.Bottom | AnchorStyles.Right)));
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(200, 72);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(72, 24);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            //
            // okButton
            //
            okButton.Anchor = ((AnchorStyles) ((AnchorStyles.Bottom | AnchorStyles.Right)));
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new System.Drawing.Point(120, 72);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(72, 24);
            okButton.TabIndex = 0;
            okButton.Text = "OK";
            //
            // label1
            //
            label1.Anchor = ((AnchorStyles) (((AnchorStyles.Top | AnchorStyles.Left)
                        | AnchorStyles.Right)));
            label1.Location = new System.Drawing.Point(8, 32);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(72, 24);
            label1.TabIndex = 3;
            label1.Text = "Filename";
            //
            // fileTextBox
            //
            fileTextBox.Anchor = ((AnchorStyles) ((AnchorStyles.Top | AnchorStyles.Right)));
            fileTextBox.Location = new System.Drawing.Point(80, 32);
            fileTextBox.Name = "fileTextBox";
            fileTextBox.Size = new System.Drawing.Size(192, 20);
            fileTextBox.TabIndex = 4;
            //
            // panel1
            //
            panel1.Anchor = ((AnchorStyles) (((AnchorStyles.Top | AnchorStyles.Left)
                        | AnchorStyles.Right)));
            panel1.Controls.Add(langLabel);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(langComboBox);
            panel1.Controls.Add(fileTextBox);
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(280, 64);
            panel1.TabIndex = 5;
            //
            // AddTranslationDialog
            //
            AcceptButton = okButton;
            AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(282, 104);
            Controls.Add(panel1);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddTranslationDialog";
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add Translation";
            Load += new EventHandler(AddTranslationDialog_Load);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);

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
            var country = ((TranslationItem) langComboBox.SelectedItem).TwoLetterISOLanguageName;
            fileTextBox.Text = project.Name.ToLower() + "_" + country + ".ts";
        }
    }

    public class TranslationItem : System.Globalization.CultureInfo
    {
        public TranslationItem(int culture)
            : base(culture)
        {
        }

        public override string ToString()
        {
            if (NativeName != DisplayName)
                return DisplayName;

            var culture = CultureInfo.GetCultureInfo(Vsix.Instance.Dte.LocaleID);
            if (culture.TwoLetterISOLanguageName == TwoLetterISOLanguageName)
                return DisplayName;

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
            for (int i = 0; i < cultures.Length; i++) {
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
