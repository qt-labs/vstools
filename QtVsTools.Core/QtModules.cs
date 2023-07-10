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
        public static QtModules Instance { get; } = new();

        private readonly IReadOnlyCollection<QtModule> qt5Modules;
        private readonly IReadOnlyCollection<QtModule> qt6Modules;

        public IEnumerable<QtModule> GetAvailableModules(uint major)
        {
            return major switch
            {
                < 6 => qt5Modules,
                6 => qt6Modules,
                _ => throw new QtVSException("Unsupported Qt version.")
            };
        }

        private QtModules()
        {
            var uri = new Uri(System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase);
            var pkgInstallPath = Path.GetDirectoryName(Uri.UnescapeDataString(uri.AbsolutePath));

            qt5Modules = FillModules(pkgInstallPath, "qtmodules.xml", "5");
            qt6Modules = FillModules(pkgInstallPath, "qt6modules.xml", "6");
        }

        private static IReadOnlyCollection<QtModule>FillModules(string packagePath,
            string modulesFile, string major)
        {
            var list = new List<QtModule>();
            var modulesFilePath = Path.Combine(packagePath, modulesFile);
            if (!File.Exists(modulesFilePath))
                return list;

            var xmlText = File.ReadAllText(modulesFilePath, Encoding.UTF8);
            XDocument xml = null;
            try {
                using var reader = XmlReader.Create(new StringReader(xmlText));
                xml = XDocument.Load(reader);
            } catch (Exception exception) {
                exception.Log();
            }

            if (xml == null)
                return list;

            foreach (var xModule in xml.Elements("QtVsTools").Elements("Module")) {
                var module = new QtModule(major)
                {
                    Name = (string)xModule.Element("Name"),
                    Selectable = (string)xModule.Element("Selectable") == "true",
                    LibraryPrefix = (string)xModule.Element("LibraryPrefix"),
                    proVarQT = (string)xModule.Element("proVarQT"),
                    IncludePath = xModule.Elements("IncludePath").Select(x => x.Value).ToList(),
                    Defines = xModule.Elements("Defines").Select(x => x.Value).ToList(),
                    AdditionalLibraries = xModule.Elements("AdditionalLibraries")
                        .Select(x => x.Value).ToList(),
                    AdditionalLibrariesDebug = xModule.Elements("AdditionalLibrariesDebug")
                        .Select(x => x.Value).ToList()
                };
                if (string.IsNullOrEmpty(module.Name) || string.IsNullOrEmpty(module.LibraryPrefix)) {
                    Messages.Print($"\r\nCritical error: incorrect format of {modulesFile}");
                    throw new QtVSException($"Critical error: incorrect format of {modulesFile}");
                }
                list.Add(module);
            }

            return list;
        }
    }
}
