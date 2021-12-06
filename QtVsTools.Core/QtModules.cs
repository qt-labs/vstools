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
        private static QtModules instance = new QtModules();
        private readonly Dictionary<int, QtModule> dictModuleInfos = new Dictionary<int, QtModule>();

        public static QtModules Instance
        {
            get { return instance; }
        }

        public QtModule ModuleInformation(int moduleId)
        {
            QtModule moduleInfo;
            dictModuleInfos.TryGetValue(moduleId, out moduleInfo);
            return moduleInfo;
        }

        public List<QtModule> GetAvailableModuleInformation()
        {
            var lst = new List<QtModule>(dictModuleInfos.Count);
            foreach (var entry in dictModuleInfos)
                lst.Add(entry.Value);
            return lst;
        }

        private QtModules()
        {
            QtModule moduleInfo = null;

            var uri = new Uri(
                System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase);
            var pkgInstallPath = Path.GetDirectoryName(
                Uri.UnescapeDataString(uri.AbsolutePath)) + @"\";
            var modulesFile = Path.Combine(pkgInstallPath, "qtmodules.xml");

            if (File.Exists(modulesFile)) {
                var xmlText = File.ReadAllText(modulesFile, Encoding.UTF8);
                XDocument xml = null;
                try {
                    using (var reader = XmlReader.Create(new StringReader(xmlText))) {
                        xml = XDocument.Load(reader);
                    }
                } catch { }
                if (xml != null) {
                    foreach (var xModule in xml.Elements("QtVsTools").Elements("Module")) {
                        int moduleId = (int)xModule.Attribute("Id");
                        moduleInfo = new QtModule(moduleId);
                        moduleInfo.Name = (string)xModule.Element("Name");
                        moduleInfo.ResourceName = (string)xModule.Element("ResourceName");
                        moduleInfo.Selectable = ((string)xModule.Element("Selectable") == "true");
                        moduleInfo.LibraryPrefix = (string)xModule.Element("LibraryPrefix");
                        moduleInfo.HasDLL = ((string)xModule.Element("HasDLL") == "true");
                        moduleInfo.proVarQT = (string)xModule.Element("proVarQT");
                        moduleInfo.proVarCONFIG = (string)xModule.Element("proVarCONFIG");
                        moduleInfo.IncludePath = xModule.Elements("IncludePath")
                            .Select(x => x.Value).ToList();
                        moduleInfo.Defines = xModule.Elements("Defines")
                            .Select(x => x.Value).ToList();
                        moduleInfo.AdditionalLibraries = xModule.Elements("AdditionalLibraries")
                            .Select(x => x.Value).ToList();
                        moduleInfo.AdditionalLibrariesDebug =
                            xModule.Elements("AdditionalLibrariesDebug")
                            .Select(x => x.Value).ToList();
                        if (string.IsNullOrEmpty(moduleInfo.Name)
                            || string.IsNullOrEmpty(moduleInfo.LibraryPrefix)) {
                            Messages.Print("\r\nCritical error: incorrect format of qtmodules.xml");
                            throw new QtVSException("qtmodules.xml");
                        }
                        dictModuleInfos.Add(moduleId, moduleInfo);
                    }
                }
            }
        }

    }
}
