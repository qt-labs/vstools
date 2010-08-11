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

using System.Xml;
using System.Collections.Generic;

namespace Nokia.QtProjectLib
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