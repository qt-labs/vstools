/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2010 Nokia Corporation and/or its subsidiary(-ies).
**
** Contact: Nokia Corporation (qt-info@nokia.com)
**
** Commercial Usage
**
** Licensees holding valid Qt Commercial licenses may use this file in
** accordance with the Qt Commercial License Agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Nokia.
**
** GNU Lesser General Public License Usage
**
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** If you are unsure which license is appropriate for your use, please
** contact the sales department at http://qt.nokia.com/contact.
**
**************************************************************************/

using System.Collections;

namespace Nokia.QtProjectLib
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
