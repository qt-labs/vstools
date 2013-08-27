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

        var strTemplatePath = wizard.FindSymbol('TEMPLATES_PATH') + "\\";

        var vcfileTmp;
        var fileTmp;
        var strClass = wizard.FindSymbol('CLASS_NAME');
        var lstNamespaces = strClass.split('::');
        strClass = lstNamespaces.pop();
        var strHeader = wizard.FindSymbol('H_NAME');
        var strSource = wizard.FindSymbol('CPP_NAME');
        var strBase = wizard.FindSymbol('BASECLASS_NAME');
        var strForm = wizard.FindSymbol('UI_NAME');
        var strFolder = wizard.FindSymbol('LOCATION');
        var bMember = wizard.FindSymbol('MEMBER');
        var bMultipleInher = wizard.FindSymbol('MULTIPLEINHERITANCE');
        var bMemberPointer = wizard.FindSymbol('MEMBERPOINTER');

        var strTmp = "";
        var strNamespacesBegin = "";
        var strNamespacesEnd = "";
        for (i in lstNamespaces) {
            strNamespacesBegin += "namespace " + lstNamespaces[i] + " {\r\n";
            strNamespacesEnd = "} // namespace " + lstNamespaces[i] + "\r\n" + strNamespacesEnd;
        }

        var regexp = /\W/g;
        var strDef = QtEngine.GetFileName(strHeader).toUpperCase().replace(regexp, "_");

        var strFormName = GetNameFromFile(strForm);

        QtEngine.UseSelectedProject(wizard.dte);

        // class.cpp
        if (bMember)
            fileTmp = QtEngine.CopyFileToFolder(strTemplatePath + "class.cpp", strFolder, strSource);
        else if (bMultipleInher)
            fileTmp = QtEngine.CopyFileToFolder(strTemplatePath + "mi_class.cpp", strFolder, strSource);
        else if (bMemberPointer)
            fileTmp = QtEngine.CopyFileToFolder(strTemplatePath + "mp_class.cpp", strFolder, strSource);
        if (QtEngine.UsesPrecompiledHeaders()) {
            var pchFile = QtEngine.GetPrecompiledHeaderThrough();
            QtEngine.ReplaceTokenInFile(fileTmp, "%INCLUDE%", pchFile + "\"\n#include \"%INCLUDE%");
        }
        QtEngine.ReplaceTokenInFile(fileTmp, "%INCLUDE%", strHeader);
        QtEngine.ReplaceTokenInFile(fileTmp, "%CLASS%", strClass);
        QtEngine.ReplaceTokenInFile(fileTmp, "%BASECLASS%", strBase);
        QtEngine.ReplaceTokenInFile(fileTmp, "%UI_HDR%", "ui_" + QtEngine.GetFileName(strFormName) + ".h");
        strTmp = strNamespacesBegin;
        if (strTmp != "")
            strTmp += "\r\n";
        QtEngine.ReplaceTokenInFile(fileTmp, "%NAMESPACE_BEGIN%", strTmp);
        strTmp = strNamespacesEnd;
        if (strTmp != "")
            strTmp = "\r\n" + strTmp;
        QtEngine.ReplaceTokenInFile(fileTmp, "%NAMESPACE_END%", strTmp);
        vcfileTmp = QtEngine.AddFileToProject(fileTmp, "QT_SOURCE_FILTER");

        // class.h
        if (bMember)
            fileTmp = QtEngine.CopyFileToFolder(strTemplatePath + "class.h", strFolder, strHeader);
        else if (bMultipleInher)
            fileTmp = QtEngine.CopyFileToFolder(strTemplatePath + "mi_class.h", strFolder, strHeader);
        else if (bMemberPointer)
            fileTmp = QtEngine.CopyFileToFolder(strTemplatePath + "mp_class.h", strFolder, strHeader);
        QtEngine.ReplaceTokenInFile(fileTmp, "%PRE_DEF%", strDef);
        QtEngine.ReplaceTokenInFile(fileTmp, "%CLASS%", strClass);
        QtEngine.ReplaceTokenInFile(fileTmp, "%BASECLASS%", strBase);
        QtEngine.ReplaceTokenInFile(fileTmp, "%UI_HDR%", "ui_" + QtEngine.GetFileName(strFormName) + ".h");
        strTmp = strNamespacesBegin;
        if (strTmp != "")
            strTmp += "\r\n";
        QtEngine.ReplaceTokenInFile(fileTmp, "%NAMESPACE_BEGIN%", strTmp);
        strTmp = strNamespacesEnd;
        if (strTmp != "")
            strTmp += "\r\n";
        QtEngine.ReplaceTokenInFile(fileTmp, "%NAMESPACE_END%", strTmp);
        vcfileTmp = QtEngine.AddFileToProject(fileTmp, "QT_HEADER_FILTER");

        // form.ui
        fileTmp = QtEngine.CopyFileToFolder(strTemplatePath + "form.ui", strFolder, strForm);
        QtEngine.ReplaceTokenInFile(fileTmp, "%CLASS%", strClass);
        QtEngine.ReplaceTokenInFile(fileTmp, "%BASECLASS%", strBase);
        if (strBase == "QMainWindow") {
            QtEngine.ReplaceTokenInFile(fileTmp, "%CENTRAL_WIDGET%",
                "\r\n  <widget class=\"QMenuBar\" name=\"menuBar\" />" +
                "\r\n  <widget class=\"QToolBar\" name=\"mainToolBar\" />" +
                "\r\n  <widget class=\"QWidget\" name=\"centralWidget\" />" +
                "\r\n  <widget class=\"QStatusBar\" name=\"statusBar\" />");
        } else if (strBase == "QDockWidget") {
            QtEngine.ReplaceTokenInFile(fileTmp, "%CENTRAL_WIDGET%",
                "\r\n  <widget class=\"QWidget\" name=\"widget\" />");
        } else {
            QtEngine.ReplaceTokenInFile(fileTmp, "%CENTRAL_WIDGET%", "");
        }
        vcfileTmp = QtEngine.AddFileToProject(fileTmp, "QT_FORM_FILTER");
    }
    catch (e) {
        wizard.ReportError("Exception in 'default.js'");
        if (e.description.length != 0)
            SetErrorInfo(e);
        return e.number
    }
}
