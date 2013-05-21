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

using System.Xml;
using System.Collections.Generic;

namespace Digia.Qt5ProjectLib
{
    public class QrcItem
    {
        private string path = null;
        private string alias = null;

        public QrcItem()
        {
        }

        public QrcItem(string p, string a)
        {
            path = p;
            alias = a;
        }

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        public string Alias
        {
            get { return alias; }
            set { alias = value; }
        }
    }

    public class QrcPrefix
    {
        private string prefix = null;
        private string lang = null;
        private List<QrcItem> items;
        public List<QrcItem> Items
        {
            get { return items; }
        }

        public QrcPrefix()
        {
            items = new List<QrcItem>();
        }

        public string Prefix
        {
            get { return prefix; }
            set { prefix = value; }
        }

        public string Language
        {
            get { return lang; }
            set { lang = value; }
        }

        public void AddQrcItem(QrcItem i)
        {
            items.Add(i);
        }
    }

    public class QrcParser
    {
        private string qrcFileName = null;
        private Stack<QrcPrefix> prefixes = null;
        private List<QrcPrefix> prefxs;

        public List<QrcPrefix> Prefixes
        {
            get { return prefxs; }
        }

        public QrcParser(string fileName)
        {
            qrcFileName = fileName;
            prefixes = new Stack<QrcPrefix>();
            prefxs = new List<QrcPrefix>();
        }

        public bool parse()
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(qrcFileName);
            if (!fi.Exists)
                return false;
            try 
            {
                XmlTextReader reader = new XmlTextReader(qrcFileName);
                QrcItem currentItem = null;
                QrcPrefix currentPrefix = null;
                while (reader.Read()) 
                {
                    switch (reader.NodeType) 
                    {
                        case XmlNodeType.Element:
                            if (reader.LocalName.ToLower() == "qresource") 
                            {
                                currentPrefix = new QrcPrefix();
                                currentPrefix.Prefix = reader.GetAttribute("prefix");
                                currentPrefix.Language = reader.GetAttribute("lang");
                                prefixes.Push(currentPrefix);
                            }
                            else if (reader.LocalName.ToLower() == "file")
                            {
                                currentItem = new QrcItem();
                                currentItem.Alias = reader.GetAttribute("name");
                            }
                            break;
                        case XmlNodeType.EndElement:
                            if (reader.LocalName.ToLower() == "qresource") 
                            {
                                prefxs.Add(prefixes.Pop());
                            } 
                            else if (reader.LocalName.ToLower() == "file"
                                && prefixes.Peek() != null && currentItem != null) 
                            {
                                ((QrcPrefix)(prefixes.Peek())).AddQrcItem(currentItem);
                                currentItem = null;
                            }
                            break;
                        case XmlNodeType.Text:
                            if (currentItem != null)
                                currentItem.Path = reader.Value;
                            break;
                    }
                }
                reader.Close();
            } 
            catch (System.Exception)
            {
                return false;
            }
            return true;
        }
    }
}