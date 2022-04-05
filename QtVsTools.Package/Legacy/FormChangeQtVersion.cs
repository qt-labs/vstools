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
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace QtVsTools.Legacy
{
    public partial class FormChangeQtVersion : Form
    {
        public FormChangeQtVersion()
        {
            InitializeComponent();
            btnOK.Text = "&OK";
            btnCancel.Text = "&Cancel";
            lQtVersions.Text = "Installed Qt Versions";
            lbQtVersions.DoubleClick += OnQtVersions_DoubleClick;
            KeyPress += FormChangeQtVersion_KeyPress;
            Shown += FormChangeQtVersion_Shown;
        }

        private void FormChangeQtVersion_Shown(object sender, EventArgs e)
        {
            Text = "Set Solution's Qt Version";
        }

        void OnQtVersions_DoubleClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        void FormChangeQtVersion_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27) {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        public void UpdateContent(ChangeFor change)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            lbQtVersions.Items.Clear();
            var vm = Core.QtVersionManager.The();
            foreach (var versionName in vm.GetVersions())
                lbQtVersions.Items.Add(versionName);

            lbQtVersions.Items.Add("$(DefaultQtVersion)");
            if (change == ChangeFor.Solution) {
                var qtVer = Core.Legacy.QtVersionManager
                    .GetSolutionQtVersion(QtVsToolsPackage.Instance.Dte.Solution);
                if (qtVer == null)
                    qtVer = vm.GetDefaultVersion();
                if (qtVer != null)
                    lbQtVersions.SelectedItem = qtVer;
                Text = "Set Solution's Qt Version";
            } else {
                var pro = Core.HelperFunctions.GetSelectedProject(QtVsToolsPackage.Instance.Dte);
                var qtVer = vm.GetProjectQtVersion(pro);
                if (qtVer == null)
                    qtVer = vm.GetDefaultVersion();
                if (qtVer != null)
                    lbQtVersions.SelectedItem = qtVer;
                Text = "Set Project's Qt Version";
            }
        }

        public string GetSelectedQtVersion()
        {
            var idx = lbQtVersions.SelectedIndex;
            if (idx < 0)
                return null;
            return lbQtVersions.Items[idx].ToString();
        }
    }
}
