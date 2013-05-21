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
** a written agreement between you and Digia.  For licensing terms and
** conditions see http://qt.digia.com/licensing.  For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights.  These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

namespace Digia.Qt5ProjectLib
{
    using EnvDTE;
    using System.Windows.Forms;

    public class Messages
    {
        private static OutputWindowPane wndp = null;

        private static OutputWindowPane GetBuildPane(OutputWindow outputWindow)
        {
            foreach (OutputWindowPane owp in outputWindow.OutputWindowPanes)
                if (owp.Guid == "{1BD8A850-02D1-11D1-BEE7-00A0C913D1F8}")
                    return owp;
            return null;
        }
        public static void PaneMessage(EnvDTE.DTE dte, string str)
        {
            OutputWindow wnd = (EnvDTE.OutputWindow)dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput).Object;
            if (wndp == null)
            {
                wndp = wnd.OutputWindowPanes.Add(Resources.msgBoxCaption);
            }

            wndp.OutputString(str + "\r\n");
            OutputWindowPane buildPane = GetBuildPane(wnd);
            // show buildPane if a build is in progress
            if (dte.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateInProgress && buildPane != null)
                buildPane.Activate();
        }

        /// <summary>
        /// Activates the message pane of the Qt Add-in / integration
        /// </summary>
        public static void ActivateMessagePane()
        {
            if (wndp == null)
                return;
            wndp.Activate();
        }

        private static string MessageToString(string msg)
        {
            // doesn't do anything for now...
            return msg;
        }

        private static string ExceptionToString(System.Exception e)
        {
            if (VerboseException)
                return e.Message + "\r\n" + "(" + e.StackTrace.Trim() + ")";
            else
                return e.Message;
        }

        private static string ErrorString = SR.GetString("Messages_ErrorOccured");
        private static string WarningString = SR.GetString("Messages_Warning");
        private static string SolutionString = SR.GetString("Messages_SolveProblem");
        private static bool VerboseException = true;

        static public void DisplayCriticalErrorMessage(System.Exception e)
        {
            MessageBox.Show(ErrorString +
                ExceptionToString(e),
                Resources.msgBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayCriticalErrorMessage(string msg)
        {
            MessageBox.Show(ErrorString +
                MessageToString(msg),
                Resources.msgBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayErrorMessage(System.Exception e, string solution)
        {
            MessageBox.Show(ErrorString + 
                ExceptionToString(e) +
                SolutionString +
                solution,
                Resources.msgBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayErrorMessage(string msg, string solution)
        {
            MessageBox.Show(ErrorString + 
                MessageToString(msg) +
                SolutionString +
                solution,
                Resources.msgBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayErrorMessage(System.Exception e)
        {
            MessageBox.Show(ErrorString +
                ExceptionToString(e),
                Resources.msgBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayErrorMessage(string msg)
        {
            MessageBox.Show(ErrorString +
                MessageToString(msg),
                Resources.msgBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayWarningMessage(System.Exception e, string solution)
        {
            MessageBox.Show(WarningString +
                ExceptionToString(e) +
                SolutionString +
                solution,
                Resources.msgBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        static public void DisplayWarningMessage(string msg, string solution)
        {
            MessageBox.Show(WarningString + 
                MessageToString(msg) +
                SolutionString +
                solution,
                Resources.msgBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        static public void DisplayWarningMessage(System.Exception e)
        {
            MessageBox.Show(WarningString +
                ExceptionToString(e),
                Resources.msgBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        static public void DisplayWarningMessage(string msg)
        {
            MessageBox.Show(WarningString + 
                MessageToString(msg),
                Resources.msgBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}