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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace QtVsTools.Core
{
    public class QtModules
    {
        public static QtModules Instance { get; } = new QtModules();
        private readonly Dictionary<int, QtModule> modules = new Dictionary<int, QtModule>();


        public QtModule Module(int id)
        {
            modules.TryGetValue(id, out QtModule module);
            return module;
        }

        public List<QtModule> GetAvailableModules()
        {
            var lst = new List<QtModule>(modules.Count);
            foreach (var entry in modules)
                lst.Add(entry.Value);
            return lst;
        }

        private QtModules()
        {
            var uri = new Uri(
                System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase);
            var pkgInstallPath = Path.GetDirectoryName(
                Uri.UnescapeDataString(uri.AbsolutePath)) + @"\";

            var modulesFile = Path.Combine(pkgInstallPath, "qtmodules.xml");
            if (!File.Exists(modulesFile))
                return;

            var xmlText = File.ReadAllText(modulesFile, Encoding.UTF8);
            XDocument xml = null;
            try {
                using (var reader = XmlReader.Create(new StringReader(xmlText))) {
                    xml = XDocument.Load(reader);
                }
            } catch { }

            if (xml == null)
                return;

            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var xModule in xml.Elements("QtVsTools").Elements("Module")) {
                int id = (int)xModule.Attribute("Id");
                QtModule module = new QtModule(id);
                module.Name = (string)xModule.Element("Name");
                module.Selectable = ((string)xModule.Element("Selectable") == "true");
                module.LibraryPrefix = (string)xModule.Element("LibraryPrefix");
                module.HasDLL = ((string)xModule.Element("HasDLL") == "true");
                module.proVarQT = (string)xModule.Element("proVarQT");
                module.proVarCONFIG = (string)xModule.Element("proVarCONFIG");
                module.IncludePath = xModule.Elements("IncludePath")
                    .Select(x => x.Value).ToList();
                module.Defines = xModule.Elements("Defines")
                    .Select(x => x.Value).ToList();
                module.AdditionalLibraries = xModule.Elements("AdditionalLibraries")
                    .Select(x => x.Value).ToList();
                module.AdditionalLibrariesDebug =
                    xModule.Elements("AdditionalLibrariesDebug")
                    .Select(x => x.Value).ToList();
                if (string.IsNullOrEmpty(module.Name)
                    || string.IsNullOrEmpty(module.LibraryPrefix)) {
                    Messages.Print("\r\nCritical error: incorrect format of qtmodules.xml");
                    throw new QtVSException("qtmodules.xml");
                }
                modules.Add(id, module);
            }
        }
    }
}
