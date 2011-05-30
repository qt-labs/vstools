/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2011 Nokia Corporation and/or its subsidiary(-ies).
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

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Nokia.QtProjectLib
{
    public enum QtModule
    {
        Invalid = -1,
        Core = 1,
        Xml = 2,
        Sql = 3,
        OpenGL = 4,
        Network = 5,
        Compat = 6,
        Gui = 7,
        ActiveQtS = 8,
        ActiveQtC = 9,
        Main = 10,
        Qt3Library = 11,    // ### unused
        Qt3Main = 12,       // ### unused
        Svg = 13,
        Designer = 14,
        Test = 15,
        Script = 16,
        Help = 17,
        WebKit = 18,
        XmlPatterns = 19,
        Phonon = 20,
        Multimedia = 21,
        Declarative = 22,
        ScriptTools = 23,
        UiTools = 24,
    }

    public class QtModuleInfo
    {
        private QtModule moduleId = QtModule.Invalid;
        public List<string> Defines = new List<string>();
        public string LibraryPrefix = "";
        public bool HasDLL = true;
        public List<string> AdditionalLibraries = new List<string>();
        public List<string> AdditionalLibrariesWinCE = new List<string>();
        public string IncludePath = null;
        public string proVarQT = null;
        public string proVarCONFIG = null;
        public List<QtModule> dependentModules = new List<QtModule>();  // For WinCE deployment.

        public QtModuleInfo(QtModule id)
        {
            moduleId = id;
        }

        public QtModule ModuleId
        {
            get { return moduleId; }
        }

        public List<string> GetLibs(bool isDebugCfg, VersionInformation vi)
        {
            return GetLibs(isDebugCfg, vi.IsStaticBuild(), vi.IsWinCEVersion());
        }

        public List<string> GetLibs(bool isDebugCfg, bool isStaticBuild, bool isWindowsCE)
        {
            List<string> libs = new List<string>();
            string libName = LibraryPrefix;
            if (isDebugCfg)
                libName += "d";
            if (!isStaticBuild && HasDLL)
                libName += "4";
            libName += ".lib";
            libs.Add(libName);
            if (isWindowsCE)
                libs.AddRange(AdditionalLibrariesWinCE);
            else
                libs.AddRange(AdditionalLibraries);
            return libs;
        }

        public string GetDllFileName(bool isDebugCfg)
        {
            string fileName = LibraryPrefix;
            if (isDebugCfg)
                fileName += "d";
            fileName += "4.dll";
            return fileName;
        }
    }

    public class QtModules
    {
        private static QtModules instance = new QtModules();
        private Dictionary<string, QtModule> dictModulesByDLL = new Dictionary<string, QtModule>();
        private Dictionary<QtModule, QtModuleInfo> dictModuleInfos = new Dictionary<QtModule, QtModuleInfo>();

        public static QtModules Instance
        {
            get { return instance; }
        }

        public QtModuleInfo ModuleInformation(QtModule moduleId)
        {
            QtModuleInfo moduleInfo;
            dictModuleInfos.TryGetValue(moduleId, out moduleInfo);
            return moduleInfo;
        }

        public QtModule ModuleIdByName(string moduleName)
        {
            QtModule moduleId;
            if (dictModulesByDLL.TryGetValue(moduleName, out moduleId))
                return moduleId;
            else
                return QtModule.Invalid;
        }

        public List<QtModuleInfo> GetAvailableModuleInformation()
        {
            List<QtModuleInfo> lst = new List<QtModuleInfo>(dictModuleInfos.Count);
            foreach (KeyValuePair<QtModule, QtModuleInfo> entry in dictModuleInfos)
                lst.Add(entry.Value);
            return lst;
        }

        private QtModules()
        {
            InitQtModule(QtModule.Core, "QtCore", "QT_CORE_LIB");
            InitQtModule(QtModule.Gui, "QtGui", "QT_GUI_LIB");
            InitQtModule(QtModule.Multimedia, "QtMultimedia", "QT_MULTIMEDIA_LIB");
            InitQtModule(QtModule.Sql, "QtSql", "QT_SQL_LIB");
            InitQtModule(QtModule.Network, "QtNetwork", "QT_NETWORK_LIB");
            InitQtModule(QtModule.Xml, "QtXml", "QT_XML_LIB");
            InitQtModule(QtModule.Script, "QtScript", "QT_SCRIPT_LIB");
            InitQtModule(QtModule.XmlPatterns, "QtXmlPatterns", "QT_XMLPATTERNS_LIB");
            InitQtModule(QtModule.ScriptTools, "QtScriptTools", "QT_SCRIPTTOOLS_LIB");

            QtModuleInfo moduleInfo = null;
            moduleInfo = InitQtModule(QtModule.Main, "qtmain", "");
            moduleInfo.proVarQT = null;
            moduleInfo.HasDLL = false;
            moduleInfo.IncludePath = null;

            moduleInfo = InitQtModule(QtModule.Test, "QtTest", "");
            moduleInfo.proVarQT = null;
            moduleInfo.proVarCONFIG = "qtestlib";

            moduleInfo = InitQtModule(QtModule.Help, "QtHelp", "");
            moduleInfo.proVarQT = null;
            moduleInfo.proVarCONFIG = "help";

            moduleInfo = InitQtModule(QtModule.Compat, "Qt3Support", new string[] { "QT_QT3SUPPORT_LIB", "QT3_SUPPORT" });
            moduleInfo.AdditionalLibraries.Add("comdlg32.lib");

            moduleInfo = InitQtModule(QtModule.Phonon, "phonon", "QT_PHONON_LIB");

            moduleInfo = InitQtModule(QtModule.WebKit, "QtWebKit", "QT_WEBKIT_LIB");
            moduleInfo.dependentModules.Add(QtModule.Phonon);

            moduleInfo = InitQtModule(QtModule.Svg, "QtSvg", "QT_SVG_LIB");
            moduleInfo.dependentModules.Add(QtModule.Xml);

            moduleInfo = InitQtModule(QtModule.Declarative, "QtDeclarative", "QT_DECLARATIVE_LIB");
            moduleInfo.dependentModules.Add(QtModule.Script);
            moduleInfo.dependentModules.Add(QtModule.Sql);
            moduleInfo.dependentModules.Add(QtModule.XmlPatterns);
            moduleInfo.dependentModules.Add(QtModule.Network);

            moduleInfo = InitQtModule(QtModule.OpenGL, "QtOpenGL", "QT_OPENGL_LIB");
            moduleInfo.AdditionalLibraries.Add("opengl32.lib");
            moduleInfo.AdditionalLibraries.Add("glu32.lib");
            moduleInfo.AdditionalLibrariesWinCE.Add("libgles_cm.lib");

            moduleInfo = InitQtModule(QtModule.ActiveQtS, "QAxServer", "QAXSERVER");
            moduleInfo.HasDLL = false;
            moduleInfo.IncludePath = "$(QTDIR)\\include\\ActiveQt";

            moduleInfo = InitQtModule(QtModule.ActiveQtC, "QAxContainer", "");
            moduleInfo.HasDLL = false;
            moduleInfo.IncludePath = "$(QTDIR)\\include\\ActiveQt";

            moduleInfo = InitQtModule(QtModule.Designer, "QtDesigner", "QDESIGNER_EXPORT_WIDGETS");
            moduleInfo.HasDLL = false;

            moduleInfo = InitQtModule(QtModule.UiTools, "QtUiTools", "");
            moduleInfo.dependentModules.Add(QtModule.Xml);
            moduleInfo.HasDLL = false;
        }

        private QtModuleInfo InitQtModule(QtModule moduleId, string libraryPrefix, string define)
        {
            return InitQtModule(moduleId, libraryPrefix, new string[] { define });
        }

        private QtModuleInfo InitQtModule(QtModule moduleId, string libraryPrefix, string[] defines)
        {
            QtModuleInfo moduleInfo = new QtModuleInfo(moduleId);
            moduleInfo.LibraryPrefix = libraryPrefix;
            moduleInfo.IncludePath = "$(QTDIR)\\include\\" + libraryPrefix;
            moduleInfo.Defines = new List<string>();
            dictModulesByDLL.Add(libraryPrefix, moduleId);
            foreach (string str in defines)
            {
                if (string.IsNullOrEmpty(str))
                    continue;
                moduleInfo.Defines.Add(str);
            }
            dictModuleInfos.Add(moduleId, moduleInfo);

            if (libraryPrefix.StartsWith("Qt"))
                moduleInfo.proVarQT = libraryPrefix.Substring(2).ToLower();
            else
                moduleInfo.proVarQT = libraryPrefix.ToLower();

            return moduleInfo;
        }

    }
}
