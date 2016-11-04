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

using System.Windows.Forms;

namespace QtProjectLib
{
    class InfoDialog : Form
    {
        private Label label1;
        private ProgressBar progressBar1;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public InfoDialog(string name)
        {
            label1 = new Label();
            progressBar1 = new ProgressBar();
            SuspendLayout();
            //
            // label1
            //
            label1.Location = new System.Drawing.Point(12, 9);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(370, 13);
            label1.TabIndex = 0;
            label1.Text = SR.GetString("QMakeProcess_OpenSolutionFromFile") + name;
            //
            // progressBar1
            //
            progressBar1.Location = new System.Drawing.Point(13, 28);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new System.Drawing.Size(369, 23);
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.TabIndex = 1;
            //
            // Form1
            //
            ClientSize = new System.Drawing.Size(394, 67);
            MinimumSize = new System.Drawing.Size(402, 94);
            ControlBox = false;
            Controls.Add(progressBar1);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            ShowInTaskbar = false;
            Text = SR.GetString("Resources_QtVsTools");
            StartPosition = FormStartPosition.CenterParent;

            ResumeLayout(false);
            PerformLayout();
        }

        public void CloseEventHandler()
        {
            Close();
        }
    }
}
