/****************************************************************************
**
** Copyright (C) 2021 The Qt Company Ltd.
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
using System.Collections.Generic;

namespace QtVsTools.Core
{
    public class QtModule
    {
        public string Name;
        public bool Selectable;
        public List<string> Defines = new List<string>();
        public string LibraryPrefix = string.Empty;
        public List<string> AdditionalLibraries = new List<string>();
        public List<string> AdditionalLibrariesDebug = new List<string>();
        public List<string> IncludePath = new List<string>();
        public string proVarQT;
        private string majorVersion;

        public string LibRelease =>
            LibraryPrefix.StartsWith("Qt", StringComparison.Ordinal)
                ? "Qt" + majorVersion + LibraryPrefix.Substring(2) + ".lib"
                : LibraryPrefix + ".lib";

        public string LibDebug =>
            LibraryPrefix.StartsWith("Qt", StringComparison.Ordinal)
                ? "Qt" + majorVersion + LibraryPrefix.Substring(2) + "d.lib"
                : LibraryPrefix + "d.lib";

        public QtModule(int id, string major)
        {
            Id = id;
            majorVersion = major;
        }

        public int Id { get; } = -1;
    }
}
