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

using QtProjectLib;
using System.Collections;
using System.Windows.Forms;

namespace QtVsTools
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
            var vM = QtVersionManager.The();
            InitializeComponent();

            cancelButton.Text = SR.GetString(SR.Cancel);
            okButton.Text = SR.GetString(SR.OK);
            groupBox1.Text = SR.GetString("QtVersionDialog_BoxTitle");
            Text = SR.GetString("QtVersionDialog_Title");

            versionComboBox.Items.AddRange(vM.GetVersions());
            if (versionComboBox.Items.Count > 0) {
                var defVersion = vM.GetSolutionQtVersion(dteObj.Solution);
                if (!string.IsNullOrEmpty(defVersion)) {
                    versionComboBox.Text = defVersion;
                } else if (dte.Solution != null && HelperFunctions.ProjectsInSolution(dte) != null) {
                    IEnumerator prjEnum = HelperFunctions.ProjectsInSolution(dte).GetEnumerator();
                    prjEnum.Reset();
                    if (prjEnum.MoveNext()) {
                        var prj = prjEnum.Current as EnvDTE.Project;
                        defVersion = vM.GetProjectQtVersion(prj);
                    }
                }
                if (!string.IsNullOrEmpty(defVersion))
                    versionComboBox.Text = defVersion;
                else
                    versionComboBox.Text = (string) versionComboBox.Items[0];
            }

            //if (SR.LanguageName == "ja")
            //{
            //    cancelButton.Location = new System.Drawing.Point(224, 72);
            //    cancelButton.Size = new Size(80, 22);
            //    okButton.Location = new System.Drawing.Point(138, 72);
            //    okButton.Size = new Size(80, 22);
            //}
            KeyPress += QtVersionDialog_KeyPress;
        }

        void QtVersionDialog_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27) {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        public string QtVersion
        {
            get { return versionComboBox.Text; }
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
            versionComboBox = new System.Windows.Forms.ComboBox();
            cancelButton = new System.Windows.Forms.Button();
            okButton = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            groupBox1.SuspendLayout();
            SuspendLayout();
            //
            // versionComboBox
            //
            versionComboBox.Location = new System.Drawing.Point(8, 24);
            versionComboBox.Name = "versionComboBox";
            versionComboBox.Size = new System.Drawing.Size(280, 21);
            versionComboBox.TabIndex = 0;
            //
            // cancelButton
            //
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(232, 72);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(75, 23);
            cancelButton.TabIndex = 1;
            //
            // okButton
            //
            okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            okButton.Location = new System.Drawing.Point(152, 72);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(75, 23);
            okButton.TabIndex = 2;
            //
            // groupBox1
            //
            groupBox1.Controls.Add(versionComboBox);
            groupBox1.Location = new System.Drawing.Point(8, 8);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(296, 56);
            groupBox1.TabIndex = 4;
            groupBox1.TabStop = false;
            //
            // QtVersionDialog
            //
            AcceptButton = okButton;
            AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(314, 103);
            Controls.Add(groupBox1);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            KeyPreview = true;
            Name = "QtVersionDialog";
            groupBox1.ResumeLayout(false);
            ResumeLayout(false);

        }
        #endregion
    }
}
