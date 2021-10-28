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

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QtVsTools.Core
{
    public abstract class Observable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static T GetPropertyValue<T>(object obj, string name)
        {
            var retval = GetPropertyValue(obj, name);
            if (retval == null)
                return default(T);
            return (T) retval;
        }

        public static object GetPropertyValue(object obj, string name)
        {
            foreach (var part in name.Split('.')) {
                if (obj == null)
                    return null;

                var type = obj.GetType();
                var propertyInfo = type.GetProperty(part);
                if (propertyInfo == null)
                    return null;
                obj = propertyInfo.GetValue(obj, null);
            }
            return obj;
        }

        protected void SetValue<T>(ref T storage, T value, [CallerMemberName] string name = null)
        {
            if (Equals(storage, value))
                return;

            storage = value;
            OnPropertyChanged(name);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = PropertyChanged;
            if (eventHandler != null)
                eventHandler.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
