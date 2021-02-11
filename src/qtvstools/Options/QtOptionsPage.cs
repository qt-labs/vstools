/****************************************************************************
**
** Copyright (C) 2020 The Qt Company Ltd.
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
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell;
using QtVsTools.Core;
using QtVsTools.Common;

namespace QtVsTools.Options
{
    using static EnumExt;

    public class QtOptionsPage : DialogPage
    {
        public enum QtOptions
        {
            [String("QMLDebug_Timeout")] QmlDebugTimeout
        }

        public enum Timeout : uint { Disabled = 0 }

        class TimeoutConverter : EnumConverter
        {
            public TimeoutConverter(Type t) : base(t)
            { }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext c)
                => true;

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext c)
                => false;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext c)
                => new StandardValuesCollection(new[] { Timeout.Disabled });

            public override object ConvertFrom(
                ITypeDescriptorContext context,
                CultureInfo culture,
                object value)
            {
                uint n = 0;
                try {
                    n = Convert.ToUInt32(value);
                } catch { }
                return (Timeout)n;
            }

            public override object ConvertTo(
                ITypeDescriptorContext context,
                CultureInfo culture,
                object value,
                Type destinationType)
            {
                if (destinationType == typeof(string))
                    return value.ToString();
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        [Category("QML Debugging")]
        [DisplayName("Runtime connection timeout (msecs)")]
        [TypeConverter(typeof(TimeoutConverter))]
        public Timeout QmlDebuggerTimeout { get; set; }

        public override void ResetSettings()
        {
            QmlDebuggerTimeout = (Timeout)60000;
        }

        public override void LoadSettingsFromStorage()
        {
            ResetSettings();
            try {
                using (var key = Registry.CurrentUser
                    .OpenSubKey(@"SOFTWARE\" + Resources.registryPackagePath, writable: false)) {
                    if (key == null)
                        return;
                    if (key.GetValue(QtOptions.QmlDebugTimeout.Cast<string>()) is int qmlTimeout)
                        QmlDebuggerTimeout = (Timeout)qmlTimeout;
                }
            } catch (Exception exception) {
                Messages.Print(
                    exception.Message + "\r\n\r\nStacktrace:\r\n" + exception.StackTrace);
            }
        }

        public override void SaveSettingsToStorage()
        {
            try {
                using (var key = Registry.CurrentUser
                    .CreateSubKey(@"SOFTWARE\" + Resources.registryPackagePath)) {
                    if (key == null)
                        return;
                    key.SetValue(
                        QtOptions.QmlDebugTimeout.Cast<string>(),
                        (int)QmlDebuggerTimeout);
                }
            } catch (Exception exception) {
                Messages.Print(
                    exception.Message + "\r\n\r\nStacktrace:\r\n" + exception.StackTrace);
            }
        }
    }
}
