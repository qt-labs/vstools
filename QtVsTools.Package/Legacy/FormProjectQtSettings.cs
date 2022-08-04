/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using EnvDTE;

namespace QtVsTools.Legacy
{
    using Core;
    using Legacy = Core.Legacy;

    public partial class FormProjectQtSettings : Form
    {
        private readonly Project project;
        private readonly ProjectQtSettings qtSettings;
        private readonly List<ModuleItem> moduleMap = new List<ModuleItem>();

        private struct ModuleItem
        {
            public readonly CheckBox checkbox;
            public readonly int moduleId;
            public bool initialValue;

            public ModuleItem(CheckBox cb, int mid, bool init)
            {
                checkbox = cb;
                moduleId = mid;
                initialValue = init;
            }
        }

        public FormProjectQtSettings(Project pro)
        {
            InitializeComponent();

            project = pro;
            qtSettings = new ProjectQtSettings(project);
            qtSettings.PropertyChanged += OnPropertyChanged;
            OptionsPropertyGrid.SelectedObject = qtSettings;

            InitModules(QtVersionManager.The().GetProjectQtVersion(project));
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (ModifierKeys == Keys.None && keyData == Keys.Escape) {
                Close();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            // Disable the buttons since some operations are quite expensive (e.g. changing
            // the Qt version) and take some to finish. Keeping the buttons enabled allows to hit
            // the buttons several times resulting in successive executions of these operations.
            okButton.Enabled = false;
            cancelButton.Enabled = false;

            qtSettings.SaveSettings();
            SaveModules();
            okButton.DialogResult = DialogResult.OK;
            Close();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Version")
                throw new ArgumentException();
            InitModules(qtSettings.Version);
        }

        private void InitModules(string qtVersion)
        {
            moduleMap.Clear();
            flowLayoutPanel1.Controls.Clear();

            var vm = QtVersionManager.The();
            var versionInfo = vm.GetVersionInfo(qtVersion);
            if (string.IsNullOrEmpty(qtVersion) || versionInfo == null) {
                flowLayoutPanel1.Controls.Add(new Label
                {
                    Text = Environment.NewLine
                        + "Please set a valid Qt version first.",
                    AutoSize = true
                });
                return;
            }

            var installPath = vm.GetInstallPath(qtVersion) ?? string.Empty;
            var modules = QtModules.Instance.GetAvailableModules(versionInfo.qtMajor)
                .Where(x => x.Selectable)
                .OrderBy(x => x.Name);
            foreach (var module in modules) {
                var checkBox = new CheckBox
                {
                    Margin = new Padding(6),
                    Location = new System.Drawing.Point(150, 150),
                    Name = module.LibraryPrefix,
                    UseVisualStyleBackColor = true,
                    AutoSize = true,
                    Text = module.Name,
                    Checked = Legacy.QtProject.HasModule(project, module.Id, qtVersion)
                };
                flowLayoutPanel1.Controls.Add(checkBox);
                moduleMap.Add(new ModuleItem(checkBox, module.Id, checkBox.Checked));

                var info = QtModules.Instance.Module(module.Id, versionInfo.qtMajor);
                var libraryPrefix = info?.LibraryPrefix;
                if (libraryPrefix.StartsWith("Qt", StringComparison.Ordinal))
                    libraryPrefix = "Qt" + versionInfo.qtMajor + libraryPrefix.Substring(2);
                checkBox.Enabled = new FileInfo(
                    Path.Combine(installPath, "lib", $"{libraryPrefix}{versionInfo.LibInfix()}.lib")
                ).Exists; // Disable the check-box if the module is not installed.
                if (!checkBox.Enabled)
                    checkBox.Checked = false; // Uncheck if the module is not installed.
            }
        }

        private void SaveModules()
        {
            for (var i = 0; i < moduleMap.Count; ++i) {
                var item = moduleMap[i];
                var isModuleChecked = item.checkbox.Checked;
                if (isModuleChecked != item.initialValue) {
                    if (isModuleChecked)
                        Legacy.QtProject.AddModule(project, item.moduleId);
                    else
                        Legacy.QtProject.RemoveModule(project, item.moduleId);
                }
            }
        }

    }
}
