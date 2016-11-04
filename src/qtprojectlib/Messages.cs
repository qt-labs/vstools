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

using EnvDTE;
using System.Windows.Forms;

namespace QtProjectLib
{
    public static class Messages
    {
        private static OutputWindowPane wndp;

        private static OutputWindowPane GetBuildPane(OutputWindow outputWindow)
        {
            foreach (OutputWindowPane owp in outputWindow.OutputWindowPanes) {
                if (owp.Guid == "{1BD8A850-02D1-11D1-BEE7-00A0C913D1F8}")
                    return owp;
            }
            return null;
        }
        public static void PaneMessage(DTE dte, string str)
        {
            var wnd = (OutputWindow) dte.Windows.Item(Constants.vsWindowKindOutput).Object;
            if (wndp == null)
                wndp = wnd.OutputWindowPanes.Add(SR.GetString("Resources_QtVsTools"));

            wndp.OutputString(str + "\r\n");
            var buildPane = GetBuildPane(wnd);
            // show buildPane if a build is in progress
            if (dte.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateInProgress && buildPane != null)
                buildPane.Activate();
        }

        /// <summary>
        /// Activates the message pane of the Qt VS Tools extension.
        /// </summary>
        public static void ActivateMessagePane()
        {
            if (wndp == null)
                return;
            wndp.Activate();
        }

        private static string ExceptionToString(System.Exception e)
        {
            return e.Message + "\r\n" + "(" + e.StackTrace.Trim() + ")";
        }

        private static readonly string ErrorString = SR.GetString("Messages_ErrorOccured");
        private static readonly string WarningString = SR.GetString("Messages_Warning");
        private static readonly string SolutionString = SR.GetString("Messages_SolveProblem");

        static public void DisplayCriticalErrorMessage(System.Exception e)
        {
            MessageBox.Show(ErrorString +
                ExceptionToString(e),
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayCriticalErrorMessage(string msg)
        {
            MessageBox.Show(ErrorString +
                msg,
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayErrorMessage(System.Exception e)
        {
            MessageBox.Show(ErrorString +
                ExceptionToString(e),
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayErrorMessage(string msg)
        {
            MessageBox.Show(ErrorString +
                msg,
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayWarningMessage(System.Exception e, string solution)
        {
            MessageBox.Show(WarningString +
                ExceptionToString(e) +
                SolutionString +
                solution,
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        static public void DisplayWarningMessage(string msg)
        {
            MessageBox.Show(WarningString +
                msg,
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
