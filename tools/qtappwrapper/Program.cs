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

using System;
using System.Windows.Forms;

namespace QtAppWrapper
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// 
        /// Usage of qtappwrapper.exe:
        ///     qtappwrapper
        ///         Tries to start the qtappwrapper server and starts to listen.
        ///
        ///     qtappwrapper filename.ui
        ///         Sends the process id of the calling process and the file name
        ///         to the qtappwrapper server.
        ///
        ///     qtappwrapper filename.ui -pid 1234
        ///         Sends the given process id and the file name to the qtappwrapper
        ///         server.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                if (args.Length >= 1)
                {
                    string fileName = args[0];
                    if (args.Length >= 2 && args[1].StartsWith("-pid "))
                    {
                        EditorServer.SendFileNameToServer(fileName, args[1].Substring(5));
                    }
                    else
                    {
                        EditorServer.SendFileNameToServer(fileName);
                    }
                }
                else
                {
                    EditorServer server = new EditorServer();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    e.ToString(),
                    "Exception in QtAppWrapper",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
