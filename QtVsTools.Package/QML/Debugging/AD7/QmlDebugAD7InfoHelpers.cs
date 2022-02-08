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

using System;
using System.Collections;
using System.Collections.Generic;

namespace QtVsTools.Qml.Debug.AD7
{
    class InfoHelper<TDerived>
         where TDerived : InfoHelper<TDerived>
    {
        public class Ref<TStruct>
        {
            public TStruct s = default(TStruct);
        }

        public abstract class MapField<TStruct, TFieldMask>
        {
            public TFieldMask FieldMaskBitCheck { get; set; }
            public TFieldMask FieldMaskBitUpdate { get; set; }
            public abstract void Map(TDerived infoObj, Ref<TStruct> infoStruct);
        }

        protected class MapField<TStruct, TFieldMask, T1, T2> : MapField<TStruct, TFieldMask>
        {
            public Func<TDerived, T1> FieldValue { get; set; }
            public Func<T1, T2> Convert { get; set; }
            public Func<T1, bool> IsNull { get; set; }
            public Action<Ref<TStruct>, T2> MapToStruct { get; set; }

            public override void Map(TDerived infoObj, Ref<TStruct> infoStruct)
            {
                if (FieldValue == null || MapToStruct == null)
                    return;

                T1 fieldValue = FieldValue(infoObj);
                if (IsNull(fieldValue))
                    return;

                MapToStruct(infoStruct, Convert(fieldValue));
            }
        }

        public abstract class Mapping
        { }

        public class Mapping<TStruct, TFieldMask> : Mapping,
            IEnumerable<MapField<TStruct, TFieldMask>>
        {
            readonly List<MapField<TStruct, TFieldMask>> fieldMaps;

            private static Action<Ref<TStruct>, TFieldMask> UpdateMask { get; set; }

            public Mapping(Action<Ref<TStruct>, TFieldMask> updateMask)
            {
                fieldMaps = new List<MapField<TStruct, TFieldMask>>();
                UpdateMask = updateMask;
            }

            public void Add<T>(
                TFieldMask fieldMaskBit,
                Action<Ref<TStruct>, T> mapToStruct,
                Func<TDerived, T> fieldValue)
                where T : class
            {
                Add(fieldMaskBit, fieldMaskBit, mapToStruct, fieldValue);
            }

            public void Add<T>(
                TFieldMask fieldMaskBitCheck,
                TFieldMask fieldMaskBitUpdate,
                Action<Ref<TStruct>, T> mapToStruct,
                Func<TDerived, T> fieldValue)
                where T : class
            {
                fieldMaps.Add(new MapField<TStruct, TFieldMask, T, T>
                {
                    FieldMaskBitCheck = fieldMaskBitCheck,
                    FieldMaskBitUpdate = fieldMaskBitUpdate,
                    FieldValue = fieldValue,
                    MapToStruct = mapToStruct,
                    IsNull = (x => x == null),
                    Convert = (x => x)
                });
            }

            public void Add<T>(
                TFieldMask fieldMaskBit,
                Action<Ref<TStruct>, T> mapToStruct,
                Func<TDerived, T?> fieldValue)
                where T : struct
            {
                Add(fieldMaskBit, fieldMaskBit, mapToStruct, fieldValue);
            }

            public void Add<T>(
                TFieldMask fieldMaskBitCheck,
                TFieldMask fieldMaskBitUpdate,
                Action<Ref<TStruct>, T> mapToStruct,
                Func<TDerived, T?> fieldValue)
                where T : struct
            {
                fieldMaps.Add(new MapField<TStruct, TFieldMask, T?, T>
                {
                    FieldMaskBitCheck = fieldMaskBitCheck,
                    FieldMaskBitUpdate = fieldMaskBitUpdate,
                    FieldValue = fieldValue,
                    MapToStruct = mapToStruct,
                    IsNull = (x => x == null),
                    Convert = (x => (x.HasValue ? x.Value : default(T)))
                });
            }

            public IEnumerator<MapField<TStruct, TFieldMask>> GetEnumerator()
            {
                return fieldMaps.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Map(TDerived infoObj, TFieldMask fieldMask, out TStruct infoStruct)
            {
                infoStruct = default(TStruct);
                var r = new Ref<TStruct>();

                foreach (var mapping in this) {
                    if (!MaskHasValue(fieldMask, mapping.FieldMaskBitCheck))
                        continue;
                    mapping.Map(infoObj, r);
                    UpdateMask(r, mapping.FieldMaskBitUpdate);
                }
                infoStruct = r.s;
            }

            protected virtual bool MaskHasValue(TFieldMask fieldMask, TFieldMask fieldMaskBit)
            {
                if (typeof(TFieldMask).IsEnum) {
                    var enumFieldMask = fieldMask as Enum;
                    var enumFieldMaskBit = fieldMaskBit as Enum;
                    return enumFieldMask.HasFlag(enumFieldMaskBit);
                }

                try {
                    var intFieldMask = Convert.ToUInt64(fieldMask);
                    var intFieldMaskBit = Convert.ToUInt64(fieldMaskBit);
                    return (intFieldMask & intFieldMaskBit) != 0;

                } catch {
                    return false;
                }
            }
        }

        public void Map<TStruct, TFieldMask>(
            Mapping mapping,
            TFieldMask fieldMask,
            out TStruct infoStruct)
        {
            if (mapping is Mapping<TStruct, TFieldMask> mappingToStruct)
                mappingToStruct.Map(this as TDerived, fieldMask, out infoStruct);
            else
                infoStruct = default(TStruct);
        }
    }
}
