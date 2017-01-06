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
        enum BuildType {
            Unknown,
            Static,
            Shared
        }

    /// <summary>
    /// A very simple reader for the qconfig.pri file. At the moment this is only to determine
    /// whether we have a static or shared Qt build.
    /// </summary>
    class QtConfig
    {
        public BuildType BuildType { get; private set; }

        public QtConfig(string qtdir)
        {
            var fi = new FileInfo(qtdir + "\\mkspecs\\qconfig.pri");
            if (!fi.Exists)
                return;

            try {
                using (var reader = new StreamReader(fi.FullName)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        line = line.Trim();
                        if (!line.StartsWith("CONFIG", StringComparison.Ordinal))
                            continue;
                        var values = line.Substring(6).Split(' ', '\t');
                        foreach (var value in values) {
                            switch (value) {
                            case "static":
                                BuildType = BuildType.Static;
                                return;
                            case "shared":
                                BuildType = BuildType.Shared;
                                return;
                            }
                        }
                    }
                }
            } catch { }
        }
    }
}
