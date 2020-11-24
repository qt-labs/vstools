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

using QtVsTools.Core;
using System;
using System.Windows.Forms;

namespace QtVsTools
{
    public partial class FormChangeQtVersion : Form
    {

        public FormChangeQtVersion()
        {
            InitializeComponent();
            btnOK.Text = SR.GetString("OK");
            btnCancel.Text = SR.GetString("Cancel");
            lQtVersions.Text = SR.GetString("InstalledQtVersions");
            lbQtVersions.DoubleClick += lbQtVersions_DoubleClick;
            KeyPress += FormChangeQtVersion_KeyPress;
            Shown += FormChangeQtVersion_Shown;
        }

        private void FormChangeQtVersion_Shown(object sender, EventArgs e)
        {
            Text = SR.GetString("SolutionQtVersion");
        }

        void lbQtVersions_DoubleClick(object sender, EventArgs e)
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
            lbQtVersions.Items.Clear();
            var vm = QtVersionManager.The();
            foreach (var versionName in vm.GetVersions())
                lbQtVersions.Items.Add(versionName);

            lbQtVersions.Items.Add("$(DefaultQtVersion)");
            string qtVer = null;
            if (change == ChangeFor.Solution) {
                qtVer = vm.GetSolutionQtVersion(Vsix.Instance.Dte.Solution);
                if (qtVer == null)
                    qtVer = vm.GetDefaultVersion();
                if (qtVer != null)
                    lbQtVersions.SelectedItem = qtVer;
                Text = SR.GetString("SolutionQtVersion");
            } else {
                var pro = HelperFunctions.GetSelectedProject(Vsix.Instance.Dte);
                qtVer = vm.GetProjectQtVersion(pro);
                if (qtVer == null)
                    qtVer = vm.GetDefaultVersion();
                if (qtVer != null)
                    lbQtVersions.SelectedItem = qtVer;
                Text = SR.GetString("ProjectQtVersion");
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
