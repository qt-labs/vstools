/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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

        private List<QtModule> qt5list = null, qt6list = null;
        private readonly Dictionary<int, QtModule> qt5modules = new Dictionary<int, QtModule>();
        private readonly Dictionary<int, QtModule> qt6modules = new Dictionary<int, QtModule>();

        public QtModule Module(int id, uint major)
        {
            QtModule module = null;
            if (major < 6)
                qt5modules.TryGetValue(id, out module);
            if (major == 6)
                qt6modules.TryGetValue(id, out module);
            if (major > 6)
                throw new QtVSException("Unsupported Qt version.");
            return module;
        }

        public List<QtModule> GetAvailableModules(uint major)
        {
            if (major < 6) {
                if (qt5list == null) {
                    qt5list = new List<QtModule>(qt5modules.Count);
                    foreach (var entry in qt5modules)
                        qt5list.Add(entry.Value);
                }
                return qt5list;
            }
            if (major == 6) {
                if (qt6list == null) {
                    qt6list = new List<QtModule>(qt6modules.Count);
                    foreach (var entry in qt6modules)
                        qt6list.Add(entry.Value);
                }
                return qt6list;
            }
            if (major > 6)
                throw new QtVSException("Unsupported Qt version.");
            return null;
        }

        private QtModules()
        {
            var uri = new Uri(System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase);
            var pkgInstallPath = Path.GetDirectoryName(Uri.UnescapeDataString(uri.AbsolutePath));

            FillModules(Path.Combine(pkgInstallPath, "qtmodules.xml"), "5", ref qt5modules);
            FillModules(Path.Combine(pkgInstallPath, "qt6modules.xml"), "6", ref qt6modules);
        }

        private void FillModules(string modulesFile, string major, ref Dictionary<int, QtModule> dict)
        {
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

            foreach (var xModule in xml.Elements("QtVsTools").Elements("Module")) {
                int id = (int)xModule.Attribute("Id");
                QtModule module = new QtModule(id, major);
                module.Name = (string)xModule.Element("Name");
                module.Selectable = ((string)xModule.Element("Selectable") == "true");
                module.LibraryPrefix = (string)xModule.Element("LibraryPrefix");
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
                dict.Add(id, module);
            }
        }
    }
}
