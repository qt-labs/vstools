/****************************************************************************
**
** Copyright (C) 2012 Digia Plc and/or its subsidiary(-ies).
** Contact: http://www.qt-project.org/legal
**
** This file is part of the Qt VS Add-in.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia.  For licensing terms and
** conditions see http://qt.digia.com/licensing.  For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights.  These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
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
