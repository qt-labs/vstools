/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Add-in.
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

namespace Digia.Qt5ProjectLib
{
    using System;
    using System.Globalization;
    using System.Resources;
    using System.Threading;
    using System.ComponentModel;

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class SRDescriptionAttribute : DescriptionAttribute
    {

        private bool replaced = false;

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
                if (!replaced)
                {
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
        static SR loader = null;
        ResourceManager resources;

        internal SR()
        {
            resources = new System.Resources.ResourceManager("Digia.Qt5ProjectLib.ProjectLibStringResources", this.GetType().Assembly);
        }

        private static SR GetLoader()
        {
            if (loader == null)
            {
                lock (typeof(SR))
                {
                    if (loader == null)
                    {
                        loader = new SR();
                    }
                }
            }

            return loader;
        }

        private static CultureInfo Culture
        {
            get { return null/*use ResourceManager default, CultureInfo.CurrentUICulture*/; }            
            //get { return new CultureInfo("de-DE"); }            
        }

        public static ResourceManager Resources
        {
            get
            {
                return GetLoader().resources;
            }
        }

        public static String LanguageName
        {
            get { return Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName; }
            //get { return Culture.TwoLetterISOLanguageName; }
        }

        public static string GetString(string name, params object[] args)
        {
            SR sys = GetLoader();
            if (sys == null)
                return null;
            string res = sys.resources.GetString(name, SR.Culture);

            if (args != null && args.Length > 0)
            {
                return String.Format(res, args);
            }
            else
            {
                return res;
            }
        }

        public static string GetString(string name)
        {
            SR sys = GetLoader();
            if (sys == null)
                return null;
            return sys.resources.GetString(name, SR.Culture);
        }

        public static object GetObject(string name)
        {
            SR sys = GetLoader();
            if (sys == null)
                return null;
            return sys.resources.GetObject(name, SR.Culture);
        }
    }
}
