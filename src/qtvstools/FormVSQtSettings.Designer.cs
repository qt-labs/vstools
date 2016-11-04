namespace QtVsTools
{
    partial class FormVSQtSettings
    {
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            okButton = new System.Windows.Forms.Button();
            cancelButton = new System.Windows.Forms.Button();
            tabControl1 = new System.Windows.Forms.TabControl();
            tabPage1 = new System.Windows.Forms.TabPage();
            defaultCombo = new System.Windows.Forms.ComboBox();
            label2 = new System.Windows.Forms.Label();
            deleteButton = new System.Windows.Forms.Button();
            addButton = new System.Windows.Forms.Button();
            listView = new System.Windows.Forms.ListView();
            tabPage2 = new System.Windows.Forms.TabPage();
            optionsPropertyGrid = new System.Windows.Forms.PropertyGrid();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            SuspendLayout();
            //
            // okButton
            //
            okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            okButton.Location = new System.Drawing.Point(247, 287);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(80, 24);
            okButton.TabIndex = 18;
            okButton.Text = "&OK";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += okButton_Click;
            //
            // cancelButton
            //
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(337, 287);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(75, 23);
            cancelButton.TabIndex = 19;
            cancelButton.Text = "&Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += cancelButton_Click;
            //
            // tabControl1
            //
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Location = new System.Drawing.Point(13, 13);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new System.Drawing.Size(399, 268);
            tabControl1.TabIndex = 20;
            //
            // tabPage1
            //
            tabPage1.BackColor = System.Drawing.SystemColors.Control;
            tabPage1.Controls.Add(defaultCombo);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(deleteButton);
            tabPage1.Controls.Add(addButton);
            tabPage1.Controls.Add(listView);
            tabPage1.Location = new System.Drawing.Point(4, 22);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new System.Windows.Forms.Padding(3);
            tabPage1.Size = new System.Drawing.Size(391, 242);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            //
            // defaultCombo
            //
            defaultCombo.FormattingEnabled = true;
            defaultCombo.Location = new System.Drawing.Point(146, 207);
            defaultCombo.Name = "defaultCombo";
            defaultCombo.Size = new System.Drawing.Size(145, 21);
            defaultCombo.TabIndex = 22;
            //
            // label2
            //
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(6, 210);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(119, 13);
            label2.TabIndex = 21;
            label2.Text = "Default Qt/Win version:";
            //
            // deleteButton
            //
            deleteButton.Location = new System.Drawing.Point(301, 36);
            deleteButton.Name = "deleteButton";
            deleteButton.Size = new System.Drawing.Size(80, 24);
            deleteButton.TabIndex = 20;
            deleteButton.Text = "&Delete";
            deleteButton.UseVisualStyleBackColor = true;
            deleteButton.Click += deleteButton_Click;
            //
            // addButton
            //
            addButton.Location = new System.Drawing.Point(301, 6);
            addButton.Name = "addButton";
            addButton.Size = new System.Drawing.Size(80, 24);
            addButton.TabIndex = 19;
            addButton.Text = "&Add";
            addButton.UseVisualStyleBackColor = true;
            addButton.Click += addButton_Click;
            //
            // listView
            //
            listView.FullRowSelect = true;
            listView.HideSelection = false;
            listView.Location = new System.Drawing.Point(6, 6);
            listView.MultiSelect = false;
            listView.Name = "listView";
            listView.Size = new System.Drawing.Size(285, 195);
            listView.TabIndex = 18;
            listView.UseCompatibleStateImageBehavior = false;
            listView.View = System.Windows.Forms.View.Details;
            //
            // tabPage2
            //
            tabPage2.BackColor = System.Drawing.SystemColors.Control;
            tabPage2.Controls.Add(optionsPropertyGrid);
            tabPage2.Location = new System.Drawing.Point(4, 22);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new System.Windows.Forms.Padding(3);
            tabPage2.Size = new System.Drawing.Size(391, 242);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            //
            // optionsPropertyGrid
            //
            optionsPropertyGrid.HelpVisible = false;
            optionsPropertyGrid.Location = new System.Drawing.Point(7, 7);
            optionsPropertyGrid.Name = "optionsPropertyGrid";
            optionsPropertyGrid.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
            optionsPropertyGrid.Size = new System.Drawing.Size(378, 229);
            optionsPropertyGrid.TabIndex = 0;
            optionsPropertyGrid.ToolbarVisible = false;
            //
            // FormVSQtSettings
            //
            AcceptButton = okButton;
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(427, 322);
            Controls.Add(tabControl1);
            Controls.Add(cancelButton);
            Controls.Add(okButton);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            KeyPreview = true;
            MaximizeBox = false;
            Name = "FormVSQtSettings";
            ShowInTaskbar = false;
            Text = "FormQtVersions";
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabPage2.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.ComboBox defaultCombo;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.PropertyGrid optionsPropertyGrid;
    }
}
