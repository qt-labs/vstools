namespace Qt5VSAddin
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
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lbQtVersions = new System.Windows.Forms.ListBox();
            this.lQtVersions = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(124, 231);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "&OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(205, 231);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // lbQtVersions
            // 
            this.lbQtVersions.FormattingEnabled = true;
            this.lbQtVersions.Location = new System.Drawing.Point(13, 39);
            this.lbQtVersions.Name = "lbQtVersions";
            this.lbQtVersions.Size = new System.Drawing.Size(267, 173);
            this.lbQtVersions.TabIndex = 0;
            // 
            // lQtVersions
            // 
            this.lQtVersions.AutoSize = true;
            this.lQtVersions.Location = new System.Drawing.Point(13, 20);
            this.lQtVersions.Name = "lQtVersions";
            this.lQtVersions.Size = new System.Drawing.Size(103, 13);
            this.lQtVersions.TabIndex = 3;
            this.lQtVersions.Text = "Installed Qt Versions";
            // 
            // FormChangeQtVersion
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.lQtVersions);
            this.Controls.Add(this.lbQtVersions);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormChangeQtVersion";
            this.ShowInTaskbar = false;
            this.Text = "FormChangeQtVersion";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ListBox lbQtVersions;
        private System.Windows.Forms.Label lQtVersions;
    }
}