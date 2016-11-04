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

namespace QtProjectLib
{
    public class QtModules
    {
        private static QtModules instance = new QtModules();
        private readonly Dictionary<string, QtModule> dictModulesByDLL = new Dictionary<string, QtModule>();
        private readonly Dictionary<QtModule, QtModuleInfo> dictModuleInfos = new Dictionary<QtModule, QtModuleInfo>();

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
            var lst = new List<QtModuleInfo>(dictModuleInfos.Count);
            foreach (var entry in dictModuleInfos)
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
            moduleInfo = InitQtModule(QtModule.Designer, "QtDesigner", new[] { "QDESIGNER_EXPORT_WIDGETS", "QT_DESIGNER_LIB" });
            moduleInfo = InitQtModule(QtModule.Main, "qtmain", string.Empty);
            moduleInfo.proVarQT = null;
            moduleInfo.HasDLL = false;
            moduleInfo.IncludePath = null;

            moduleInfo = InitQtModule(QtModule.Test, "QtTest", "QT_TESTLIB_LIB");
            moduleInfo.proVarQT = null;
            moduleInfo.proVarCONFIG = "qtestlib";

            moduleInfo = InitQtModule(QtModule.Help, "QtHelp", "QT_HELP_LIB");
            moduleInfo.proVarQT = null;
            moduleInfo.proVarCONFIG = "help";
            moduleInfo = InitQtModule(QtModule.WebKit, "QtWebKit", string.Empty);

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

            moduleInfo = InitQtModule(QtModule.ActiveQtC, "QtAxContainer", string.Empty);
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
            return InitQtModule(moduleId, libraryPrefix, new[] { define });
        }

        private QtModuleInfo InitQtModule(QtModule moduleId, string libraryPrefix, string[] defines)
        {
            var moduleInfo = new QtModuleInfo(moduleId);
            moduleInfo.LibraryPrefix = libraryPrefix;
            moduleInfo.IncludePath = "$(QTDIR)\\include\\" + libraryPrefix;
            moduleInfo.Defines = new List<string>();
            dictModulesByDLL.Add(libraryPrefix, moduleId);
            foreach (var str in defines) {
                if (string.IsNullOrEmpty(str))
                    continue;
                moduleInfo.Defines.Add(str);
            }
            dictModuleInfos.Add(moduleId, moduleInfo);

            if (libraryPrefix.StartsWith("Qt", StringComparison.Ordinal))
                moduleInfo.proVarQT = libraryPrefix.Substring(2).ToLower();
            else
                moduleInfo.proVarQT = libraryPrefix.ToLower();

            return moduleInfo;
        }

    }
}
