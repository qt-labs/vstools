﻿/**************************************************************************
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
