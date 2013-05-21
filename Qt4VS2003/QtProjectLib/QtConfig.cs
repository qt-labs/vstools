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

using System.IO;

namespace Digia.Qt5ProjectLib
{
    /// <summary>
    /// Very very simple reader for the qconfig.pri file.
    /// At the moment this is only to determine whether we
    /// have a static or shared Qt build.
    /// Also, we extract the default signature file here for Windows CE builds.
    /// </summary>
    class QtConfig
    {
        private bool isStaticBuild = false;
        private string signatureFile = null;

        public QtConfig(string qtdir)
        {
            Init(qtdir);
        }

        private void Init(string qtdir)
        {
            FileInfo fi = new FileInfo(qtdir + "\\mkspecs\\qconfig.pri");
            if (fi.Exists)
            {
                try
                {
                    StreamReader reader = new StreamReader(fi.FullName);
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                        parseLine(line);
                }
                catch {}
            }
        }

        public bool IsStaticBuild
        {
            get { return isStaticBuild; }
        }

        public string SignatureFile
        {
            get { return signatureFile; }
        }

        /// <summary>
        /// parses a single line of the configuration file
        /// </summary>
        /// <returns>true if we don't have to read any further</returns>
        private void parseLine(string line)
        {
            line = line.Trim();
            if (line.StartsWith("CONFIG"))
            {
                string[] values = line.Substring(6).Split(new char[] { ' ', '\t' });
                foreach (string s in values)
                {
                    if (s == "static")
                        isStaticBuild = true;
                    else if (s == "shared")
                        isStaticBuild = false;
                }
            }
            else if (line.StartsWith("DEFAULT_SIGNATURE"))
            {
                int idx = line.IndexOf('=');
                if (idx < 0)
                    return;
                signatureFile = line.Remove(0, idx + 1).Trim();
            }
        }
    }
}
