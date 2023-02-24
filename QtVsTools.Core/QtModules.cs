/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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

        public List<QtModule> GetAvailableModules(uint major)
        {
            switch (major) {
            case < 6:
                if (qt5list == null) {
                    qt5list = new List<QtModule>(qt5modules.Count);
                    foreach (var entry in qt5modules)
                        qt5list.Add(entry.Value);
                }
                return qt5list;
            case 6:
                if (qt6list == null) {
                    qt6list = new List<QtModule>(qt6modules.Count);
                    foreach (var entry in qt6modules)
                        qt6list.Add(entry.Value);
                }
                return qt6list;
            default:
                throw new QtVSException("Unsupported Qt version.");
            }
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
