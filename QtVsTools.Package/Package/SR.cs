/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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

using System.Resources;

namespace QtVsTools
{
    internal sealed class SR
    {
        static SR loader;
        readonly ResourceManager resources;
        static readonly object obj = new object();

        internal SR()
        {
            resources = new ResourceManager("QtVsTools.Package.Resources", GetType().Assembly);
        }

        private static SR GetLoader()
        {
            if (loader == null) {
                lock (obj) {
                    if (loader == null)
                        loader = new SR();
                }
            }
            return loader;
        }

        public static string GetString(string name, params object[] args)
        {
            var sys = GetLoader();
            if (sys == null)
                return null;

            var res = sys.resources.GetString(name, null);
            if (args != null && args.Length > 0 && !string.IsNullOrEmpty(res))
                return string.Format(res, args);
            return res;
        }

        public static string GetString(string name)
        {
            return GetString(name, null);
        }
    }
}
