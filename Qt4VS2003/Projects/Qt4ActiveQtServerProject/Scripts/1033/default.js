var QtEngine;

function GetNameFromFile(strFile)
{
    var nPos = strFile.lastIndexOf(".");
    return strFile.substr(0,nPos);
}

function OnFinish(selProj, selObj)
{
    try
    {
        // load right project engine
        var dte = wizard.dte;
        var version = dte.version;
        if (version == "8.0")
            QtEngine = new ActiveXObject("Nokia.QtProjectEngine80");
    	else if (version == "9.0")
            QtEngine = new ActiveXObject("Nokia.QtProjectEngine90");
    	else if (version == "10.0")
            QtEngine = new ActiveXObject("Nokia.QtProjectEngine100");

        var strProjectPath = wizard.FindSymbol('PROJECT_PATH');
        var strProjectName = wizard.FindSymbol('PROJECT_NAME');
        var strSolutionName = wizard.FindSymbol('VS_SOLUTION_NAME');
        var strTemplatePath = wizard.FindSymbol('TEMPLATES_PATH') + "\\";
        var bExclusive = wizard.FindSymbol("CLOSE_SOLUTION");

        var vcfileTmp;
        var fileTmp;
        var strClass = wizard.FindSymbol('CLASSNAME_TEXT');
        var strHeader = wizard.FindSymbol('HFILE_TEXT');
        var strSource = wizard.FindSymbol('CPPFILE_TEXT');
        var strForm = wizard.FindSymbol('UIFILE_TEXT');
        var bPrecompiled = wizard.FindSymbol('PRECOMPILED_HEADERS');

        var regexp = /\W/g;
        var strDef = strHeader.toUpperCase().replace(regexp,"_");

        var strFormName = GetNameFromFile(strForm);

        var regexp = /\s/g;
        var strProName = strProjectName.toLowerCase().replace(regexp,"");

        QtEngine.CreateLibraryProject(wizard.dte, strProjectName,
            strProjectPath, strSolutionName, bExclusive, false, bPrecompiled);

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

        // source.cpp
        fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "source.cpp", strSource);
        QtEngine.ReplaceTokenInFile(fileTmp, "%INCLUDE%", strHeaderInclude);
        QtEngine.ReplaceTokenInFile(fileTmp, "%CLASS%", strClass);
        for (var i=0; i<5; i++)
            QtEngine.ReplaceTokenInFile(fileTmp, "%GUID" + i + "%", QtEngine.CreateNewGUID());
        vcfileTmp = QtEngine.AddFileToProject(fileTmp, "QT_SOURCE_FILTER");

        // source.h
        fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "source.h", strHeader);
        QtEngine.ReplaceTokenInFile(fileTmp, "%PRE_DEF%", strDef);
        QtEngine.ReplaceTokenInFile(fileTmp, "%CLASS%", strClass);
        QtEngine.ReplaceTokenInFile(fileTmp, "%UI_HDR%", "ui_" + strFormName + ".h");
        vcfileTmp = QtEngine.AddFileToProject(fileTmp, "QT_HEADER_FILTER");

        // widget.ui
        fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "widget.ui", strForm);
        QtEngine.ReplaceTokenInFile(fileTmp, "%CLASS%", strClass);
        vcfileTmp = QtEngine.AddFileToProject(fileTmp, "QT_FORM_FILTER");

        // server.rc
        fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "server.rc",
            strProName + ".rc");
        QtEngine.ReplaceTokenInFile(fileTmp, "%PRO_NAME%", strProjectName);
        QtEngine.AddFileToProject(fileTmp, "NONE");

        // server.ico
        fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "server.ico",
            strProName + ".ico");
        QtEngine.AddFileToProject(fileTmp, "NONE");

        // server.def
        fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "server.def",
            strProName + ".def");
        QtEngine.AddFileToProject(fileTmp, "QT_SOURCE_FILTER");

        QtEngine.AddActiveQtBuildStep("1.0.0");

        QtEngine.Finish();
    }
    catch(e)
    {
        if (e.description.length != 0)
            SetErrorInfo(e);
        return e.number
    }
}

function AddModules()
{
    if (wizard.FindSymbol('CORE_MODULE'))
        QtEngine.AddModule("QtCore");
    if (wizard.FindSymbol('GUI_MODULE'))
        QtEngine.AddModule("QtGui");
    if (wizard.FindSymbol('MULTIMEDIA_MODULE'))
        QtEngine.AddModule("QtMultimedia");
    if (wizard.FindSymbol('XML_MODULE'))
        QtEngine.AddModule("QtXml");
    if (wizard.FindSymbol('SQL_MODULE'))
        QtEngine.AddModule("QtSql");
    if (wizard.FindSymbol('OPENGL_MODULE'))
        QtEngine.AddModule("QtOpenGL");
    if (wizard.FindSymbol('NETWORK_MODULE'))
        QtEngine.AddModule("QtNetwork");
    if (wizard.FindSymbol('SCRIPT_MODULE'))
        QtEngine.AddModule("QtScript");
    if (wizard.FindSymbol('COMPAT_MODULE'))
        QtEngine.AddModule("Qt3Support");
    if (wizard.FindSymbol('AQSERVER_MODULE'))
        QtEngine.AddModule("QAxServer");
    if (wizard.FindSymbol('AQCONTAINER_MODULE'))
        QtEngine.AddModule("QAxContainer");
    if (wizard.FindSymbol('SVG_MODULE'))
        QtEngine.AddModule("QtSvg");
    if (wizard.FindSymbol('HELP_MODULE'))
        QtEngine.AddModule("QtHelp");
    if (wizard.FindSymbol('WEBKIT_MODULE'))
        QtEngine.AddModule("QtWebKit");
    if (wizard.FindSymbol('XMLPATTERNS_MODULE'))
        QtEngine.AddModule("QtXmlPatterns");
    if (wizard.FindSymbol('TEST_MODULE'))
        QtEngine.AddModule("QtTest");
    if (wizard.FindSymbol('PHONON_MODULE'))
        QtEngine.AddModule("phonon");
}
