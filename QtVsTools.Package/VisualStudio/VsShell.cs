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
                ThreadHelper.ThrowIfNotOnUIThread();

                Initialize();
                return _InstallRootDir;
            }
        }

        private static IVsShell vsShell;
        private static string _InstallRootDir;

        private static void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (vsShell != null)
                return;
            vsShell = VsServiceProvider.GetService<IVsShell>();

            int res = vsShell.GetProperty((int)__VSSPROPID2.VSSPROPID_InstallRootDir, out object o);
            if (res == VSConstants.S_OK && o is string property)
                _InstallRootDir = property;
        }

        public static EnvDTE.Project GetProject(IVsHierarchy context)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            int res = context.GetProperty(
                (uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out object value);
            if (res == VSConstants.S_OK && value is EnvDTE.Project project)
                return project;
            return null;
        }

        public static EnvDTE.ProjectItem GetProjectItem(IVsHierarchy context, uint itemid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            int res = context.GetProperty(
                itemid, (int)__VSHPROPID.VSHPROPID_ExtObject, out object value);
            if (res == VSConstants.S_OK && value is EnvDTE.ProjectItem item)
                return item;
            return null;
        }

        public static EnvDTE.Document GetDocument(IVsHierarchy context, uint itemid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetProjectItem(context, itemid)?.Document;
        }

        private static IVsInfoBarHost _InfoBarHost = null;
        public static IVsInfoBarHost InfoBarHost
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (_InfoBarHost != null)
                    return _InfoBarHost;
                Initialize();
                object host = null;
                vsShell?.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out host);
                return _InfoBarHost = host as IVsInfoBarHost;
            }
        }
    }
}
