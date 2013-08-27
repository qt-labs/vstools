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
        var strHeader = wizard.FindSymbol('H_NAME');
        var strSource = wizard.FindSymbol('CPP_NAME');
        var strBase = wizard.FindSymbol('BASECLASS_NAME');
        var strSignature = wizard.FindSymbol('SIGNATURE');
        var bInsertQObject = wizard.FindSymbol('INSERT_QOBJECT');
        var strFolder = wizard.FindSymbol('LOCATION');

        var lstNamespaces = strClass.split('::');
        strClass = lstNamespaces.pop();

        var strTmp = "";
        var strNamespacesBegin = "";
        var strNamespacesEnd = "";
        for (i in lstNamespaces) {
            strNamespacesBegin += "namespace " + lstNamespaces[i] + " {\r\n";
            strNamespacesEnd = "} // namespace " + lstNamespaces[i] + "\r\n" + strNamespacesEnd;
        }

        if (strBase == "") {
            strSignature = 0;
            bInsertQObject = false;
        }

        var regexp = /\W/g;
        var strDef = QtEngine.GetFileName(strHeader).toUpperCase().replace(regexp, "_");

        QtEngine.UseSelectedProject(wizard.dte);

        fileTmp = QtEngine.CopyFileToFolder(strTemplatePath + "class.cpp", strFolder, strSource);

        if (QtEngine.UsesPrecompiledHeaders()) {
            var pchFile = QtEngine.GetPrecompiledHeaderThrough();
            QtEngine.ReplaceTokenInFile(fileTmp, "%INCLUDE%", pchFile + "\"\n#include \"%INCLUDE%");
        }
        QtEngine.ReplaceTokenInFile(fileTmp, "%INCLUDE%", strHeader);
        QtEngine.ReplaceTokenInFile(fileTmp, "%CLASS%", strClass);
        if (strSignature == 1) {
            QtEngine.ReplaceTokenInFile(fileTmp, "%BASEIMPL%", "\r\n\t: " + strBase + "(parent)");
            QtEngine.ReplaceTokenInFile(fileTmp, "%CTORSIG%", "QObject *parent");
        } else if (strSignature == 2) {
            QtEngine.ReplaceTokenInFile(fileTmp, "%BASEIMPL%", "\r\n\t: " + strBase + "(parent)");
            QtEngine.ReplaceTokenInFile(fileTmp, "%CTORSIG%", "QWidget *parent");
        } else {
            var baseImplReplacement = "";
            if (strBase != "")
                baseImplReplacement = "\r\n\t: " + strBase + "()";
            QtEngine.ReplaceTokenInFile(fileTmp, "%BASEIMPL%", baseImplReplacement);
            QtEngine.ReplaceTokenInFile(fileTmp, "%CTORSIG%", "");
        }
        strTmp = strNamespacesBegin;
        if (strTmp != "")
            strTmp += "\r\n";
        QtEngine.ReplaceTokenInFile(fileTmp, "%NAMESPACE_BEGIN%", strTmp);
        strTmp = strNamespacesEnd;
        if (strTmp != "")
            strTmp = "\r\n" + strTmp;
        QtEngine.ReplaceTokenInFile(fileTmp, "%NAMESPACE_END%", strTmp);
        vcfileTmp = QtEngine.AddFileToProject(fileTmp, "QT_SOURCE_FILTER");

        fileTmp = QtEngine.CopyFileToFolder(strTemplatePath + "class.h", strFolder, strHeader);
        QtEngine.ReplaceTokenInFile(fileTmp, "%PRE_DEF%", strDef);
        QtEngine.ReplaceTokenInFile(fileTmp, "%CLASS%", strClass);
        QtEngine.ReplaceTokenInFile(fileTmp, "%BASECLASS%", strBase);
        if (strBase == "") {
            QtEngine.ReplaceTokenInFile(fileTmp, "%BASEDECL%", "");
            QtEngine.ReplaceTokenInFile(fileTmp, "%BASECLASSINCLUDE%", "");
        } else {
            QtEngine.ReplaceTokenInFile(fileTmp, "%BASEDECL%", " : public " + strBase);
            QtEngine.ReplaceTokenInFile(fileTmp, "%BASECLASSINCLUDE%", "#include <" + strBase + ">\r\n\r\n");
        }
        if (bInsertQObject)
            QtEngine.ReplaceTokenInFile(fileTmp, "%Q_OBJECT%", "\r\n\tQ_OBJECT\r\n");
        else
            QtEngine.ReplaceTokenInFile(fileTmp, "%Q_OBJECT%", "");
        if (strSignature == 1)
            QtEngine.ReplaceTokenInFile(fileTmp, "%CTORSIG%", "QObject *parent");
        else if (strSignature == 2)
            QtEngine.ReplaceTokenInFile(fileTmp, "%CTORSIG%", "QWidget *parent");
        else
            QtEngine.ReplaceTokenInFile(fileTmp, "%CTORSIG%", "");
        strTmp = strNamespacesBegin;
        if (strTmp != "")
            strTmp += "\r\n";
        QtEngine.ReplaceTokenInFile(fileTmp, "%NAMESPACE_BEGIN%", strTmp);
        strTmp = strNamespacesEnd;
        if (strTmp != "")
            strTmp += "\r\n";
        QtEngine.ReplaceTokenInFile(fileTmp, "%NAMESPACE_END%", strTmp);
        vcfileTmp = QtEngine.AddFileToProject(fileTmp, "QT_HEADER_FILTER");
    }
    catch (e) {
        wizard.ReportError("Exception in 'default.js'");
        if (e.description.length != 0)
            SetErrorInfo(e);
        return e.number
    }
}
