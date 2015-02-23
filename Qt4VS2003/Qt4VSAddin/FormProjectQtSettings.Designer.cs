namespace Qt5VSAddin
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
            this.panel2 = new System.Windows.Forms.Panel();
            this.quickLib = new System.Windows.Forms.CheckBox();
            this.qmlLib = new System.Windows.Forms.CheckBox();
            this.webKitLib = new System.Windows.Forms.CheckBox();
            this.multimediaLib = new System.Windows.Forms.CheckBox();
            this.networkLib = new System.Windows.Forms.CheckBox();
            this.coreLib = new System.Windows.Forms.CheckBox();
            this.threeDLib = new System.Windows.Forms.CheckBox();
            this.guiLib = new System.Windows.Forms.CheckBox();
            this.sqlLib = new System.Windows.Forms.CheckBox();
            this.testLib = new System.Windows.Forms.CheckBox();
            this.svgLib = new System.Windows.Forms.CheckBox();
            this.multimediaWidgetsLib = new System.Windows.Forms.CheckBox();
            this.concurrentLib = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.widgetsLib = new System.Windows.Forms.CheckBox();
            this.locationLib = new System.Windows.Forms.CheckBox();
            this.webkitWidgetsLib = new System.Windows.Forms.CheckBox();
            this.sensorsLib = new System.Windows.Forms.CheckBox();
            this.declarativeLib = new System.Windows.Forms.CheckBox();
            this.printSupportLib = new System.Windows.Forms.CheckBox();
            this.bluetoothLib = new System.Windows.Forms.CheckBox();
            this.helpLib = new System.Windows.Forms.CheckBox();
            this.xmlLib = new System.Windows.Forms.CheckBox();
            this.activeQtCLib = new System.Windows.Forms.CheckBox();
            this.activeQtSLib = new System.Windows.Forms.CheckBox();
            this.xmlPatternsLib = new System.Windows.Forms.CheckBox();
            this.openGLLib = new System.Windows.Forms.CheckBox();
            this.scriptLib = new System.Windows.Forms.CheckBox();
            this.scriptToolsLib = new System.Windows.Forms.CheckBox();
            this.uiToolsLib = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // OptionsPropertyGrid
            // 
            this.OptionsPropertyGrid.HelpVisible = false;
            this.OptionsPropertyGrid.Location = new System.Drawing.Point(6, 6);
            this.OptionsPropertyGrid.Name = "OptionsPropertyGrid";
            this.OptionsPropertyGrid.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
            this.OptionsPropertyGrid.Size = new System.Drawing.Size(443, 213);
            this.OptionsPropertyGrid.TabIndex = 8;
            this.OptionsPropertyGrid.ToolbarVisible = false;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.okButton);
            this.panel1.Controls.Add(this.cancelButton);
            this.panel1.Location = new System.Drawing.Point(304, 388);
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
            this.tabControl1.Size = new System.Drawing.Size(463, 344);
            this.tabControl1.TabIndex = 10;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage1.Controls.Add(this.OptionsPropertyGrid);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(455, 318);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General Settings";
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage2.Controls.Add(this.panel2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(455, 318);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Add/Remove Qt Modules";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.uiToolsLib);
            this.panel2.Controls.Add(this.scriptToolsLib);
            this.panel2.Controls.Add(this.quickLib);
            this.panel2.Controls.Add(this.qmlLib);
            this.panel2.Controls.Add(this.webKitLib);
            this.panel2.Controls.Add(this.multimediaLib);
            this.panel2.Controls.Add(this.networkLib);
            this.panel2.Controls.Add(this.coreLib);
            this.panel2.Controls.Add(this.threeDLib);
            this.panel2.Controls.Add(this.guiLib);
            this.panel2.Controls.Add(this.sqlLib);
            this.panel2.Controls.Add(this.testLib);
            this.panel2.Controls.Add(this.svgLib);
            this.panel2.Controls.Add(this.multimediaWidgetsLib);
            this.panel2.Controls.Add(this.concurrentLib);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.widgetsLib);
            this.panel2.Controls.Add(this.locationLib);
            this.panel2.Controls.Add(this.webkitWidgetsLib);
            this.panel2.Controls.Add(this.sensorsLib);
            this.panel2.Controls.Add(this.declarativeLib);
            this.panel2.Controls.Add(this.printSupportLib);
            this.panel2.Controls.Add(this.bluetoothLib);
            this.panel2.Controls.Add(this.helpLib);
            this.panel2.Controls.Add(this.xmlLib);
            this.panel2.Controls.Add(this.activeQtCLib);
            this.panel2.Controls.Add(this.activeQtSLib);
            this.panel2.Controls.Add(this.xmlPatternsLib);
            this.panel2.Controls.Add(this.openGLLib);
            this.panel2.Controls.Add(this.scriptLib);
            this.panel2.Location = new System.Drawing.Point(0, 6);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(449, 303);
            this.panel2.TabIndex = 11;
            // 
            // quickLib
            // 
            this.quickLib.Location = new System.Drawing.Point(149, 104);
            this.quickLib.Name = "quickLib";
            this.quickLib.Size = new System.Drawing.Size(128, 24);
            this.quickLib.TabIndex = 16;
            this.quickLib.UseVisualStyleBackColor = true;
            // 
            // qmlLib
            // 
            this.qmlLib.Location = new System.Drawing.Point(149, 80);
            this.qmlLib.Name = "qmlLib";
            this.qmlLib.Size = new System.Drawing.Size(128, 24);
            this.qmlLib.TabIndex = 15;
            this.qmlLib.UseVisualStyleBackColor = true;
            // 
            // webKitLib
            // 
            this.webKitLib.Location = new System.Drawing.Point(296, 32);
            this.webKitLib.Name = "webKitLib";
            this.webKitLib.Size = new System.Drawing.Size(111, 24);
            this.webKitLib.TabIndex = 25;
            // 
            // multimediaLib
            // 
            this.multimediaLib.Location = new System.Drawing.Point(5, 248);
            this.multimediaLib.Name = "multimediaLib";
            this.multimediaLib.Size = new System.Drawing.Size(128, 24);
            this.multimediaLib.TabIndex = 10;
            // 
            // networkLib
            // 
            this.networkLib.Location = new System.Drawing.Point(149, 8);
            this.networkLib.Name = "networkLib";
            this.networkLib.Size = new System.Drawing.Size(128, 24);
            this.networkLib.TabIndex = 12;
            // 
            // coreLib
            // 
            this.coreLib.Location = new System.Drawing.Point(5, 128);
            this.coreLib.Name = "coreLib";
            this.coreLib.Size = new System.Drawing.Size(128, 24);
            this.coreLib.TabIndex = 5;
            // 
            // threeDLib
            // 
            this.threeDLib.Location = new System.Drawing.Point(5, 8);
            this.threeDLib.Name = "threeDLib";
            this.threeDLib.Size = new System.Drawing.Size(128, 24);
            this.threeDLib.TabIndex = 0;
            this.threeDLib.UseVisualStyleBackColor = true;
            // 
            // guiLib
            // 
            this.guiLib.Location = new System.Drawing.Point(5, 176);
            this.guiLib.Name = "guiLib";
            this.guiLib.Size = new System.Drawing.Size(135, 24);
            this.guiLib.TabIndex = 7;
            // 
            // sqlLib
            // 
            this.sqlLib.Location = new System.Drawing.Point(149, 200);
            this.sqlLib.Name = "sqlLib";
            this.sqlLib.Size = new System.Drawing.Size(111, 24);
            this.sqlLib.TabIndex = 20;
            // 
            // testLib
            // 
            this.testLib.Location = new System.Drawing.Point(296, 8);
            this.testLib.Name = "testLib";
            this.testLib.Size = new System.Drawing.Size(111, 24);
            this.testLib.TabIndex = 24;
            // 
            // svgLib
            // 
            this.svgLib.Location = new System.Drawing.Point(149, 271);
            this.svgLib.Name = "svgLib";
            this.svgLib.Size = new System.Drawing.Size(118, 24);
            this.svgLib.TabIndex = 23;
            // 
            // multimediaWidgetsLib
            // 
            this.multimediaWidgetsLib.Location = new System.Drawing.Point(5, 276);
            this.multimediaWidgetsLib.Name = "multimediaWidgetsLib";
            this.multimediaWidgetsLib.Size = new System.Drawing.Size(118, 24);
            this.multimediaWidgetsLib.TabIndex = 11;
            this.multimediaWidgetsLib.UseVisualStyleBackColor = true;
            // 
            // concurrentLib
            // 
            this.concurrentLib.Location = new System.Drawing.Point(5, 104);
            this.concurrentLib.Name = "concurrentLib";
            this.concurrentLib.Size = new System.Drawing.Size(128, 24);
            this.concurrentLib.TabIndex = 4;
            this.concurrentLib.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label1.Location = new System.Drawing.Point(2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(443, 1);
            this.label1.TabIndex = 21;
            // 
            // widgetsLib
            // 
            this.widgetsLib.Location = new System.Drawing.Point(296, 80);
            this.widgetsLib.Name = "widgetsLib";
            this.widgetsLib.Size = new System.Drawing.Size(135, 24);
            this.widgetsLib.TabIndex = 27;
            this.widgetsLib.UseVisualStyleBackColor = true;
            // 
            // locationLib
            // 
            this.locationLib.Location = new System.Drawing.Point(5, 224);
            this.locationLib.Name = "locationLib";
            this.locationLib.Size = new System.Drawing.Size(128, 24);
            this.locationLib.TabIndex = 9;
            this.locationLib.UseVisualStyleBackColor = true;
            // 
            // webkitWidgetsLib
            // 
            this.webkitWidgetsLib.Location = new System.Drawing.Point(296, 56);
            this.webkitWidgetsLib.Name = "webkitWidgetsLib";
            this.webkitWidgetsLib.Size = new System.Drawing.Size(135, 24);
            this.webkitWidgetsLib.TabIndex = 26;
            this.webkitWidgetsLib.UseVisualStyleBackColor = true;
            // 
            // sensorsLib
            // 
            this.sensorsLib.Location = new System.Drawing.Point(149, 176);
            this.sensorsLib.Name = "sensorsLib";
            this.sensorsLib.Size = new System.Drawing.Size(135, 24);
            this.sensorsLib.TabIndex = 19;
            this.sensorsLib.UseVisualStyleBackColor = true;
            // 
            // declarativeLib
            // 
            this.declarativeLib.Location = new System.Drawing.Point(5, 152);
            this.declarativeLib.Name = "declarativeLib";
            this.declarativeLib.Size = new System.Drawing.Size(135, 24);
            this.declarativeLib.TabIndex = 6;
            this.declarativeLib.UseVisualStyleBackColor = true;
            // 
            // printSupportLib
            // 
            this.printSupportLib.Location = new System.Drawing.Point(149, 56);
            this.printSupportLib.Name = "printSupportLib";
            this.printSupportLib.Size = new System.Drawing.Size(135, 24);
            this.printSupportLib.TabIndex = 14;
            this.printSupportLib.UseVisualStyleBackColor = true;
            // 
            // bluetoothLib
            // 
            this.bluetoothLib.Location = new System.Drawing.Point(5, 80);
            this.bluetoothLib.Name = "bluetoothLib";
            this.bluetoothLib.Size = new System.Drawing.Size(137, 24);
            this.bluetoothLib.TabIndex = 3;
            this.bluetoothLib.UseVisualStyleBackColor = true;
            // 
            // helpLib
            // 
            this.helpLib.Location = new System.Drawing.Point(5, 200);
            this.helpLib.Name = "helpLib";
            this.helpLib.Size = new System.Drawing.Size(128, 24);
            this.helpLib.TabIndex = 8;
            // 
            // xmlLib
            // 
            this.xmlLib.Location = new System.Drawing.Point(296, 104);
            this.xmlLib.Name = "xmlLib";
            this.xmlLib.Size = new System.Drawing.Size(118, 24);
            this.xmlLib.TabIndex = 28;
            // 
            // activeQtCLib
            // 
            this.activeQtCLib.Location = new System.Drawing.Point(5, 32);
            this.activeQtCLib.Name = "activeQtCLib";
            this.activeQtCLib.Size = new System.Drawing.Size(128, 24);
            this.activeQtCLib.TabIndex = 1;
            // 
            // activeQtSLib
            // 
            this.activeQtSLib.Location = new System.Drawing.Point(5, 55);
            this.activeQtSLib.Name = "activeQtSLib";
            this.activeQtSLib.Size = new System.Drawing.Size(120, 24);
            this.activeQtSLib.TabIndex = 2;
            // 
            // xmlPatternsLib
            // 
            this.xmlPatternsLib.Location = new System.Drawing.Point(149, 247);
            this.xmlPatternsLib.Name = "xmlPatternsLib";
            this.xmlPatternsLib.Size = new System.Drawing.Size(118, 24);
            this.xmlPatternsLib.TabIndex = 22;
            // 
            // openGLLib
            // 
            this.openGLLib.Location = new System.Drawing.Point(149, 32);
            this.openGLLib.Name = "openGLLib";
            this.openGLLib.Size = new System.Drawing.Size(120, 24);
            this.openGLLib.TabIndex = 13;
            // 
            // scriptLib
            // 
            this.scriptLib.Location = new System.Drawing.Point(149, 128);
            this.scriptLib.Name = "scriptLib";
            this.scriptLib.Size = new System.Drawing.Size(128, 24);
            this.scriptLib.TabIndex = 17;
            // 
            // scriptToolsLib
            // 
            this.scriptToolsLib.Location = new System.Drawing.Point(149, 152);
            this.scriptToolsLib.Name = "scriptToolsLib";
            this.scriptToolsLib.Size = new System.Drawing.Size(135, 24);
            this.scriptToolsLib.TabIndex = 18;
            this.scriptToolsLib.UseVisualStyleBackColor = true;
            // 
            // uiToolsLib
            // 
            this.uiToolsLib.Location = new System.Drawing.Point(149, 223);
            this.uiToolsLib.Name = "uiToolsLib";
            this.uiToolsLib.Size = new System.Drawing.Size(135, 24);
            this.uiToolsLib.TabIndex = 21;
            this.uiToolsLib.UseVisualStyleBackColor = true;
            // 
            // FormProjectQtSettings
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(481, 438);
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
        private System.Windows.Forms.CheckBox webKitLib;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.CheckBox networkLib;
        private System.Windows.Forms.CheckBox coreLib;
        private System.Windows.Forms.CheckBox guiLib;
        private System.Windows.Forms.CheckBox sqlLib;
        private System.Windows.Forms.CheckBox testLib;
        private System.Windows.Forms.CheckBox multimediaLib;
        private System.Windows.Forms.CheckBox quickLib;
        private System.Windows.Forms.CheckBox qmlLib;
        private System.Windows.Forms.CheckBox multimediaWidgetsLib;
        private System.Windows.Forms.CheckBox concurrentLib;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox widgetsLib;
        private System.Windows.Forms.CheckBox locationLib;
        private System.Windows.Forms.CheckBox webkitWidgetsLib;
        private System.Windows.Forms.CheckBox sensorsLib;
        private System.Windows.Forms.CheckBox declarativeLib;
        private System.Windows.Forms.CheckBox printSupportLib;
        private System.Windows.Forms.CheckBox bluetoothLib;
        private System.Windows.Forms.CheckBox helpLib;
        private System.Windows.Forms.CheckBox xmlLib;
        private System.Windows.Forms.CheckBox activeQtCLib;
        private System.Windows.Forms.CheckBox activeQtSLib;
        private System.Windows.Forms.CheckBox xmlPatternsLib;
        private System.Windows.Forms.CheckBox openGLLib;
        private System.Windows.Forms.CheckBox scriptLib;
        private System.Windows.Forms.CheckBox threeDLib;
        private System.Windows.Forms.CheckBox svgLib;
        private System.Windows.Forms.CheckBox uiToolsLib;
        private System.Windows.Forms.CheckBox scriptToolsLib;
    }
}