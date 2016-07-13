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

namespace QtProjectLib
{
    public class QtProjectEngine
    {
        /// <summary>
        /// Checks if an add-on qt module is installed
        /// </summary>
        /// <param name="moduleName">the module to find
        /// </param>
        public bool IsModuleInstalled(string moduleName)
        {
            QtVersionManager versionManager = QtVersionManager.The();
            string qtVersion = versionManager.GetDefaultVersion();
            if (qtVersion == null) {
                throw new QtVSException("Unable to find a Qt build!\r\n"
                    + "To solve this problem specify a Qt build");
            }
            string install_path = versionManager.GetInstallPath(qtVersion);

            if (moduleName.StartsWith("Qt", StringComparison.Ordinal))
                moduleName = "Qt5" + moduleName.Substring(2);

            string full_path = install_path + "\\lib\\" + moduleName + ".lib";

            System.IO.FileInfo fi = new System.IO.FileInfo(full_path);

            return fi.Exists;
        }
    }
}
