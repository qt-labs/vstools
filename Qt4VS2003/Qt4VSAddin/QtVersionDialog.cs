/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2011 Nokia Corporation and/or its subsidiary(-ies).
**
** Contact: Nokia Corporation (qt-info@nokia.com)
**
** Commercial Usage
**
** Licensees holding valid Qt Commercial licenses may use this file in
** accordance with the Qt Commercial License Agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Nokia.
**
** GNU Lesser General Public License Usage
**
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** If you are unsure which license is appropriate for your use, please
** contact the sales department at http://qt.nokia.com/contact.
**
**************************************************************************/

using System.Collections;
using System.Windows.Forms;
using Nokia.QtProjectLib;

namespace Qt4VSAddin
{
	/// <summary>
	/// Summary description for QtVersionDialog.
	/// </summary>
	public class QtVersionDialog : System.Windows.Forms.Form
	{
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.ComboBox versionComboBox;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private EnvDTE.DTE dteObj = null;

		public QtVersionDialog(EnvDTE.DTE dte)
		{
            dteObj = dte;
            QtVersionManager vM = QtVersionManager.The();
            InitializeComponent();

            this.cancelButton.Text = SR.GetString(SR.Cancel);
            this.okButton.Text = SR.GetString(SR.OK);
            this.groupBox1.Text = SR.GetString("QtVersionDialog_BoxTitle");
            this.Text = SR.GetString("QtVersionDialog_Title");
                        
            this.versionComboBox.Items.AddRange(vM.GetVersions());
            if (this.versionComboBox.Items.Count > 0) 
            {
                string defVersion = vM.GetSolutionQtVersion(dteObj.Solution);
                if (defVersion != null && defVersion.Length > 0)
                {
                    this.versionComboBox.Text = defVersion;
                }
                else if (dte.Solution != null && HelperFunctions.ProjectsInSolution(dte) != null)
                {
                    IEnumerator prjEnum = HelperFunctions.ProjectsInSolution(dte).GetEnumerator();
                    prjEnum.Reset();
                    if (prjEnum.MoveNext())
                    {
                        EnvDTE.Project prj = prjEnum.Current as EnvDTE.Project;
                        defVersion = vM.GetProjectQtVersion(prj);
                    }
                }
                if (defVersion != null && defVersion.Length > 0)
                    this.versionComboBox.Text = defVersion;
                else
                    this.versionComboBox.Text = (string)this.versionComboBox.Items[0];
            }

            //if (SR.LanguageName == "ja") 
            //{
            //    this.cancelButton.Location = new System.Drawing.Point(224, 72);
            //    this.cancelButton.Size = new Size(80, 22);
            //    this.okButton.Location = new System.Drawing.Point(138, 72);
            //    this.okButton.Size = new Size(80, 22);
            //}
            this.KeyPress += new KeyPressEventHandler(this.QtVersionDialog_KeyPress);
		}

        void QtVersionDialog_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        public string QtVersion
        {
            get { return this.versionComboBox.Text; }
        }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
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
            this.versionComboBox = new System.Windows.Forms.ComboBox();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // versionComboBox
            // 
            this.versionComboBox.Location = new System.Drawing.Point(8, 24);
            this.versionComboBox.Name = "versionComboBox";
            this.versionComboBox.Size = new System.Drawing.Size(280, 21);
            this.versionComboBox.TabIndex = 0;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(232, 72);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 1;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(152, 72);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 2;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.versionComboBox);
            this.groupBox1.Location = new System.Drawing.Point(8, 8);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(296, 56);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            // 
            // QtVersionDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(314, 103);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.Name = "QtVersionDialog";
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion
    }
}
