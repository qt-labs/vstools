/****************************************************************************
**
** Copyright (C) 2018 The Qt Company Ltd.
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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.VisualStudio
{
    static class VsShell
    {
        public static string InstallRootDir
        {
            get
            {
                Initialize();
                return _InstallRootDir;
            }
        }

        private static IVsShell vsShell;
        private static string _InstallRootDir;

        private static void Initialize()
        {
            if (vsShell != null)
                return;
            vsShell = Package.GetGlobalService(typeof(IVsShell)) as IVsShell;

            object objProp;
            int res = vsShell.GetProperty((int)__VSSPROPID2.VSSPROPID_InstallRootDir, out objProp);
            if (res == VSConstants.S_OK && objProp is string)
                _InstallRootDir = objProp as string;
        }
    }
}
