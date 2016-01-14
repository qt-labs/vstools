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

using System.Collections;

namespace Digia.Qt5ProjectLib
{
    /// <summary>
    /// Simple mathematical set.
    /// Once, we switched to .NET Framework 3.5 for all VS version,
    /// this class can be replaced by HashSet.
    /// </summary>
    class SimpleSet
    {
        private ArrayList elements = null;

        public SimpleSet(ICollection elements)
        {
            this.elements = new ArrayList(elements);
        }

        public ArrayList Elements
        {
            get { return elements; }
        }

        public SimpleSet Union(ICollection collection)
        {
            elements.AddRange(collection);
            elements.Sort();
            int i = 1;
            while (i < elements.Count)
            {
                if (elements[i] == elements[i - 1])
                    elements.RemoveAt(i);
                else
                    i++;
            }
            return this;
        }

        public SimpleSet Minus(SimpleSet subtrahend)
        {
            subtrahend.elements.Sort();
            int i = 0;
            while (i < elements.Count)
            {
                if (subtrahend.elements.BinarySearch(elements[i]) >= 0)
                    elements.RemoveAt(i);
                else
                    i++;
            }
            return this;
        }

        public string JoinElements(char separator)
        {
            string result = "";
            bool firstLoop = true;
            foreach (string elem in elements)
            {
                if (string.IsNullOrEmpty(elem))
                    continue;

                if (firstLoop)
                {
                    firstLoop = false;
                    result = elem;
                }
                else
                    result += separator + elem;
            }
            return result;
        }
    }
}
