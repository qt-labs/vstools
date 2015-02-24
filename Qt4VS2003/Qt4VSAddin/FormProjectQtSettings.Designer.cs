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
            this.uiToolsLib = new System.Windows.Forms.CheckBox();
            this.scriptToolsLib = new System.Windows.Forms.CheckBox();
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
            this.enginioLib = new System.Windows.Forms.CheckBox();
            this.nfcLib = new System.Windows.Forms.CheckBox();
            this.positioningLib = new System.Windows.Forms.CheckBox();
            this.serialPortLib = new System.Windows.Forms.CheckBox();
            this.webChannelLib = new System.Windows.Forms.CheckBox();
            this.webSocketsLib = new System.Windows.Forms.CheckBox();
            this.windowsExtrasLib = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
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
            this.tabPage2.Controls.Add(this.windowsExtrasLib);
            this.tabPage2.Controls.Add(this.webSocketsLib);
            this.tabPage2.Controls.Add(this.webChannelLib);
            this.tabPage2.Controls.Add(this.serialPortLib);
            this.tabPage2.Controls.Add(this.positioningLib);
            this.tabPage2.Controls.Add(this.nfcLib);
            this.tabPage2.Controls.Add(this.enginioLib);
            this.tabPage2.Controls.Add(this.uiToolsLib);
            this.tabPage2.Controls.Add(this.scriptToolsLib);
            this.tabPage2.Controls.Add(this.quickLib);
            this.tabPage2.Controls.Add(this.qmlLib);
            this.tabPage2.Controls.Add(this.webKitLib);
            this.tabPage2.Controls.Add(this.multimediaLib);
            this.tabPage2.Controls.Add(this.networkLib);
            this.tabPage2.Controls.Add(this.coreLib);
            this.tabPage2.Controls.Add(this.threeDLib);
            this.tabPage2.Controls.Add(this.guiLib);
            this.tabPage2.Controls.Add(this.sqlLib);
            this.tabPage2.Controls.Add(this.testLib);
            this.tabPage2.Controls.Add(this.svgLib);
            this.tabPage2.Controls.Add(this.multimediaWidgetsLib);
            this.tabPage2.Controls.Add(this.concurrentLib);
            this.tabPage2.Controls.Add(this.widgetsLib);
            this.tabPage2.Controls.Add(this.locationLib);
            this.tabPage2.Controls.Add(this.webkitWidgetsLib);
            this.tabPage2.Controls.Add(this.sensorsLib);
            this.tabPage2.Controls.Add(this.declarativeLib);
            this.tabPage2.Controls.Add(this.printSupportLib);
            this.tabPage2.Controls.Add(this.bluetoothLib);
            this.tabPage2.Controls.Add(this.helpLib);
            this.tabPage2.Controls.Add(this.xmlLib);
            this.tabPage2.Controls.Add(this.activeQtCLib);
            this.tabPage2.Controls.Add(this.activeQtSLib);
            this.tabPage2.Controls.Add(this.xmlPatternsLib);
            this.tabPage2.Controls.Add(this.openGLLib);
            this.tabPage2.Controls.Add(this.scriptLib);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(455, 318);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Add/Remove Qt Modules";
            // 
            // uiToolsLib
            // 
            this.uiToolsLib.Location = new System.Drawing.Point(294, 27);
            this.uiToolsLib.Name = "uiToolsLib";
            this.uiToolsLib.Size = new System.Drawing.Size(135, 24);
            this.uiToolsLib.TabIndex = 56;
            this.uiToolsLib.UseVisualStyleBackColor = true;
            // 
            // scriptToolsLib
            // 
            this.scriptToolsLib.Location = new System.Drawing.Point(150, 195);
            this.scriptToolsLib.Name = "scriptToolsLib";
            this.scriptToolsLib.Size = new System.Drawing.Size(135, 24);
            this.scriptToolsLib.TabIndex = 50;
            this.scriptToolsLib.UseVisualStyleBackColor = true;
            // 
            // quickLib
            // 
            this.quickLib.Location = new System.Drawing.Point(150, 147);
            this.quickLib.Name = "quickLib";
            this.quickLib.Size = new System.Drawing.Size(128, 24);
            this.quickLib.TabIndex = 48;
            this.quickLib.UseVisualStyleBackColor = true;
            // 
            // qmlLib
            // 
            this.qmlLib.Location = new System.Drawing.Point(150, 123);
            this.qmlLib.Name = "qmlLib";
            this.qmlLib.Size = new System.Drawing.Size(128, 24);
            this.qmlLib.TabIndex = 47;
            this.qmlLib.UseVisualStyleBackColor = true;
            // 
            // webKitLib
            // 
            this.webKitLib.Location = new System.Drawing.Point(294, 75);
            this.webKitLib.Name = "webKitLib";
            this.webKitLib.Size = new System.Drawing.Size(111, 24);
            this.webKitLib.TabIndex = 58;
            // 
            // multimediaLib
            // 
            this.multimediaLib.Location = new System.Drawing.Point(6, 267);
            this.multimediaLib.Name = "multimediaLib";
            this.multimediaLib.Size = new System.Drawing.Size(128, 24);
            this.multimediaLib.TabIndex = 40;
            // 
            // networkLib
            // 
            this.networkLib.Location = new System.Drawing.Point(150, 3);
            this.networkLib.Name = "networkLib";
            this.networkLib.Size = new System.Drawing.Size(128, 24);
            this.networkLib.TabIndex = 42;
            // 
            // coreLib
            // 
            this.coreLib.Location = new System.Drawing.Point(6, 123);
            this.coreLib.Name = "coreLib";
            this.coreLib.Size = new System.Drawing.Size(128, 24);
            this.coreLib.TabIndex = 34;
            // 
            // threeDLib
            // 
            this.threeDLib.Location = new System.Drawing.Point(6, 3);
            this.threeDLib.Name = "threeDLib";
            this.threeDLib.Size = new System.Drawing.Size(128, 24);
            this.threeDLib.TabIndex = 29;
            this.threeDLib.UseVisualStyleBackColor = true;
            // 
            // guiLib
            // 
            this.guiLib.Location = new System.Drawing.Point(6, 195);
            this.guiLib.Name = "guiLib";
            this.guiLib.Size = new System.Drawing.Size(135, 24);
            this.guiLib.TabIndex = 37;
            // 
            // sqlLib
            // 
            this.sqlLib.Location = new System.Drawing.Point(150, 267);
            this.sqlLib.Name = "sqlLib";
            this.sqlLib.Size = new System.Drawing.Size(111, 24);
            this.sqlLib.TabIndex = 53;
            // 
            // testLib
            // 
            this.testLib.Location = new System.Drawing.Point(294, 3);
            this.testLib.Name = "testLib";
            this.testLib.Size = new System.Drawing.Size(111, 24);
            this.testLib.TabIndex = 55;
            // 
            // svgLib
            // 
            this.svgLib.Location = new System.Drawing.Point(150, 291);
            this.svgLib.Name = "svgLib";
            this.svgLib.Size = new System.Drawing.Size(118, 24);
            this.svgLib.TabIndex = 54;
            // 
            // multimediaWidgetsLib
            // 
            this.multimediaWidgetsLib.Location = new System.Drawing.Point(6, 291);
            this.multimediaWidgetsLib.Name = "multimediaWidgetsLib";
            this.multimediaWidgetsLib.Size = new System.Drawing.Size(118, 24);
            this.multimediaWidgetsLib.TabIndex = 41;
            this.multimediaWidgetsLib.UseVisualStyleBackColor = true;
            // 
            // concurrentLib
            // 
            this.concurrentLib.Location = new System.Drawing.Point(6, 99);
            this.concurrentLib.Name = "concurrentLib";
            this.concurrentLib.Size = new System.Drawing.Size(128, 24);
            this.concurrentLib.TabIndex = 33;
            this.concurrentLib.UseVisualStyleBackColor = true;
            // 
            // widgetsLib
            // 
            this.widgetsLib.Location = new System.Drawing.Point(294, 147);
            this.widgetsLib.Name = "widgetsLib";
            this.widgetsLib.Size = new System.Drawing.Size(135, 24);
            this.widgetsLib.TabIndex = 61;
            this.widgetsLib.UseVisualStyleBackColor = true;
            // 
            // locationLib
            // 
            this.locationLib.Location = new System.Drawing.Point(6, 243);
            this.locationLib.Name = "locationLib";
            this.locationLib.Size = new System.Drawing.Size(128, 24);
            this.locationLib.TabIndex = 39;
            this.locationLib.UseVisualStyleBackColor = true;
            // 
            // webkitWidgetsLib
            // 
            this.webkitWidgetsLib.Location = new System.Drawing.Point(294, 99);
            this.webkitWidgetsLib.Name = "webkitWidgetsLib";
            this.webkitWidgetsLib.Size = new System.Drawing.Size(135, 24);
            this.webkitWidgetsLib.TabIndex = 59;
            this.webkitWidgetsLib.UseVisualStyleBackColor = true;
            // 
            // sensorsLib
            // 
            this.sensorsLib.Location = new System.Drawing.Point(150, 219);
            this.sensorsLib.Name = "sensorsLib";
            this.sensorsLib.Size = new System.Drawing.Size(135, 24);
            this.sensorsLib.TabIndex = 51;
            this.sensorsLib.UseVisualStyleBackColor = true;
            // 
            // declarativeLib
            // 
            this.declarativeLib.Location = new System.Drawing.Point(6, 171);
            this.declarativeLib.Name = "declarativeLib";
            this.declarativeLib.Size = new System.Drawing.Size(135, 24);
            this.declarativeLib.TabIndex = 36;
            this.declarativeLib.UseVisualStyleBackColor = true;
            // 
            // printSupportLib
            // 
            this.printSupportLib.Location = new System.Drawing.Point(150, 99);
            this.printSupportLib.Name = "printSupportLib";
            this.printSupportLib.Size = new System.Drawing.Size(135, 24);
            this.printSupportLib.TabIndex = 46;
            this.printSupportLib.UseVisualStyleBackColor = true;
            // 
            // bluetoothLib
            // 
            this.bluetoothLib.Location = new System.Drawing.Point(6, 75);
            this.bluetoothLib.Name = "bluetoothLib";
            this.bluetoothLib.Size = new System.Drawing.Size(137, 24);
            this.bluetoothLib.TabIndex = 32;
            this.bluetoothLib.UseVisualStyleBackColor = true;
            // 
            // helpLib
            // 
            this.helpLib.Location = new System.Drawing.Point(6, 219);
            this.helpLib.Name = "helpLib";
            this.helpLib.Size = new System.Drawing.Size(128, 24);
            this.helpLib.TabIndex = 38;
            // 
            // xmlLib
            // 
            this.xmlLib.Location = new System.Drawing.Point(294, 195);
            this.xmlLib.Name = "xmlLib";
            this.xmlLib.Size = new System.Drawing.Size(118, 24);
            this.xmlLib.TabIndex = 63;
            // 
            // activeQtCLib
            // 
            this.activeQtCLib.Location = new System.Drawing.Point(6, 27);
            this.activeQtCLib.Name = "activeQtCLib";
            this.activeQtCLib.Size = new System.Drawing.Size(128, 24);
            this.activeQtCLib.TabIndex = 30;
            // 
            // activeQtSLib
            // 
            this.activeQtSLib.Location = new System.Drawing.Point(6, 51);
            this.activeQtSLib.Name = "activeQtSLib";
            this.activeQtSLib.Size = new System.Drawing.Size(120, 24);
            this.activeQtSLib.TabIndex = 31;
            // 
            // xmlPatternsLib
            // 
            this.xmlPatternsLib.Location = new System.Drawing.Point(294, 219);
            this.xmlPatternsLib.Name = "xmlPatternsLib";
            this.xmlPatternsLib.Size = new System.Drawing.Size(118, 24);
            this.xmlPatternsLib.TabIndex = 64;
            // 
            // openGLLib
            // 
            this.openGLLib.Location = new System.Drawing.Point(150, 51);
            this.openGLLib.Name = "openGLLib";
            this.openGLLib.Size = new System.Drawing.Size(120, 24);
            this.openGLLib.TabIndex = 44;
            // 
            // scriptLib
            // 
            this.scriptLib.Location = new System.Drawing.Point(150, 171);
            this.scriptLib.Name = "scriptLib";
            this.scriptLib.Size = new System.Drawing.Size(128, 24);
            this.scriptLib.TabIndex = 49;
            // 
            // enginioLib
            // 
            this.enginioLib.Location = new System.Drawing.Point(6, 147);
            this.enginioLib.Name = "enginioLib";
            this.enginioLib.Size = new System.Drawing.Size(128, 24);
            this.enginioLib.TabIndex = 35;
            // 
            // nfcLib
            // 
            this.nfcLib.Location = new System.Drawing.Point(150, 27);
            this.nfcLib.Name = "nfcLib";
            this.nfcLib.Size = new System.Drawing.Size(128, 24);
            this.nfcLib.TabIndex = 43;
            // 
            // positioningLib
            // 
            this.positioningLib.Location = new System.Drawing.Point(150, 75);
            this.positioningLib.Name = "positioningLib";
            this.positioningLib.Size = new System.Drawing.Size(120, 24);
            this.positioningLib.TabIndex = 45;
            // 
            // serialPortLib
            // 
            this.serialPortLib.Location = new System.Drawing.Point(150, 243);
            this.serialPortLib.Name = "serialPortLib";
            this.serialPortLib.Size = new System.Drawing.Size(135, 24);
            this.serialPortLib.TabIndex = 52;
            this.serialPortLib.UseVisualStyleBackColor = true;
            // 
            // webChannelLib
            // 
            this.webChannelLib.Location = new System.Drawing.Point(294, 51);
            this.webChannelLib.Name = "webChannelLib";
            this.webChannelLib.Size = new System.Drawing.Size(111, 24);
            this.webChannelLib.TabIndex = 57;
            // 
            // webSocketsLib
            // 
            this.webSocketsLib.Location = new System.Drawing.Point(294, 123);
            this.webSocketsLib.Name = "webSocketsLib";
            this.webSocketsLib.Size = new System.Drawing.Size(135, 24);
            this.webSocketsLib.TabIndex = 60;
            this.webSocketsLib.UseVisualStyleBackColor = true;
            // 
            // windowsExtrasLib
            // 
            this.windowsExtrasLib.Location = new System.Drawing.Point(294, 171);
            this.windowsExtrasLib.Name = "windowsExtrasLib";
            this.windowsExtrasLib.Size = new System.Drawing.Size(135, 24);
            this.windowsExtrasLib.TabIndex = 62;
            this.windowsExtrasLib.UseVisualStyleBackColor = true;
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
        private System.Windows.Forms.CheckBox uiToolsLib;
        private System.Windows.Forms.CheckBox scriptToolsLib;
        private System.Windows.Forms.CheckBox quickLib;
        private System.Windows.Forms.CheckBox qmlLib;
        private System.Windows.Forms.CheckBox webKitLib;
        private System.Windows.Forms.CheckBox multimediaLib;
        private System.Windows.Forms.CheckBox networkLib;
        private System.Windows.Forms.CheckBox coreLib;
        private System.Windows.Forms.CheckBox threeDLib;
        private System.Windows.Forms.CheckBox guiLib;
        private System.Windows.Forms.CheckBox sqlLib;
        private System.Windows.Forms.CheckBox testLib;
        private System.Windows.Forms.CheckBox svgLib;
        private System.Windows.Forms.CheckBox multimediaWidgetsLib;
        private System.Windows.Forms.CheckBox concurrentLib;
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
        private System.Windows.Forms.CheckBox enginioLib;
        private System.Windows.Forms.CheckBox nfcLib;
        private System.Windows.Forms.CheckBox positioningLib;
        private System.Windows.Forms.CheckBox serialPortLib;
        private System.Windows.Forms.CheckBox webChannelLib;
        private System.Windows.Forms.CheckBox webSocketsLib;
        private System.Windows.Forms.CheckBox windowsExtrasLib;
    }
}