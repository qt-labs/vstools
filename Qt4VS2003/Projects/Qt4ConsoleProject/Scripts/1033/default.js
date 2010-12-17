var QtEngine;

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
        var strTemplatePath = wizard.FindSymbol('TEMPLATES_PATH');
        var bExclusive = wizard.FindSymbol("CLOSE_SOLUTION");
        var bPrecompiled = wizard.FindSymbol('PRECOMPILED_HEADERS');
        var fileTmp;

        QtEngine.CreateConsoleProject(wizard.dte, strProjectName,
            strProjectPath, strSolutionName, bExclusive, bPrecompiled);

        // add the selected modules to the project
        AddModules();

        var strHeaderInclude = "";
        if (bPrecompiled) {
            strHeaderInclude = "#include \"stdafx.h\"";
            fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "\\stdafx.cpp", "stdafx.cpp");
            QtEngine.AddFileToProject(fileTmp, "QT_SOURCE_FILTER");
            fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "\\stdafx.h", "stdafx.h");
            QtEngine.AddFileToProject(fileTmp, "QT_HEADER_FILTER");
        }

        // add files to the project
        fileTmp = QtEngine.CopyFileToProjectFolder(strTemplatePath + "\\main.cpp", "main.cpp");
        QtEngine.ReplaceTokenInFile(fileTmp, "%INCLUDE%", strHeaderInclude);
        QtEngine.AddFileToProject(fileTmp, "QT_SOURCE_FILTER");

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
    if (wizard.FindSymbol('DECLARATIVE_MODULE'))
        QtEngine.AddModule("QtDeclarative");
    if (wizard.FindSymbol('PHONON_MODULE'))
        QtEngine.AddModule("phonon");
}
