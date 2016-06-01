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

using System.Collections.Generic;

namespace QtProjectLib
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
        Enginio = 20,
        Multimedia = 21,
        Declarative = 22,
        ScriptTools = 23,
        UiTools = 24,

        Widgets = 25,
        ThreeD = 26,
        Location = 27,
        Nfc = 28,
        Qml = 29,
        Bluetooth = 30,
        Positioning = 31,
        SerialPort = 32,
        PrintSupport = 33,
        WebChannel = 34,
        WebSockets = 35,
        Sensors = 36,
        WindowsExtras = 37,
        QuickWidgets = 38,
        // JSBackend = 39,
        Quick = 40,
        ThreeDQuick = 41,
        // Feedback = 42,
        // QA = 43,
        // QLALR = 44,
        // RepoTools = 45,
        // Translations = 46,
        // CLucene = 48,
        // DesignerComponents = 49,
        WebkitWidgets = 50,
        Concurrent = 51,
        MultimediaWidgets = 52,
    }

    public class QtModuleInfo
    {
        private QtModule moduleId = QtModule.Invalid;
        public List<string> Defines = new List<string>();
        public string LibraryPrefix = "";
        public bool HasDLL = true;
        public List<string> AdditionalLibraries = new List<string>();
        public List<string> AdditionalLibrariesDebug = new List<string>();
        public string IncludePath = null;
        public string proVarQT = null;
        public string proVarCONFIG = null;

        public QtModuleInfo(QtModule id)
        {
            moduleId = id;
        }

        public QtModule ModuleId
        {
            get { return moduleId; }
        }

        public string GetIncludePath()
        {
            return IncludePath;
        }

        public List<string> GetLibs(bool isDebugCfg, VersionInformation vi)
        {
            return GetLibs(isDebugCfg, vi.IsStaticBuild());
        }

        public List<string> GetLibs(bool isDebugCfg, bool isStaticBuild)
        {
            List<string> libs = new List<string>();
            string libName = LibraryPrefix;
            if (libName.StartsWith("Qt"))
                libName = "Qt5" + libName.Substring(2);
            if (isDebugCfg)
                libName += "d";
            libName += ".lib";
            libs.Add(libName);
            libs.AddRange(GetAdditionalLibs(isDebugCfg));
            return libs;
        }

        public string GetDllFileName(bool isDebugCfg)
        {
            string fileName = LibraryPrefix;
            if (fileName.StartsWith("Qt"))
                fileName = "Qt5" + fileName.Substring(2);
            if (isDebugCfg)
                fileName += "d";
            fileName += ".dll";
            return fileName;
        }

        private List<string> GetAdditionalLibs(bool isDebugCfg)
        {
            if (isDebugCfg && AdditionalLibrariesDebug.Count > 0)
                return AdditionalLibrariesDebug;
            return AdditionalLibraries;
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
            QtModuleInfo moduleInfo = null;
            InitQtModule(QtModule.Core, "QtCore", "QT_CORE_LIB");
            InitQtModule(QtModule.Multimedia, "QtMultimedia", "QT_MULTIMEDIA_LIB");
            InitQtModule(QtModule.Sql, "QtSql", "QT_SQL_LIB");
            InitQtModule(QtModule.Network, "QtNetwork", "QT_NETWORK_LIB");
            InitQtModule(QtModule.Xml, "QtXml", "QT_XML_LIB");
            InitQtModule(QtModule.Script, "QtScript", "QT_SCRIPT_LIB");
            InitQtModule(QtModule.XmlPatterns, "QtXmlPatterns", "QT_XMLPATTERNS_LIB");
            moduleInfo = InitQtModule(QtModule.ScriptTools, "QtScriptTools", "QT_SCRIPTTOOLS_LIB");
            moduleInfo = InitQtModule(QtModule.Designer, "QtDesigner", new string[] { "QDESIGNER_EXPORT_WIDGETS", "QT_DESIGNER_LIB" });
            moduleInfo = InitQtModule(QtModule.Main, "qtmain", "");
            moduleInfo.proVarQT = null;
            moduleInfo.HasDLL = false;
            moduleInfo.IncludePath = null;

            moduleInfo = InitQtModule(QtModule.Test, "QtTest", "QT_TESTLIB_LIB");
            moduleInfo.proVarQT = null;
            moduleInfo.proVarCONFIG = "qtestlib";

            moduleInfo = InitQtModule(QtModule.Help, "QtHelp", "QT_HELP_LIB");
            moduleInfo.proVarQT = null;
            moduleInfo.proVarCONFIG = "help";
            moduleInfo = InitQtModule(QtModule.WebKit, "QtWebKit", "");

            moduleInfo = InitQtModule(QtModule.Svg, "QtSvg", "QT_SVG_LIB");

            moduleInfo = InitQtModule(QtModule.Declarative, "QtDeclarative", "QT_DECLARATIVE_LIB");

            moduleInfo = InitQtModule(QtModule.OpenGL, "QtOpenGL", "QT_OPENGL_LIB");
            moduleInfo.AdditionalLibraries.Add("opengl32.lib");
            moduleInfo.AdditionalLibraries.Add("glu32.lib");

            moduleInfo = InitQtModule(QtModule.ActiveQtS, "QtAxServer", "QAXSERVER");
            moduleInfo.HasDLL = false;
            moduleInfo.IncludePath = "$(QTDIR)\\include\\ActiveQt";
            moduleInfo.AdditionalLibraries.Add("Qt5AxBase.lib");
            moduleInfo.AdditionalLibrariesDebug.Add("Qt5AxBased.lib");

            moduleInfo = InitQtModule(QtModule.ActiveQtC, "QtAxContainer", "");
            moduleInfo.HasDLL = false;
            moduleInfo.IncludePath = "$(QTDIR)\\include\\ActiveQt";
            moduleInfo.AdditionalLibraries.Add("Qt5AxBase.lib");
            moduleInfo.AdditionalLibrariesDebug.Add("Qt5AxBased.lib");

            moduleInfo = InitQtModule(QtModule.UiTools, "QtUiTools", "QT_UITOOLS_LIB");
            moduleInfo.HasDLL = false;

            // Qt5
            InitQtModule(QtModule.Widgets, "QtWidgets", "QT_WIDGETS_LIB");

            moduleInfo = InitQtModule(QtModule.Gui, "QtGui", "QT_GUI_LIB");

            InitQtModule(QtModule.ThreeD, "Qt3D", "QT_3D_LIB");
            InitQtModule(QtModule.Location, "QtLocation", "QT_LOCATION_LIB");

            InitQtModule(QtModule.Qml, "QtQml", "QT_QML_LIB");
            moduleInfo = InitQtModule(QtModule.Bluetooth, "QtBluetooth", "QT_BLUETOOTH_LIB");
            InitQtModule(QtModule.PrintSupport, "QtPrintSupport", "QT_PRINTSUPPORT_LIB");

            moduleInfo = InitQtModule(QtModule.Sensors, "QtSensors", "QT_SENSORS_LIB");
            InitQtModule(QtModule.Quick, "QtQuick", "QT_QUICK_LIB");

            InitQtModule(QtModule.ThreeDQuick, "Qt3DQuick", "QT_3DQUICK_LIB");

            InitQtModule(QtModule.WebkitWidgets, "QtWebkitWidgets", "QT_WEBKITWIDGETS_LIB");

            InitQtModule(QtModule.Concurrent, "QtConcurrent", "QT_CONCURRENT_LIB");
            InitQtModule(QtModule.MultimediaWidgets, "QtMultimediaWidgets", "QT_MULTIMEDIAWIDGETS_LIB");

            moduleInfo = InitQtModule(QtModule.Enginio, "Enginio", "QT_ENGINIO_LIB");

            InitQtModule(QtModule.Nfc, "QtNfc", "QT_NFC_LIB");
            InitQtModule(QtModule.Positioning, "QtPositioning", "QT_POSITIONING_LIB");
            InitQtModule(QtModule.SerialPort, "QtSerialPort", "QT_SERIALPORT_LIB");
            InitQtModule(QtModule.WebChannel, "QtWebChannel", "QT_WEBCHANNEL_LIB");
            moduleInfo = InitQtModule(QtModule.WebSockets, "QtWebSockets", "QT_WEBSOCKETS_LIB");
            InitQtModule(QtModule.WindowsExtras, "QtWinExtras", "QT_WINEXTRAS_LIB");
            InitQtModule(QtModule.QuickWidgets, "QtQuickWidgets", "QT_QUICKWIDGETS_LIB");

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
            foreach (string str in defines) {
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
