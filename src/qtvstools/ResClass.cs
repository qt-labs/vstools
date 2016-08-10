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
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace QtVsTools
{
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class SRDescriptionAttribute : DescriptionAttribute
    {

        private bool replaced;

        /// <summary>
        ///     Constructs a new sys description.
        /// </summary>
        /// <param name='description'>
        ///     description text.
        /// </param>
        public SRDescriptionAttribute(string description)
            : base(description)
        {
        }

        /// <summary>
        ///     Retrieves the description text.
        /// </summary>
        /// <returns>
        ///     description
        /// </returns>
        public override string Description
        {
            get
            {
                if (!replaced) {
                    replaced = true;
                    DescriptionValue = SR.GetString(base.Description);
                }
                return base.Description;
            }
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class SRCategoryAttribute : CategoryAttribute
    {

        public SRCategoryAttribute(string category)
            : base(category)
        {
        }

        protected override string GetLocalizedString(string value)
        {
            return SR.GetString(value);
        }
    }

    internal sealed class SR
    {
        static SR loader;
        readonly ResourceManager resources;
        static readonly Object obj = new Object();

        internal const string OK = "OK";
        internal const string Cancel = "Cancel";
        internal const string QtVSIntegration = "QtVSIntegration";
        internal const string CannotOpenFile = "CannotOpenFile";
        internal const string NotExistingFile = "NotExistingFile";
        internal const string Add = "Add";
        internal const string Edit = "Edit";
        internal const string Remove = "Remove";
        internal const string Delete = "Delete";
        internal static CultureInfo appCultureInfo;
        internal static CultureInfo defaultCultureInfo;

        internal SR()
        {
            defaultCultureInfo = CultureInfo.GetCultureInfo("en");
            appCultureInfo = CultureInfo.GetCultureInfo(Vsix.Instance.Dte.LocaleID);
            if (appCultureInfo.Name.StartsWith("en", StringComparison.Ordinal))
                appCultureInfo = null;
            resources = new ResourceManager("QtVsTools.Resources", GetType().Assembly);
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

        private static CultureInfo Culture
        {
            get { return appCultureInfo; }
        }

        public static string GetString(string name, params object[] args)
        {
            var res = GetString(name);
            if (args != null && args.Length > 0)
                return string.Format(res, args);
            return res;
        }

        public static string GetString(string name)
        {
            var sys = GetLoader();
            if (sys == null)
                return null;

            string result;
            try {
                result = sys.resources.GetString(name, SR.Culture);
            } catch (Exception) {
                result = sys.resources.GetString(name, defaultCultureInfo);
            }

            return result;
        }
    }
}
