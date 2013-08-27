var QtEngine;

function GetNameFromFile(strFile) {
    var nPos = strFile.lastIndexOf(".");
    return strFile.substr(0, nPos);
}

function OnFinish(selProj, selObj) {
    try {
        // load right project engine
        var dte = wizard.dte;
        var version = dte.version;
        if (version == "8.0")
            QtEngine = new ActiveXObject("Digia.Qt5ProjectEngine80");
        else if (version == "9.0")
            QtEngine = new ActiveXObject("Digia.Qt5ProjectEngine90");
        else if (version == "10.0")
            QtEngine = new ActiveXObject("Digia.Qt5ProjectEngine100");
        else if (version == "11.0")
            QtEngine = new ActiveXObject("Digia.Qt5ProjectEngine110");
        else if (version == "12.0")
            QtEngine = new ActiveXObject("Digia.Qt5ProjectEngine120");

        var strProjectPath = wizard.FindSymbol('PROJECT_PATH');
        var strProjectName = wizard.FindSymbol('PROJECT_NAME');
        var strSolutionName = wizard.FindSymbol('VS_SOLUTION_NAME');
        var strTemplatePath = wizard.FindSymbol('TEMPLATES_PATH') + "\\";
        var bExclusive = wizard.FindSymbol("CLOSE_SOLUTION");
        var bStaticLib = wizard.FindSymbol("STATICLIB_CHECK");

        var fileTmp;
        var strClass = wizard.FindSymbol('CLASSNAME_TEXT');
        var strHeader = wizard.FindSymbol('HFILE_TEXT');
        var strSource = wizard.FindSymbol('CPPFILE_TEXT');
        var bPrecompiled = wizard.FindSymbol('PRECOMPILED_HEADERS');

        var regexp = /\W/g;
        var strDef = strHeader.toUpperCase().replace(regexp, "_");
        regexp = /\s/g;
        var strProName = strProjectName.toUpperCase().replace(regexp, "");

        var strDefine = strProName + "_LIB";
        var strExport = strProName + "_EXPORT";
        var strDefFile = strProName.toLowerCase() + "_global.h"; var strDefGuard = strProName + "_GLOBAL_H";

        QtEngine.CreateWinCELibraryProject(wizard.dte, strProjectName,
            strProjectPath, strSolutionName, bExclusive, wizard.FindSymbol("PLATFORM_COMBO"), bStaticLib, bPrecompiled);

        // add the selected modules to the project
        AddModules();

        var strHeaderInclude = strHeader;
        if (bPrecompiled) {
            strHeaderInclude = "stdafx.h\"\n#include \"" + strHeader;
            fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "stdafx.cpp", "stdafx.cpp");
            QtEngine.AddFileToProject(fileTmp, "QT_SOURCE_FILTER");
            fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "stdafx.h", "stdafx.h");
            QtEngine.AddFileToProject(fileTmp, "QT_HEADER_FILTER");
        }


        // add a define
        QtEngine.AddDefine(strDefine, "BOTH");

        // source.cpp
        fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "source.cpp", strSource);
        QtEngine.ReplaceTokenInFile(fileTmp, "%INCLUDE%", strHeaderInclude);
        QtEngine.ReplaceTokenInFile(fileTmp, "%CLASSNAME%", strClass);
        QtEngine.AddFileToProject(fileTmp, "QT_SOURCE_FILTER");

        // header.h
        fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "header.h", strHeader);
        QtEngine.ReplaceTokenInFile(fileTmp, "%PRE_DEF%", strDef);
        QtEngine.ReplaceTokenInFile(fileTmp, "%CLASSNAME%", strClass);
        if (bStaticLib) {
            QtEngine.ReplaceTokenInFile(fileTmp, "%PRO_LIB_EXPORT% ", "");
            QtEngine.ReplaceTokenInFile(fileTmp, "\n#include \"%DEF_HEADER%\"", "");
        }
        else {
            QtEngine.ReplaceTokenInFile(fileTmp, "%PRO_LIB_EXPORT%", strExport);
            QtEngine.ReplaceTokenInFile(fileTmp, "%DEF_HEADER%", strDefFile);
        }
        QtEngine.AddFileToProject(fileTmp, "QT_HEADER_FILTER");

        // defines.h
        if (!bStaticLib) {
            fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "defines.h", strDefFile);

            QtEngine.ReplaceTokenInFile(fileTmp, "%PRE_DEF%", strDefGuard);
            QtEngine.ReplaceTokenInFile(fileTmp, "%PRO_LIB_DEFINE%", strDefine);
            QtEngine.ReplaceTokenInFile(fileTmp, "%PRO_LIB_EXPORT%", strExport);
            QtEngine.AddFileToProject(fileTmp, "QT_HEADER_FILTER");
        }

        QtEngine.Finish();
    }
    catch (e) {
        if (e.description.length != 0)
            SetErrorInfo(e);
        return e.number
    }
}

function AddModules() {
    // Essential modules
    if (wizard.FindSymbol('THREED_MODULE'))
        QtEngine.AddModule("Qt3D");
    if (wizard.FindSymbol('CORE_MODULE'))
        QtEngine.AddModule("QtCore");
    if (wizard.FindSymbol('GUI_MODULE'))
        QtEngine.AddModule("QtGui");
    if (wizard.FindSymbol('LOCATION_MODULE'))
        QtEngine.AddModule("QtLocation");
    if (wizard.FindSymbol('MULTIMEDIA_MODULE'))
        QtEngine.AddModule("QtMultimedia");
    if (wizard.FindSymbol('MULTIMEDIAWIDGETS_MODULE'))
        QtEngine.AddModule("QtMultimediaWidgets");
    if (wizard.FindSymbol('NETWORK_MODULE'))
        QtEngine.AddModule("QtNetwork");
    if (wizard.FindSymbol('QML_MODULE'))
        QtEngine.AddModule("QtQml");
    if (wizard.FindSymbol('QUICK_MODULE'))
        QtEngine.AddModule("QtQuick");
    if (wizard.FindSymbol('SQL_MODULE'))
        QtEngine.AddModule("QtSql");
    if (wizard.FindSymbol('TEST_MODULE'))
        QtEngine.AddModule("QtTest");
    if (wizard.FindSymbol('WEBKIT_MODULE'))
        QtEngine.AddModule("QtWebKit"); // ??

    // Add-on modules
    // Active Qt better split to server and container
    if (wizard.FindSymbol('AQCONTAINER_MODULE'))
        QtEngine.AddModule("QtAxContainer");
    if (wizard.FindSymbol('AQSERVER_MODULE'))
        QtEngine.AddModule("QtAxServer");
    if (wizard.FindSymbol('BLUETOOTH_MODULE'))
        QtEngine.AddModule("QtBluetooth");
    if (wizard.FindSymbol('CONTACTS_MODULE'))
        QtEngine.AddModule("QtContacts");
    if (wizard.FindSymbol('CONCURRENT_MODULE'))
        QtEngine.AddModule("QtConcurrent");
    if (wizard.FindSymbol('HELP_MODULE'))
        QtEngine.AddModule("QtHelp");
    if (wizard.FindSymbol('OPENGL_MODULE'))
        QtEngine.AddModule("QtOpenGL");
    if (wizard.FindSymbol('ORGANIZER_MODULE'))
        QtEngine.AddModule("QtOrganizer");
    if (wizard.FindSymbol('PHONON_MODULE'))
        QtEngine.AddModule("phonon");
    if (wizard.FindSymbol('PRINTSUPPORT_MODULE'))
        QtEngine.AddModule("QtPrintSupport");
    if (wizard.FindSymbol('PUBSUB_MODULE'))
        QtEngine.AddModule("QtPublishSubscribe");
    if (wizard.FindSymbol('DECLARATIVE_MODULE'))
        QtEngine.AddModule("QtDeclarative");
    if (wizard.FindSymbol('SCRIPT_MODULE'))
        QtEngine.AddModule("QtScript");
    if (wizard.FindSymbol('SENSORS_MODULE'))
        QtEngine.AddModule("QtSensors");
    if (wizard.FindSymbol('SERVICEFRAMEWORK_MODULE'))
        QtEngine.AddModule("QtServiceFramework");
    if (wizard.FindSymbol('SVG_MODULE'))
        QtEngine.AddModule("QtSvg");
    if (wizard.FindSymbol('SYSTEMINFO_MODULE'))
        QtEngine.AddModule("QtSystemInfo");
    if (wizard.FindSymbol('VERSIT_MODULE'))
        QtEngine.AddModule("QtVersit");
    if (wizard.FindSymbol('WEBKITWIDGETS_MODULE'))
        QtEngine.AddModule("QtWebkitWidgets"); // ??
    if (wizard.FindSymbol('WIDGETS_MODULE'))
        QtEngine.AddModule("QtWidgets");
    if (wizard.FindSymbol('XML_MODULE'))
        QtEngine.AddModule("QtXml");
    if (wizard.FindSymbol('XMLPATTERNS_MODULE'))
        QtEngine.AddModule("QtXmlPatterns");
}
