namespace QtVsTools
{
    partial class FormChangeQtVersion
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">
        /// true if managed resources should be disposed; otherwise, false.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnOK = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            lbQtVersions = new System.Windows.Forms.ListBox();
            lQtVersions = new System.Windows.Forms.Label();
            SuspendLayout();
            //
            // btnOK
            //
            btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnOK.Location = new System.Drawing.Point(124, 231);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(75, 23);
            btnOK.TabIndex = 1;
            btnOK.Text = "&OK";
            btnOK.UseVisualStyleBackColor = true;
            //
            // btnCancel
            //
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(205, 231);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(75, 23);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "&Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            //
            // lbQtVersions
            //
            lbQtVersions.FormattingEnabled = true;
            lbQtVersions.Location = new System.Drawing.Point(13, 39);
            lbQtVersions.Name = "lbQtVersions";
            lbQtVersions.Size = new System.Drawing.Size(267, 173);
            lbQtVersions.TabIndex = 0;
            //
            // lQtVersions
            //
            lQtVersions.AutoSize = true;
            lQtVersions.Location = new System.Drawing.Point(13, 20);
            lQtVersions.Name = "lQtVersions";
            lQtVersions.Size = new System.Drawing.Size(103, 13);
            lQtVersions.TabIndex = 3;
            lQtVersions.Text = "Installed Qt Versions";
            //
            // FormChangeQtVersion
            //
            AcceptButton = btnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(292, 266);
            Controls.Add(lQtVersions);
            Controls.Add(lbQtVersions);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormChangeQtVersion";
            ShowInTaskbar = false;
            Text = "FormChangeQtVersion";
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ListBox lbQtVersions;
        private System.Windows.Forms.Label lQtVersions;
    }
}