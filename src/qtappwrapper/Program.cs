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
using System.Diagnostics;

namespace QtAppWrapper
{
    /// <summary>
    /// This application serves two different purposes. The first one is to run as a standalone
    /// server application. The second purpose is to be registered by a VSIX instance as default
    /// handler for .ui and .ts files. Once a user starts editing a file inside VS, the default
    /// registered file handler (this application) is started as new instance with the file name
    /// as argument. We now simply forward the file and PID to the running server, that will then
    /// broadcast the incoming data to connected VSIX instances. The VSIX instance matching the
    /// PID will now lookup the right Qt Designer or Qt Linguist and open the corresponding file.
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Debug.AutoFlush = true;
            Debug.WriteLine("Entering Main function.");
            Debug.Indent();

            try {
                switch (args.Length) {
                case 0:
                    // Try to start the server and begin to listen.
                    Debug.WriteLine("Starting the server instance.");
                    DefaultEditorsServer.StartListen();
                    break;
                default:
                    // Forward incoming arguments to the running server instance.
                    Debug.WriteLine("Sending file name to server instance.");
                    DefaultEditorsHandler.SendFileNameToDefaultEditorsServer(args);
                    break;
                }
            } catch (Exception e) {
                Debug.WriteLine("Exception thrown:");
                Debug.Indent();
                Debug.WriteLine(e.Message);
                Debug.Unindent();
            }

            Debug.Unindent();
            Debug.WriteLine("Leaving Main function");
        }
    }
}
