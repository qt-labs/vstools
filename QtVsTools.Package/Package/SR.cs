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

using Microsoft.VisualStudio.Shell;
using System;
using System.Globalization;
using System.Resources;

namespace QtVsTools
{
    internal sealed class SR
    {
        static SR loader;
        readonly ResourceManager resources;
        static readonly Object obj = new Object();

        internal const string OK = "OK";
        internal const string Cancel = "Cancel";

        private static CultureInfo appCultureInfo;
        private static CultureInfo defaultCultureInfo;

        internal SR(int localeId)
        {
            defaultCultureInfo = CultureInfo.GetCultureInfo("en");
            appCultureInfo = CultureInfo.GetCultureInfo(localeId);
            if (appCultureInfo.Name.StartsWith("en", StringComparison.Ordinal))
                appCultureInfo = null;
            resources = new ResourceManager("QtVsTools.Package.Resources", GetType().Assembly);
        }

        private static SR GetLoader(int localeId)
        {
            if (loader == null) {
                lock (obj) {
                    if (loader == null)
                        loader = new SR(localeId);
                }
            }
            return loader;
        }

        private static CultureInfo Culture
        {
            get { return appCultureInfo; }
        }

        public static string GetString(string name, params object[] args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var res = GetString(name);
            if (!string.IsNullOrEmpty(res) && args != null && args.Length > 0)
                return string.Format(res, args);
            return res;
        }

        public static string GetString(string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetString(name, QtVsToolsPackage.Instance);
        }

        public static string GetString(string name, QtVsToolsPackage vsixInstance)
        {
           ThreadHelper.ThrowIfNotOnUIThread();

            var sys = GetLoader(vsixInstance.Dte.LocaleID);
            if (sys == null)
                return null;

            string result;
            try {
                result = sys.resources.GetString(name, Culture);
            } catch (Exception) {
                result = sys.resources.GetString(name, defaultCultureInfo);
            }

            return result;
        }
    }
}
