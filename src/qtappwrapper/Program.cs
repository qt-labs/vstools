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
            try {
                if (args.Length >= 1) {
                    string fileName = args[0];
                    if (args.Length >= 2 && args[1].StartsWith("-pid "))
                        EditorServer.SendFileNameToServer(fileName, args[1].Substring(5));
                    else
                        EditorServer.SendFileNameToServer(fileName);
                } else {
                    EditorServer server = new EditorServer();
                }
            } catch (Exception e) {
                MessageBox.Show(e.ToString(), "Exception in QtAppWrapper", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
