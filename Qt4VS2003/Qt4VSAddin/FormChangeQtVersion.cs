/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2010 Nokia Corporation and/or its subsidiary(-ies).
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

using EnvDTE;
using System;
using System.Windows.Forms;
using Nokia.QtProjectLib;

namespace Qt4VSAddin
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
