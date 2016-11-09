using System.Windows.Forms;

namespace QtVsTools
{
    partial class FormProjectQtSettings
    {

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            OptionsPropertyGrid = new PropertyGrid();
            panel1 = new Panel();
            okButton = new Button();
            cancelButton = new Button();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            quickWidgetsLib = new CheckBox();
            windowsExtrasLib = new CheckBox();
            webSocketsLib = new CheckBox();
            webChannelLib = new CheckBox();
            serialPortLib = new CheckBox();
            positioningLib = new CheckBox();
            nfcLib = new CheckBox();
            enginioLib = new CheckBox();
            uiToolsLib = new CheckBox();
            scriptToolsLib = new CheckBox();
            quickLib = new CheckBox();
            qmlLib = new CheckBox();
            webKitLib = new CheckBox();
            multimediaLib = new CheckBox();
            networkLib = new CheckBox();
            coreLib = new CheckBox();
            threeDLib = new CheckBox();
            guiLib = new CheckBox();
            sqlLib = new CheckBox();
            testLib = new CheckBox();
            svgLib = new CheckBox();
            multimediaWidgetsLib = new CheckBox();
            concurrentLib = new CheckBox();
            widgetsLib = new CheckBox();
            locationLib = new CheckBox();
            webkitWidgetsLib = new CheckBox();
            sensorsLib = new CheckBox();
            declarativeLib = new CheckBox();
            printSupportLib = new CheckBox();
            bluetoothLib = new CheckBox();
            helpLib = new CheckBox();
            xmlLib = new CheckBox();
            activeQtCLib = new CheckBox();
            activeQtSLib = new CheckBox();
            xmlPatternsLib = new CheckBox();
            openGLLib = new CheckBox();
            scriptLib = new CheckBox();
            panel1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            SuspendLayout();
            //
            // OptionsPropertyGrid
            //
            OptionsPropertyGrid.HelpVisible = false;
            OptionsPropertyGrid.Location = new System.Drawing.Point(6, 6);
            OptionsPropertyGrid.Name = "OptionsPropertyGrid";
            OptionsPropertyGrid.PropertySort = PropertySort.Alphabetical;
            OptionsPropertyGrid.Size = new System.Drawing.Size(443, 213);
            OptionsPropertyGrid.TabIndex = 8;
            OptionsPropertyGrid.ToolbarVisible = false;
            //
            // panel1
            //
            panel1.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            panel1.Controls.Add(okButton);
            panel1.Controls.Add(cancelButton);
            panel1.Location = new System.Drawing.Point(304, 388);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(168, 38);
            panel1.TabIndex = 9;
            //
            // okButton
            //
            okButton.Location = new System.Drawing.Point(8, 8);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(75, 23);
            okButton.TabIndex = 0;
            okButton.Click += okButton_Click;
            //
            // cancelButton
            //
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(88, 8);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(75, 23);
            cancelButton.TabIndex = 1;
            //
            // tabControl1
            //
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Location = new System.Drawing.Point(12, 12);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new System.Drawing.Size(463, 344);
            tabControl1.TabIndex = 10;
            //
            // tabPage1
            //
            tabPage1.BackColor = System.Drawing.SystemColors.Control;
            tabPage1.Controls.Add(OptionsPropertyGrid);
            tabPage1.Location = new System.Drawing.Point(4, 22);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new System.Drawing.Size(455, 318);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "General Settings";
            //
            // tabPage2
            //
            tabPage2.BackColor = System.Drawing.SystemColors.Control;
            tabPage2.Controls.Add(quickWidgetsLib);
            tabPage2.Controls.Add(windowsExtrasLib);
            tabPage2.Controls.Add(webSocketsLib);
            tabPage2.Controls.Add(webChannelLib);
            tabPage2.Controls.Add(serialPortLib);
            tabPage2.Controls.Add(positioningLib);
            tabPage2.Controls.Add(nfcLib);
            tabPage2.Controls.Add(enginioLib);
            tabPage2.Controls.Add(uiToolsLib);
            tabPage2.Controls.Add(scriptToolsLib);
            tabPage2.Controls.Add(quickLib);
            tabPage2.Controls.Add(qmlLib);
            tabPage2.Controls.Add(webKitLib);
            tabPage2.Controls.Add(multimediaLib);
            tabPage2.Controls.Add(networkLib);
            tabPage2.Controls.Add(coreLib);
            tabPage2.Controls.Add(threeDLib);
            tabPage2.Controls.Add(guiLib);
            tabPage2.Controls.Add(sqlLib);
            tabPage2.Controls.Add(testLib);
            tabPage2.Controls.Add(svgLib);
            tabPage2.Controls.Add(multimediaWidgetsLib);
            tabPage2.Controls.Add(concurrentLib);
            tabPage2.Controls.Add(widgetsLib);
            tabPage2.Controls.Add(locationLib);
            tabPage2.Controls.Add(webkitWidgetsLib);
            tabPage2.Controls.Add(sensorsLib);
            tabPage2.Controls.Add(declarativeLib);
            tabPage2.Controls.Add(printSupportLib);
            tabPage2.Controls.Add(bluetoothLib);
            tabPage2.Controls.Add(helpLib);
            tabPage2.Controls.Add(xmlLib);
            tabPage2.Controls.Add(activeQtCLib);
            tabPage2.Controls.Add(activeQtSLib);
            tabPage2.Controls.Add(xmlPatternsLib);
            tabPage2.Controls.Add(openGLLib);
            tabPage2.Controls.Add(scriptLib);
            tabPage2.Location = new System.Drawing.Point(4, 22);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new System.Drawing.Size(455, 318);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Add/Remove Qt Modules";
            //
            // quickWidgetsLib
            //
            quickWidgetsLib.Location = new System.Drawing.Point(150, 171);
            quickWidgetsLib.Name = "quickWidgetsLib";
            quickWidgetsLib.Size = new System.Drawing.Size(128, 24);
            quickWidgetsLib.TabIndex = 49;
            quickWidgetsLib.UseVisualStyleBackColor = true;
            //
            // windowsExtrasLib
            //
            windowsExtrasLib.Location = new System.Drawing.Point(294, 195);
            windowsExtrasLib.Name = "windowsExtrasLib";
            windowsExtrasLib.Size = new System.Drawing.Size(135, 24);
            windowsExtrasLib.TabIndex = 63;
            windowsExtrasLib.UseVisualStyleBackColor = true;
            //
            // webSocketsLib
            //
            webSocketsLib.Location = new System.Drawing.Point(294, 147);
            webSocketsLib.Name = "webSocketsLib";
            webSocketsLib.Size = new System.Drawing.Size(135, 24);
            webSocketsLib.TabIndex = 61;
            webSocketsLib.UseVisualStyleBackColor = true;
            //
            // webChannelLib
            //
            webChannelLib.Location = new System.Drawing.Point(294, 75);
            webChannelLib.Name = "webChannelLib";
            webChannelLib.Size = new System.Drawing.Size(111, 24);
            webChannelLib.TabIndex = 58;
            //
            // serialPortLib
            //
            serialPortLib.Location = new System.Drawing.Point(150, 267);
            serialPortLib.Name = "serialPortLib";
            serialPortLib.Size = new System.Drawing.Size(135, 24);
            serialPortLib.TabIndex = 53;
            serialPortLib.UseVisualStyleBackColor = true;
            //
            // positioningLib
            //
            positioningLib.Location = new System.Drawing.Point(150, 75);
            positioningLib.Name = "positioningLib";
            positioningLib.Size = new System.Drawing.Size(120, 24);
            positioningLib.TabIndex = 45;
            //
            // nfcLib
            //
            nfcLib.Location = new System.Drawing.Point(150, 27);
            nfcLib.Name = "nfcLib";
            nfcLib.Size = new System.Drawing.Size(128, 24);
            nfcLib.TabIndex = 43;
            //
            // enginioLib
            //
            enginioLib.Location = new System.Drawing.Point(6, 147);
            enginioLib.Name = "enginioLib";
            enginioLib.Size = new System.Drawing.Size(128, 24);
            enginioLib.TabIndex = 35;
            //
            // uiToolsLib
            //
            uiToolsLib.Location = new System.Drawing.Point(294, 51);
            uiToolsLib.Name = "uiToolsLib";
            uiToolsLib.Size = new System.Drawing.Size(135, 24);
            uiToolsLib.TabIndex = 57;
            uiToolsLib.UseVisualStyleBackColor = true;
            //
            // scriptToolsLib
            //
            scriptToolsLib.Location = new System.Drawing.Point(150, 219);
            scriptToolsLib.Name = "scriptToolsLib";
            scriptToolsLib.Size = new System.Drawing.Size(135, 24);
            scriptToolsLib.TabIndex = 51;
            scriptToolsLib.UseVisualStyleBackColor = true;
            //
            // quickLib
            //
            quickLib.Location = new System.Drawing.Point(150, 147);
            quickLib.Name = "quickLib";
            quickLib.Size = new System.Drawing.Size(128, 24);
            quickLib.TabIndex = 48;
            quickLib.UseVisualStyleBackColor = true;
            //
            // qmlLib
            //
            qmlLib.Location = new System.Drawing.Point(150, 123);
            qmlLib.Name = "qmlLib";
            qmlLib.Size = new System.Drawing.Size(128, 24);
            qmlLib.TabIndex = 47;
            qmlLib.UseVisualStyleBackColor = true;
            //
            // webKitLib
            //
            webKitLib.Location = new System.Drawing.Point(294, 99);
            webKitLib.Name = "webKitLib";
            webKitLib.Size = new System.Drawing.Size(111, 24);
            webKitLib.TabIndex = 59;
            //
            // multimediaLib
            //
            multimediaLib.Location = new System.Drawing.Point(6, 267);
            multimediaLib.Name = "multimediaLib";
            multimediaLib.Size = new System.Drawing.Size(128, 24);
            multimediaLib.TabIndex = 40;
            //
            // networkLib
            //
            networkLib.Location = new System.Drawing.Point(150, 3);
            networkLib.Name = "networkLib";
            networkLib.Size = new System.Drawing.Size(128, 24);
            networkLib.TabIndex = 42;
            //
            // coreLib
            //
            coreLib.Location = new System.Drawing.Point(6, 123);
            coreLib.Name = "coreLib";
            coreLib.Size = new System.Drawing.Size(128, 24);
            coreLib.TabIndex = 34;
            //
            // threeDLib
            //
            threeDLib.Location = new System.Drawing.Point(6, 3);
            threeDLib.Name = "threeDLib";
            threeDLib.Size = new System.Drawing.Size(128, 24);
            threeDLib.TabIndex = 29;
            threeDLib.UseVisualStyleBackColor = true;
            //
            // guiLib
            //
            guiLib.Location = new System.Drawing.Point(6, 195);
            guiLib.Name = "guiLib";
            guiLib.Size = new System.Drawing.Size(135, 24);
            guiLib.TabIndex = 37;
            //
            // sqlLib
            //
            sqlLib.Location = new System.Drawing.Point(150, 291);
            sqlLib.Name = "sqlLib";
            sqlLib.Size = new System.Drawing.Size(111, 24);
            sqlLib.TabIndex = 54;
            //
            // testLib
            //
            testLib.Location = new System.Drawing.Point(294, 27);
            testLib.Name = "testLib";
            testLib.Size = new System.Drawing.Size(111, 24);
            testLib.TabIndex = 56;
            //
            // svgLib
            //
            svgLib.Location = new System.Drawing.Point(294, 3);
            svgLib.Name = "svgLib";
            svgLib.Size = new System.Drawing.Size(118, 24);
            svgLib.TabIndex = 55;
            //
            // multimediaWidgetsLib
            //
            multimediaWidgetsLib.Location = new System.Drawing.Point(6, 291);
            multimediaWidgetsLib.Name = "multimediaWidgetsLib";
            multimediaWidgetsLib.Size = new System.Drawing.Size(118, 24);
            multimediaWidgetsLib.TabIndex = 41;
            multimediaWidgetsLib.UseVisualStyleBackColor = true;
            //
            // concurrentLib
            //
            concurrentLib.Location = new System.Drawing.Point(6, 99);
            concurrentLib.Name = "concurrentLib";
            concurrentLib.Size = new System.Drawing.Size(128, 24);
            concurrentLib.TabIndex = 33;
            concurrentLib.UseVisualStyleBackColor = true;
            //
            // widgetsLib
            //
            widgetsLib.Location = new System.Drawing.Point(294, 171);
            widgetsLib.Name = "widgetsLib";
            widgetsLib.Size = new System.Drawing.Size(135, 24);
            widgetsLib.TabIndex = 62;
            widgetsLib.UseVisualStyleBackColor = true;
            //
            // locationLib
            //
            locationLib.Location = new System.Drawing.Point(6, 243);
            locationLib.Name = "locationLib";
            locationLib.Size = new System.Drawing.Size(128, 24);
            locationLib.TabIndex = 39;
            locationLib.UseVisualStyleBackColor = true;
            //
            // webkitWidgetsLib
            //
            webkitWidgetsLib.Location = new System.Drawing.Point(294, 123);
            webkitWidgetsLib.Name = "webkitWidgetsLib";
            webkitWidgetsLib.Size = new System.Drawing.Size(135, 24);
            webkitWidgetsLib.TabIndex = 60;
            webkitWidgetsLib.UseVisualStyleBackColor = true;
            //
            // sensorsLib
            //
            sensorsLib.Location = new System.Drawing.Point(150, 243);
            sensorsLib.Name = "sensorsLib";
            sensorsLib.Size = new System.Drawing.Size(135, 24);
            sensorsLib.TabIndex = 52;
            sensorsLib.UseVisualStyleBackColor = true;
            //
            // declarativeLib
            //
            declarativeLib.Location = new System.Drawing.Point(6, 171);
            declarativeLib.Name = "declarativeLib";
            declarativeLib.Size = new System.Drawing.Size(135, 24);
            declarativeLib.TabIndex = 36;
            declarativeLib.UseVisualStyleBackColor = true;
            //
            // printSupportLib
            //
            printSupportLib.Location = new System.Drawing.Point(150, 99);
            printSupportLib.Name = "printSupportLib";
            printSupportLib.Size = new System.Drawing.Size(135, 24);
            printSupportLib.TabIndex = 46;
            printSupportLib.UseVisualStyleBackColor = true;
            //
            // bluetoothLib
            //
            bluetoothLib.Location = new System.Drawing.Point(6, 75);
            bluetoothLib.Name = "bluetoothLib";
            bluetoothLib.Size = new System.Drawing.Size(137, 24);
            bluetoothLib.TabIndex = 32;
            bluetoothLib.UseVisualStyleBackColor = true;
            //
            // helpLib
            //
            helpLib.Location = new System.Drawing.Point(6, 219);
            helpLib.Name = "helpLib";
            helpLib.Size = new System.Drawing.Size(128, 24);
            helpLib.TabIndex = 38;
            //
            // xmlLib
            //
            xmlLib.Location = new System.Drawing.Point(294, 219);
            xmlLib.Name = "xmlLib";
            xmlLib.Size = new System.Drawing.Size(118, 24);
            xmlLib.TabIndex = 64;
            //
            // activeQtCLib
            //
            activeQtCLib.Location = new System.Drawing.Point(6, 27);
            activeQtCLib.Name = "activeQtCLib";
            activeQtCLib.Size = new System.Drawing.Size(128, 24);
            activeQtCLib.TabIndex = 30;
            //
            // activeQtSLib
            //
            activeQtSLib.Location = new System.Drawing.Point(6, 51);
            activeQtSLib.Name = "activeQtSLib";
            activeQtSLib.Size = new System.Drawing.Size(120, 24);
            activeQtSLib.TabIndex = 31;
            //
            // xmlPatternsLib
            //
            xmlPatternsLib.Location = new System.Drawing.Point(294, 243);
            xmlPatternsLib.Name = "xmlPatternsLib";
            xmlPatternsLib.Size = new System.Drawing.Size(118, 24);
            xmlPatternsLib.TabIndex = 65;
            //
            // openGLLib
            //
            openGLLib.Location = new System.Drawing.Point(150, 51);
            openGLLib.Name = "openGLLib";
            openGLLib.Size = new System.Drawing.Size(120, 24);
            openGLLib.TabIndex = 44;
            //
            // scriptLib
            //
            scriptLib.Location = new System.Drawing.Point(150, 195);
            scriptLib.Name = "scriptLib";
            scriptLib.Size = new System.Drawing.Size(128, 24);
            scriptLib.TabIndex = 50;
            //
            // FormProjectQtSettings
            //
            AcceptButton = okButton;
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(481, 438);
            Controls.Add(tabControl1);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            KeyPreview = true;
            MaximizeBox = false;
            Name = "FormProjectQtSettings";
            ShowInTaskbar = false;
            Text = "FormAddinSettings";
            panel1.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private PropertyGrid OptionsPropertyGrid;
        private Panel panel1;
        private Button okButton;
        private Button cancelButton;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private CheckBox uiToolsLib;
        private CheckBox scriptToolsLib;
        private CheckBox quickLib;
        private CheckBox qmlLib;
        private CheckBox webKitLib;
        private CheckBox multimediaLib;
        private CheckBox networkLib;
        private CheckBox coreLib;
        private CheckBox threeDLib;
        private CheckBox guiLib;
        private CheckBox sqlLib;
        private CheckBox testLib;
        private CheckBox svgLib;
        private CheckBox multimediaWidgetsLib;
        private CheckBox concurrentLib;
        private CheckBox widgetsLib;
        private CheckBox locationLib;
        private CheckBox webkitWidgetsLib;
        private CheckBox sensorsLib;
        private CheckBox declarativeLib;
        private CheckBox printSupportLib;
        private CheckBox bluetoothLib;
        private CheckBox helpLib;
        private CheckBox xmlLib;
        private CheckBox activeQtCLib;
        private CheckBox activeQtSLib;
        private CheckBox xmlPatternsLib;
        private CheckBox openGLLib;
        private CheckBox scriptLib;
        private CheckBox enginioLib;
        private CheckBox nfcLib;
        private CheckBox positioningLib;
        private CheckBox serialPortLib;
        private CheckBox webChannelLib;
        private CheckBox webSocketsLib;
        private CheckBox windowsExtrasLib;
        private CheckBox quickWidgetsLib;
    }
}
