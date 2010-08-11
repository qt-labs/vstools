namespace Qt4VSAddin
{
    partial class FormProjectQtSettings
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
            this.OptionsPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.panel1 = new System.Windows.Forms.Panel();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.panel3 = new System.Windows.Forms.Panel();
            this.phononLib = new System.Windows.Forms.CheckBox();
            this.helpLib = new System.Windows.Forms.CheckBox();
            this.webKitLib = new System.Windows.Forms.CheckBox();
            this.xmlLib = new System.Windows.Forms.CheckBox();
            this.activeQtCLib = new System.Windows.Forms.CheckBox();
            this.activeQtSLib = new System.Windows.Forms.CheckBox();
            this.qt3Lib = new System.Windows.Forms.CheckBox();
            this.svgLib = new System.Windows.Forms.CheckBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.networkLib = new System.Windows.Forms.CheckBox();
            this.coreLib = new System.Windows.Forms.CheckBox();
            this.guiLib = new System.Windows.Forms.CheckBox();
            this.sqlLib = new System.Windows.Forms.CheckBox();
            this.openGLLib = new System.Windows.Forms.CheckBox();
            this.testLib = new System.Windows.Forms.CheckBox();
            this.scriptLib = new System.Windows.Forms.CheckBox();
            this.xmlPatternsLib = new System.Windows.Forms.CheckBox();
            this.multimediaLib = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // OptionsPropertyGrid
            // 
            this.OptionsPropertyGrid.HelpVisible = false;
            this.OptionsPropertyGrid.Location = new System.Drawing.Point(6, 6);
            this.OptionsPropertyGrid.Name = "OptionsPropertyGrid";
            this.OptionsPropertyGrid.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
            this.OptionsPropertyGrid.Size = new System.Drawing.Size(350, 213);
            this.OptionsPropertyGrid.TabIndex = 8;
            this.OptionsPropertyGrid.ToolbarVisible = false;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.okButton);
            this.panel1.Controls.Add(this.cancelButton);
            this.panel1.Location = new System.Drawing.Point(214, 403);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(168, 38);
            this.panel1.TabIndex = 9;
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(8, 8);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 0;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(88, 8);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 1;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(370, 373);
            this.tabControl1.TabIndex = 10;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage1.Controls.Add(this.OptionsPropertyGrid);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(362, 347);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General Settings";
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage2.Controls.Add(this.panel3);
            this.tabPage2.Controls.Add(this.panel2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(362, 347);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Add/Remove Qt Modules";
            // 
            // panel3
            // 
            this.panel3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel3.Controls.Add(this.phononLib);
            this.panel3.Controls.Add(this.helpLib);
            this.panel3.Controls.Add(this.webKitLib);
            this.panel3.Controls.Add(this.xmlLib);
            this.panel3.Controls.Add(this.activeQtCLib);
            this.panel3.Controls.Add(this.activeQtSLib);
            this.panel3.Controls.Add(this.qt3Lib);
            this.panel3.Controls.Add(this.svgLib);
            this.panel3.Location = new System.Drawing.Point(178, 6);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(184, 272);
            this.panel3.TabIndex = 12;
            // 
            // phononLib
            // 
            this.phononLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.phononLib.Location = new System.Drawing.Point(8, 176);
            this.phononLib.Name = "phononLib";
            this.phononLib.Size = new System.Drawing.Size(168, 24);
            this.phononLib.TabIndex = 7;
            // 
            // helpLib
            // 
            this.helpLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.helpLib.Location = new System.Drawing.Point(8, 128);
            this.helpLib.Name = "helpLib";
            this.helpLib.Size = new System.Drawing.Size(160, 24);
            this.helpLib.TabIndex = 5;
            // 
            // webKitLib
            // 
            this.webKitLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.webKitLib.Location = new System.Drawing.Point(8, 152);
            this.webKitLib.Name = "webKitLib";
            this.webKitLib.Size = new System.Drawing.Size(160, 24);
            this.webKitLib.TabIndex = 6;
            // 
            // xmlLib
            // 
            this.xmlLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.xmlLib.Location = new System.Drawing.Point(8, 8);
            this.xmlLib.Name = "xmlLib";
            this.xmlLib.Size = new System.Drawing.Size(160, 24);
            this.xmlLib.TabIndex = 0;
            // 
            // activeQtCLib
            // 
            this.activeQtCLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.activeQtCLib.Location = new System.Drawing.Point(8, 104);
            this.activeQtCLib.Name = "activeQtCLib";
            this.activeQtCLib.Size = new System.Drawing.Size(168, 24);
            this.activeQtCLib.TabIndex = 4;
            // 
            // activeQtSLib
            // 
            this.activeQtSLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.activeQtSLib.Location = new System.Drawing.Point(8, 80);
            this.activeQtSLib.Name = "activeQtSLib";
            this.activeQtSLib.Size = new System.Drawing.Size(168, 24);
            this.activeQtSLib.TabIndex = 3;
            // 
            // qt3Lib
            // 
            this.qt3Lib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.qt3Lib.Location = new System.Drawing.Point(8, 56);
            this.qt3Lib.Name = "qt3Lib";
            this.qt3Lib.Size = new System.Drawing.Size(168, 24);
            this.qt3Lib.TabIndex = 2;
            // 
            // svgLib
            // 
            this.svgLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.svgLib.Location = new System.Drawing.Point(8, 32);
            this.svgLib.Name = "svgLib";
            this.svgLib.Size = new System.Drawing.Size(168, 24);
            this.svgLib.TabIndex = 1;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.multimediaLib);
            this.panel2.Controls.Add(this.networkLib);
            this.panel2.Controls.Add(this.coreLib);
            this.panel2.Controls.Add(this.guiLib);
            this.panel2.Controls.Add(this.sqlLib);
            this.panel2.Controls.Add(this.openGLLib);
            this.panel2.Controls.Add(this.testLib);
            this.panel2.Controls.Add(this.scriptLib);
            this.panel2.Controls.Add(this.xmlPatternsLib);
            this.panel2.Location = new System.Drawing.Point(0, 6);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(176, 272);
            this.panel2.TabIndex = 11;
            // 
            // networkLib
            // 
            this.networkLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.networkLib.Location = new System.Drawing.Point(8, 128);
            this.networkLib.Name = "networkLib";
            this.networkLib.Size = new System.Drawing.Size(160, 24);
            this.networkLib.TabIndex = 5;
            // 
            // coreLib
            // 
            this.coreLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.coreLib.Location = new System.Drawing.Point(8, 8);
            this.coreLib.Name = "coreLib";
            this.coreLib.Size = new System.Drawing.Size(160, 24);
            this.coreLib.TabIndex = 0;
            // 
            // guiLib
            // 
            this.guiLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.guiLib.Location = new System.Drawing.Point(8, 32);
            this.guiLib.Name = "guiLib";
            this.guiLib.Size = new System.Drawing.Size(160, 24);
            this.guiLib.TabIndex = 1;
            // 
            // sqlLib
            // 
            this.sqlLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.sqlLib.Location = new System.Drawing.Point(8, 80);
            this.sqlLib.Name = "sqlLib";
            this.sqlLib.Size = new System.Drawing.Size(160, 24);
            this.sqlLib.TabIndex = 3;
            // 
            // openGLLib
            // 
            this.openGLLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.openGLLib.Location = new System.Drawing.Point(8, 104);
            this.openGLLib.Name = "openGLLib";
            this.openGLLib.Size = new System.Drawing.Size(160, 24);
            this.openGLLib.TabIndex = 4;
            // 
            // testLib
            // 
            this.testLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.testLib.Location = new System.Drawing.Point(8, 200);
            this.testLib.Name = "testLib";
            this.testLib.Size = new System.Drawing.Size(168, 24);
            this.testLib.TabIndex = 8;
            // 
            // scriptLib
            // 
            this.scriptLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.scriptLib.Location = new System.Drawing.Point(8, 152);
            this.scriptLib.Name = "scriptLib";
            this.scriptLib.Size = new System.Drawing.Size(168, 24);
            this.scriptLib.TabIndex = 6;
            // 
            // xmlPatternsLib
            // 
            this.xmlPatternsLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.xmlPatternsLib.Location = new System.Drawing.Point(8, 176);
            this.xmlPatternsLib.Name = "xmlPatternsLib";
            this.xmlPatternsLib.Size = new System.Drawing.Size(160, 24);
            this.xmlPatternsLib.TabIndex = 7;
            // 
            // multimediaLib
            // 
            this.multimediaLib.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.multimediaLib.Location = new System.Drawing.Point(8, 56);
            this.multimediaLib.Name = "multimediaLib";
            this.multimediaLib.Size = new System.Drawing.Size(168, 24);
            this.multimediaLib.TabIndex = 2;
            // 
            // FormProjectQtSettings
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(391, 453);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "FormProjectQtSettings";
            this.ShowInTaskbar = false;
            this.Text = "FormAddinSettings";
            this.panel1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PropertyGrid OptionsPropertyGrid;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.CheckBox helpLib;
        private System.Windows.Forms.CheckBox webKitLib;
        private System.Windows.Forms.CheckBox xmlLib;
        private System.Windows.Forms.CheckBox activeQtCLib;
        private System.Windows.Forms.CheckBox activeQtSLib;
        private System.Windows.Forms.CheckBox qt3Lib;
        private System.Windows.Forms.CheckBox svgLib;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.CheckBox networkLib;
        private System.Windows.Forms.CheckBox coreLib;
        private System.Windows.Forms.CheckBox guiLib;
        private System.Windows.Forms.CheckBox sqlLib;
        private System.Windows.Forms.CheckBox openGLLib;
        private System.Windows.Forms.CheckBox testLib;
        private System.Windows.Forms.CheckBox scriptLib;
        private System.Windows.Forms.CheckBox xmlPatternsLib;
        private System.Windows.Forms.CheckBox phononLib;
        private System.Windows.Forms.CheckBox multimediaLib;

    }
}