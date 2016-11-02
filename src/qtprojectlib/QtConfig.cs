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
using System.IO;

namespace QtProjectLib
{
    /// <summary>
    /// Very very simple reader for the qconfig.pri file.
    /// At the moment this is only to determine whether we
    /// have a static or shared Qt build.
    /// Also, we extract the default signature file here for Windows CE builds.
    /// </summary>
    class QtConfig
    {
        private bool isStaticBuild;
        private string signatureFile;

        public QtConfig(string qtdir)
        {
            Init(qtdir);
        }

        private void Init(string qtdir)
        {
            var fi = new FileInfo(qtdir + "\\mkspecs\\qconfig.pri");
            if (fi.Exists) {
                try {
                    var reader = new StreamReader(fi.FullName);
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                        parseLine(line);
                } catch { }
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
            if (line.StartsWith("CONFIG", StringComparison.Ordinal)) {
                var values = line.Substring(6).Split(' ', '\t');
                foreach (var s in values) {
                    if (s == "static")
                        isStaticBuild = true;
                    else if (s == "shared")
                        isStaticBuild = false;
                }
            } else if (line.StartsWith("DEFAULT_SIGNATURE", StringComparison.Ordinal)) {
                var idx = line.IndexOf('=');
                if (idx < 0)
                    return;
                signatureFile = line.Remove(0, idx + 1).Trim();
            }
        }
    }
}
