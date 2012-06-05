/****************************************************************************
**
** Copyright (C) 2012 Digia Plc and/or its subsidiary(-ies).
** Contact: http://www.qt-project.org/legal
**
** This file is part of the Qt VS Add-in.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia. For licensing terms and
** conditions see http://qt.digia.com/licensing. For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file. Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights. These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file. Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

using EnvDTE;
using System;
using System.Windows.Forms;

using Digia.Qt5ProjectLib;
namespace Qt5VSAddin
{
    public enum ChangeFor { Solution, Project };

    public partial class FormChangeQtVersion : Form
    {
        
        public FormChangeQtVersion()
        {
            InitializeComponent();
            this.btnOK.Text = SR.GetString("OK");
            this.btnCancel.Text = SR.GetString("Cancel");
            this.Text = SR.GetString("SolutionQtVersion");
            lQtVersions.Text = SR.GetString("InstalledQtVersions");
            lbQtVersions.DoubleClick += new EventHandler(lbQtVersions_DoubleClick);
            this.KeyPress += new KeyPressEventHandler(this.FormChangeQtVersion_KeyPress);
		}

        void lbQtVersions_DoubleClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        void FormChangeQtVersion_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        public void UpdateContent(ChangeFor change)
        {
            lbQtVersions.Items.Clear();
            QtVersionManager vm = QtVersionManager.The();
            foreach (string versionName in vm.GetVersions())
            {
                lbQtVersions.Items.Add(versionName);
            }
            lbQtVersions.Items.Add("$(DefaultQtVersion)");
            string qtVer = null;
            if (change == ChangeFor.Solution)
            {
                qtVer = vm.GetSolutionQtVersion(Connect._applicationObject.Solution);
                if (qtVer == null)
                    qtVer = vm.GetDefaultVersion();
                if (qtVer != null)
                    lbQtVersions.SelectedItem = qtVer;
                this.Text = SR.GetString("SolutionQtVersion");
            }
            else
            {
                Project pro = HelperFunctions.GetSelectedProject(Connect._applicationObject);
                qtVer = vm.GetProjectQtVersion(pro);
                if (qtVer == null)
                    qtVer = vm.GetDefaultVersion();
                if (qtVer != null)
                    lbQtVersions.SelectedItem = qtVer;
                this.Text = SR.GetString("ProjectQtVersion");
            }
        }

        public string GetSelectedQtVersion()
        {
            int idx = lbQtVersions.SelectedIndex;
            if (idx < 0)
                return null;
            return lbQtVersions.Items[idx].ToString();
        }
    }
}
