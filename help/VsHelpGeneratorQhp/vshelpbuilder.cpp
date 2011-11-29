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

#include <math.h>
#include <QtCore/QTextStream>
#include <QtCore/QDir>
#include <QtCore/QDirIterator>
#include <QtCore/QDateTime>
#include <QTextCodec>
#include <QDebug>

#include "vshelpbuilder.h"
#include "qhelpprojectdata_p.h"

VSHelpBuilder::VSHelpBuilder(VSHelpBuilder::Kind kind, const QString &srcDir,
                             const QString &version, const QString &title)
: m_Kind(kind), m_SrcDir(srcDir + "/"), m_Title(title), m_DocSetTitle(title),
m_FilterName(title)
{
    m_outCodec = QTextCodec::codecForName("UTF-8");
    if (!m_outCodec) {
		fprintf(stderr, "\nCannot create the UTF-8 text codec.\n");
        exit(128);
    }

    m_Version = version;
	m_Version = m_Version.replace('.', '_');
    m_OutPath = QDir::temp().absolutePath();
    if (m_Kind == VSHelpBuilder::VS) {
        m_UniqueID = "TrolltechAS_Qt4VS_Help_" + m_Version;    
        m_FileRoot = "qt4vs_" + version;
        m_Namespace = "TrolltechAS.Qt4VS." + m_Version + ".1033";
        m_DocSet = "qt4vsdoc" + m_Version;
	    m_LinkGroup = "qt4vsdoclg" + m_Version;
        m_LinkGroupTitle = "Qt Integration " + m_Version;
    } else {
        m_UniqueID = "TrolltechAS_Qt_Help_" + m_Version;
        m_FileRoot = "qt_" + version;
        m_Namespace = "TrolltechAS.Qt." + m_Version + ".1033";
        m_DocSet = "qtrefdoc" + m_Version;
	    m_LinkGroup = "qtrefdoclg" + m_Version;
        m_LinkGroupTitle = "Qt Help " + m_Version;
    }
	m_FileVersion = "1.0.0.0";	
    m_LinkGroupPriority = "1300";
#if _MSC_VER < 1400
    if (m_Kind == VSHelpBuilder::VS)
        m_DynamicHelpDefaultLink = "ms-help://MS.VSCC.2003/" + m_Namespace + "/" + m_UniqueID + "/html/vs2003integration.html";
    else
	    m_DynamicHelpDefaultLink = "ms-help://MS.VSCC.2003/" + m_Namespace + "/" + m_UniqueID + "/html/index.html";	
#else
	if (m_Kind == VSHelpBuilder::VS)
        m_DynamicHelpDefaultLink = "ms-help://MS.VSCC.v80/" + m_Namespace + "/" + m_UniqueID + "/html/vs2005integration.html";
    else
	    m_DynamicHelpDefaultLink = "ms-help://MS.VSCC.v80/" + m_Namespace + "/" + m_UniqueID + "/html/index.html";	
#endif

    QFile comp;
    bool foundCompiler = false;
    
    m_HelpSDKDir = QLatin1String(qgetenv("ProgramFiles")) + QLatin1String("\\Microsoft Help 2.0 SDK");
    comp.setFileName(m_HelpSDKDir + "\\hxcomp.exe");
    if (comp.exists())
        foundCompiler = true;

    if (!foundCompiler) {
        QStringList env = QProcess::systemEnvironment();
        foreach (QString str, env) {
            if (str.startsWith("VS_SDK_LOCATION")) {
                QStringList lst = str.split('=');
                if (lst.count() > 1) {
                    m_HelpSDKDir = lst.at(1) + "\\VisualStudioIntegration\\Archive\\HelpIntegration";
                    comp.setFileName(m_HelpSDKDir + "\\hxcomp.exe");
                    if (comp.exists())
                        foundCompiler = true;
                }
                break;
            }
        }
    }

    if (!foundCompiler) {
	    m_HelpSDKDir = QLatin1String(qgetenv("ProgramFiles")) + QLatin1String("\\Common Files\\microsoft shared\\Help 2.0 Compiler");
        comp.setFileName(m_HelpSDKDir + "\\hxcomp.exe");
        if (comp.exists())
            foundCompiler = true;
    }
    if (!foundCompiler) {
        fprintf(stderr, "The Microsoft Help Compiler could not be found.");
	    exit(128);
    }
    
    process = new QProcess();
    connect(process, SIGNAL(finished(int)), SLOT(compilationFinished(int)));
    connect(process, SIGNAL(started()), SLOT(compilationStarted()));
    connect(process, SIGNAL(error(QProcess::ProcessError)), SLOT(compilationProcessError(QProcess::ProcessError)));
}

bool VSHelpBuilder::init()
{
    QString fileName = QLatin1String("qt.qhp");
    if (m_Kind == VSHelpBuilder::VS)
        fileName = QLatin1String("vs-addin.qhp");
        
    if (!m_helpData.readData(m_SrcDir + fileName)) {
        fprintf(stderr, "Cannot read file %s\n.", qPrintable(m_SrcDir + fileName));
        return false;
    }
    return true;
}

bool VSHelpBuilder::createDocumentation(BuildMode mode)
{
    QDir d(m_OutPath);
    if (!d.exists())
        d.mkdir(QDir::cleanPath(m_OutPath));

    if ((mode & VSHelpBuilder::CopyModifyFiles) > 0) {
		copyAndModifyFiles();
		if (mode == VSHelpBuilder::CopyModifyFiles) {
			return false;
		}
	}
	if ((mode & VSHelpBuilder::CreateCollectionFiles) > 0) {
		writeProjectFile();
		writeHelpFileListFile();
        writeHelpCollectionFile();
		writeIndexFiles();
		writeTableOfContents();
		writeVirtualTopics();
		writeLinkGroup();
		writeCollectionLevelFiles();
		writeH2RegFile();
        writeExternalH2RegFile();
		if (mode == VSHelpBuilder::CreateCollectionFiles
			|| mode == (VSHelpBuilder::CopyModifyFiles|VSHelpBuilder::CreateCollectionFiles)) {
			return false;
			}
	}
    if ((mode & VSHelpBuilder::Compile) > 0) {
		compile();
        return true;
	}
    return false;
}

void VSHelpBuilder::compilationStarted()
{
    fprintf(stdout, "Starting help compilation...\n");
}

void VSHelpBuilder::compilationFinished(int)
{
    QFile log(logFile);
    if (log.open(QIODevice::ReadOnly)) {
        QString output = QString::fromLocal8Bit(log.readAll());
        fprintf(stdout, "%s\n", qPrintable(output));
        log.close();        
    }
    emit finished();	
}

void VSHelpBuilder::compilationProcessError(QProcess::ProcessError)
{
	fprintf(stderr, "An error occured when starting the process.");
	exit(128);
}

void VSHelpBuilder::setOutPath(const QString &path)
{
	m_OutPath = path;
	QString filePrefix;
    if (m_Kind == VSHelpBuilder::VS)
        filePrefix = "qt4vs_";
    else
        filePrefix = "qt_";
    QString rootFileName = m_OutPath + "\\" + filePrefix + m_Version;
	projectFile = rootFileName + ".HWProj";
	HxSFile = rootFileName + ".HxS";
	HxCFile = rootFileName + ".HxC";
	HxFFile = rootFileName + ".HxF";
	HxTFile = rootFileName + ".HxT";
	HxKKFile = rootFileName + "K.HxK";
	HxKFFile = rootFileName + "F.HxK";
	HxVFile = rootFileName + ".HxV";
	groupDefFile = rootFileName + ".xml";


	HxCColFile = filePrefix + m_Version + "C.HxC";
	HxTColFile = filePrefix + m_Version + "C.HxT";
	HxKKColFile = filePrefix + m_Version + "KC.HxK";
	HxKFColFile = filePrefix + m_Version + "FC.HxK";
	HxAColFile = filePrefix + m_Version + "C.HxA";

	H2RegFile = m_OutPath + "\\h2reg.ini";
    ExtH2RegFile = m_OutPath + "\\" + filePrefix + "h2reg.ini";

    logFile = m_OutPath + "/log.txt";
}

bool VSHelpBuilder::copyAndModifyFiles()
{
	QDir htmlDir(m_OutPath);
	if (!htmlDir.cd("html")) {
		htmlDir.mkpath("html");
		if (!htmlDir.cd("html"))
			return false;        
	}

    QString srcDir = m_SrcDir;
	QString destDir = htmlDir.absolutePath() + "/";
    QString line;
    bool noCheck;

    foreach (const QString &file, m_helpData.filterSections().first().files()) {
        if (!file.toLower().endsWith(".html")) {
            const QString sourceFile = srcDir + file;
            const QString destinyFile = destDir + file;
            QFileInfo fi(destinyFile);
            if (fi.exists()) {
                QDateTime lastModifiedDestiny = fi.lastModified();
                fi.setFile(sourceFile);
                QDateTime lastModifiedSource = fi.lastModified();
                if (lastModifiedSource > lastModifiedDestiny)
                    QFile::remove(destinyFile);
                else
                    continue;
            }
            QDir destinyDir = fi.dir();
            if (!destinyDir.exists() && !destinyDir.mkpath(destinyDir.absolutePath()))
                fprintf(stderr, "Error: Cannot create directory '%s'.", qPrintable(destinyDir.absolutePath()));
            if (!QFile::copy(sourceFile, destinyFile))
                fprintf(stderr, "Error: Cannot copy file '%s' to '%s'.\n", qPrintable(srcDir + file), qPrintable(destinyFile));
        }
    }

    // Copy HTML files by using a QDirIterator, because qt.qhp is inconsistent. :-(
    QDirIterator dirit(srcDir, QStringList() << "*.html", QDir::NoFilter, QDirIterator::Subdirectories);
    while (dirit.hasNext()) {
        QString absFileName = dirit.next();
        QString file = absFileName;
        file.remove(0, srcDir.length());
//        printf("*** %s\n", qPrintable(file));
        QFile srcFile(absFileName);
        QFile destFile(destDir + file);
        if (srcFile.open(QIODevice::ReadOnly)) {
            if (destFile.open(QIODevice::WriteOnly)) {
                QTextStream r(&srcFile);
                QTextStream w(&destFile);
                noCheck = false;
                while (!(line = r.readLine()).isNull())
                {
                    if (noCheck) {
                        w << line << endl;
                        continue;
                    }
                    if (line.simplified() == "<html>") {
                        w << "<html xmlns:MSHelp=\"http://msdn.microsoft.com/mshelp\">" << endl;
                    } else if (line.simplified() == "</head>") {
                        w << "<xml>" << endl;
                        w << "\t<MSHelp:Attr Name=\"DocSet\" Value=\"" << m_DocSet << "\"/>" << endl;
                        w << "\t<MSHelp:Attr Name=\"Locale\" Value=\"kbEnglish\"/>" << endl;
                        w << "\t<MSHelp:Attr Name=\"TopicType\" Value=\"kbSyntax\"/>" << endl;
                        w << "\t<MSHelp:Attr Name=\"DevLang\" Value=\"C++\"/>" << endl;
                        w << "\t<MSHelp:Attr Name=\"LinkGroup\" Value=\"" << m_LinkGroup << "\"/>" << endl;
                        w << "</xml>" << endl;
                        w << "</head>" << endl;
                        noCheck = true;
                    } else {
                        w << line << endl;
                    }
                }
            } else {
                fprintf(stderr, "Error: Cannot open file %s for writing.\n", qPrintable(destDir + file));
            }
        } else {
            fprintf(stderr, "Error: Cannot open file %s for reading.\n", qPrintable(srcDir + file));
        }
    }

    return true;
}

void VSHelpBuilder::writeProjectFile()
{
    fprintf(stdout, "Creating Project File (%s)...", qPrintable(projectFile));
	QFile file(projectFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
        s.setCodec(m_outCodec);

		s << "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" << endl;
		s << "<HelpWorkshopProject>" << endl;
		s << "\t<ProjectFile Name=\"" << HxCFile << "\"/>" << endl;
		s << "\t<IncludeFile Name=\"" << HxFFile << "\"/>" << endl;
		s << "\t<Files>" << endl;
		s << "\t\t<File Url=\"" << HxTFile << "\"/>" << endl;
		s << "\t\t<File Url=\"" << HxKKFile << "\"/>" << endl;
		s << "\t\t<File Url=\"" << HxKFFile << "\"/>" << endl;
		s << "\t\t<File Url=\"" << HxVFile << "\"/>" << endl;
		s << "\t\t<File Url=\"" << groupDefFile << "\"/>" << endl;
		// write html files???
		s << "\t</Files>" << endl;
		s << "\t<Dirs>" << endl;
		s << "\t<Dir Url=\"html\"/>" << endl;
		s << "\t\t</Dirs>" << endl;
		s << "\t<Options MSTOCMRUDIR=\"" << m_OutPath << "\\html" << "\" MSTOCMRUFT=\"0\"/>" << endl;
		s << "</HelpWorkshopProject>";
		fprintf(stdout, " Done.\n");
	} else {
		fprintf(stderr, "\nCreation of Project File failed. Cannot open file!\n");
	}
}

void VSHelpBuilder::writeHelpCollectionFile()
{
    fprintf(stdout, "Creating Help Collection File (%s)...", qPrintable(HxCFile));
	QFile file(HxCFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
        s.setCodec(m_outCodec);
		s << "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" << endl;
		s << "<!DOCTYPE HelpCollection SYSTEM \"ms-help://hx/resources/HelpCollection.DTD\">" << endl;
		s << "<HelpCollection DTDVersion=\"1.0\" LangId=\"1033\" Title=\"" << m_Title << "\" ";
		s << "FileVersion=\"" << m_FileVersion << "\" Copyright=\"Trolltech AS\">" << endl;
		s << "\t<CompilerOptions CreateFullTextIndex=\"Yes\" CompileResult=\"Hxs\">" << endl;
		s << "\t\t<IncludeFile File=\"" << HxFFile << "\"/>" << endl;
		s << "\t</CompilerOptions>" << endl;
		s << "\t<VTopicDef File=\"" << HxVFile << "\"/>" << endl;
		s << "\t<TOCDef File=\"" << HxTFile << "\"/>" << endl;
		s << "\t<KeywordIndexDef File=\"" << HxKKFile << "\"/>" << endl;
		s << "\t<KeywordIndexDef File=\"" << HxKFFile << "\"/>" << endl;
		s << "\t<ItemMoniker Name=\"!DefaultNamedUrlIndex\" ProgId=\"HxDs.HxIndex\" InitData=\"K\"/>" << endl;
		s << "\t<ItemMoniker Name=\"!DefaultToc\" ProgId=\"HxDs.HxHierarchy\" InitData=\"\"/>" << endl;
		s << "\t<ItemMoniker Name=\"!DefaultFullTextSearch\" ProgId=\"HxDs.HxFullTextSearch\" InitData=\"\"/>" << endl;
		s << "\t<ItemMoniker Name=\"!DefaultAssociativeIndex\" ProgId=\"HxDs.HxIndex\" InitData=\"A\"/>" << endl;
		s << "\t<ItemMoniker Name=\"!DefaultKeywordIndex\" ProgId=\"HxDs.HxIndex\" InitData=\"K\"/>" << endl;
		s << "\t<ItemMoniker Name=\"!SampleInfo\" ProgId=\"HxDs.HxSampleCollection\" InitData=\"\"/>" << endl;
		s << "\t<ItemMoniker Name=\"!DefaultContextWindowIndex\" ProgId=\"HxDs.HxIndex\" InitData=\"F\"/>" << endl;
		s << "\t<ToolData Name=\"MSVHWLogLevel\" Value=\"3\"/>" << endl;
		s << "\t<ToolData Name=\"MSVHWNamespace\" Value=\"" << m_Namespace << "\"/>" << endl;
		s << "\t<ToolData Name=\"MSVHWUniqueID\" Value=\"" << m_UniqueID << "\"/>" << endl;
		s << "</HelpCollection>";
		fprintf(stdout, " Done.\n");
	} else {
		fprintf(stderr, "\nCreation of Help Collection File failed. Cannot open file!\n");
	}
}

void VSHelpBuilder::writeHelpFileListFile()
{
    fprintf(stdout, "Creating Help File List File (%s)...", qPrintable(HxFFile));
	    
	QFile file(HxFFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
        s.setCodec(m_outCodec);
		s << "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n";
		s << "<!DOCTYPE HelpFileList SYSTEM \"ms-help://hx/resources/HelpFileList.DTD\">\n";
		s << "<HelpFileList DTDVersion=\"1.0\">\n";
		
        foreach (const QString &f, m_helpData.filterSections().first().files())
            s << "\t<File Url=\"html/" << f << "\"/>" << endl;
        
        s << "\t<File Url=\"" << groupDefFile << "\"/>" << endl;		
        s << "</HelpFileList>";
		fprintf(stdout, " Done.\n");
	} else {
		fprintf(stderr, "\nCreation of Help Collection File failed. Cannot open file!\n");
	}
}

void VSHelpBuilder::writeIndexFiles()
{
	QStringList indexFiles;
	indexFiles << HxKFFile << HxKKFile;
	QStringList indices;
	indices << "F" << "K";
	for (int i=0; i<2; ++i)
	{
        fprintf(stdout, "Creating Index File (%s)...", qPrintable(indexFiles.at(i)));
		QFile file(indexFiles.at(i));
		if (file.open(QIODevice::WriteOnly))
		{
			QTextStream s(&file);
            s.setCodec(m_outCodec);
			s << "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" << endl;
			s << "<!DOCTYPE HelpIndex SYSTEM \"ms-help://hx/resources/HelpIndex.DTD\">" << endl;
			s << "<HelpIndex DTDVersion=\"1.0\" Name=\"" << indices.at(i) << "\">" << endl;
			s << "</HelpIndex>";
			file.close();
			fprintf(stdout, " Done.\n");
		} else {
			fprintf(stderr, "\nCreation of Index File failed. Cannot open file!\n");
		}
	}
}

QString VSHelpBuilder::quote(const QString &str) const
{
    QString s = str;
    s.replace('&', QLatin1String("&amp;"));
    s.replace('>', QLatin1String("&gt;"));
    s.replace('<', QLatin1String("&lt;"));
    s.replace('\"', QLatin1String("&quot;"));
    return s;
}

// remove me after qdoc has been fixed!!!
QString VSHelpBuilder::fixRef(const QString &str) const
{
    QString s = str;
    s.replace('&', '-');
    s.replace('>', QLatin1String("-gt"));
    s.replace('<', QLatin1String("-lt"));
    return s;
}

QString VSHelpBuilder::getIndent(int depth)
{
	QString indent = "\t";
	for (int i=0; i<depth; ++i)
		indent.append("\t");
	return indent;
}

void VSHelpBuilder::writeContentItem(QTextStream *s, QHelpDataContentItem *item, int depth)
{
    *s << getIndent(depth) << "<HelpTOCNode Title=\"" << quote(item->title()) << "\" Url=\"html/" << item->reference() << "\">" << endl;
    foreach (QHelpDataContentItem *itm, item->children())
        writeContentItem(s, itm, depth + 1);
    *s << getIndent(depth) << "</HelpTOCNode>" << endl;
}

void VSHelpBuilder::writeTableOfContents()
{
    fprintf(stdout, "Creating TOC File (%s)...", qPrintable(HxTFile));
	QFile file(HxTFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
        s.setCodec(m_outCodec);
		s << "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" << endl;
		s << "<!DOCTYPE HelpTOC SYSTEM \"ms-help://hx/resources/HelpTOC.DTD\">" << endl;
		s << "<HelpTOC DTDVersion=\"1.0\">" << endl;
        s << "\t<HelpTOCNode Title=\"" << m_Title << "\" Url=\"html\\index.html\">" << endl;

		QList<QHelpDataContentItem*> contents = m_helpData.filterSections().first().contents();
        foreach (QHelpDataContentItem *item, contents) {
            foreach (QHelpDataContentItem *itm, item->children())
                writeContentItem(&s, itm, 1);            
        }
        
        s << "\t</HelpTOCNode>" << endl;
		s << "\t<ToolData Name=\"MSTOCEXPST\" Value=\"Expanded\"/>" << endl;
		s << "</HelpTOC>";
		file.close();
		fprintf(stdout, " Done.\n");
	} else {
		fprintf(stderr, "\nCreation of TOC File failed. Cannot open file!\n");
	}
}

void VSHelpBuilder::writeVirtualTopics()
{
    fprintf(stdout, "Creating Virtual Topics File (%s)...", qPrintable(HxVFile));

	QFile file(HxVFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
        s.setCodec(m_outCodec);
		s << "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" << endl;
		s << "<!DOCTYPE VTopicSet SYSTEM \"ms-help://hx/resources/HelpVTopic.DTD\">" << endl;
		s << "<VTopicSet DTDVersion=\"1.0\">" << endl;

        QString id;
        foreach (QHelpDataIndexItem item, m_helpData.filterSections().first().indices()) {
            id = item.identifier;
            if (id.isEmpty())
                id = item.name;
            id = quote(id);

            s << "\t<VTopic Url=\"html/" << fixRef(item.reference) << "\" RLTitle=\"" << id << "\">" << endl;
			s << "\t\t<Attr Name=\"Locale\" Value=\"kbEnglish\"/>" << endl;
			s << "\t\t<Attr Name=\"TopicType\" Value=\"kbSyntax\"/>" << endl;
			s << "\t\t<Attr Name=\"DevLang\" Value=\"C++\"/>" << endl;
			s << "\t\t<Attr Name=\"DocSet\" Value=\"" << m_DocSet << "\"/>" << endl;
			s << "\t\t<Attr Name=\"LinkGroup\" Value=\"" << m_LinkGroup << "\"/>" << endl;
			s << "\t\t<Keyword Index=\"K\" Term=\"" << quote(item.name) << "\"/>" << endl;
			s << "\t\t<Keyword Index=\"F\" Term=\"" << id << "\"/>" << endl;
			s << "\t</VTopic>" << endl;
        }
        
        s << "</VTopicSet>";
		fprintf(stdout, " Done.\n");
	} else {
		fprintf(stderr, "\nCreation of Virtual Topics File failed. Cannot open file!\n");
	}
}

void VSHelpBuilder::writeLinkGroup()
{
    fprintf(stdout, "Creating Group Definition File (%s)...", qPrintable(groupDefFile));
	QFile file(groupDefFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
        s.setCodec(m_outCodec);
		s << "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" << endl;
		s << "<DynamicHelp xmlns=\"http://microsoft.com/vstudio/tbd/vsdh.xsd\">" << endl;
		s << "\t<LINKGROUP ID=\"" << m_LinkGroup << "\" Title=\"" << m_LinkGroupTitle << "\" Priority=\"" << m_LinkGroupPriority << "\">" << endl;
		s << "\t\t<GLYPH Collapsed=\"1\" Expanded=\"2\"/>" << endl;
		s << "\t</LINKGROUP>" << endl;
		s << "\t<Context>" << endl;
		s << "\t\t<Keywords>" << endl;
		s << "\t\t\t<KItem Name=\"VS.TextEditor\"/>" << endl;
		s << "\t\t</Keywords>" << endl;
		s << "\t\t<Attributes>" << endl;
		s << "\t\t\t<AItem Name=\"DocSet\" Value=\"" << m_DocSet << "\"/>" << endl;
		s << "\t\t\t<AItem Name=\"DevLang\" Value=\"C++\"/>" << endl;
		s << "\t\t</Attributes>" << endl;
		s << "\t\t<Links>" << endl;
		s << "\t\t</Links>" << endl;
		s << "\t</Context>" << endl;
		s << "</DynamicHelp>";
		fprintf(stdout, " Done.\n");
	} else {
		fprintf(stderr, "\nCreation of Group Definition File failed. Cannot open file!\n");
	}
}

void VSHelpBuilder::writeCollectionLevelFiles()
{
	fprintf(stdout, "Creating Collection Level Files...");

	QFile file(m_OutPath + "\\" + HxCColFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
        s.setCodec(m_outCodec);
		s << "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n";
		s << "<!DOCTYPE HelpCollection>" << endl;
		s << "<HelpCollection DTDVersion=\"1.0\" FileVersion=\"" << m_FileVersion << "\" LangId=\"1033\" "
			<< "Title=\"" << m_Title << "\" Copyright=\"Trolltech AS\">" << endl;
		s << "\t<AttributeDef File=\"" << HxAColFile << "\"/>" << endl;
		s << "\t<TOCDef File=\"" << HxTColFile << "\"/>" << endl;
		s << "\t<KeywordIndexDef File=\"" << HxKKColFile << "\"/>" << endl;
		s << "\t<KeywordIndexDef File=\"" << HxKFColFile << "\"/>" << endl;   
		s << "\t<ItemMoniker Name=\"!DefaultToc\" ProgId=\"HxDs.HxHierarchy\" InitData=\"\"/>" << endl;
		s << "\t<ItemMoniker Name=\"!DefaultFullTextSearch\" ProgId=\"HxDs.HxFullTextSearch\" InitData=\"\"/>" << endl;
		s << "\t<ItemMoniker Name=\"!DefaultKeywordIndex\" ProgId=\"HxDs.HxIndex\" InitData=\"K\"/>" << endl;
		s << "\t<ItemMoniker Name=\"!DefaultContextWindowIndex\" ProgId=\"HxDs.HxIndex\" InitData=\"F\"/>" << endl;
		s << "</HelpCollection>";
	}
	else {
		fprintf(stderr, "\nCreation of Collection File failed. Cannot open file!\n");
	}

	file.close();
	file.setFileName(m_OutPath + "\\" + HxTColFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
        s.setCodec(m_outCodec);
		s << "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" << endl; 
		s << "<!DOCTYPE HelpTOC>" << endl;
		s << "<HelpTOC DTDVersion=\"1.0\" PluginStyle=\"Flat\">" << endl;
		s << "\t<HelpTOCNode NodeType=\"TOC\" Url=\"" << m_UniqueID << "\"/>" << endl;        
        s << "</HelpTOC>";		
	}
	else {
		fprintf(stderr, "\nCreation of Table of Contents File failed. Cannot open file!\n");
	}

	file.close();
	file.setFileName(m_OutPath + "\\" + HxKKColFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
        s.setCodec(m_outCodec);
		s << "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" << endl; 
		s << "<!DOCTYPE HelpIndex>" << endl;
		s << "<HelpIndex DTDVersion=\"1.0\" Name=\"K\" Visible=\"No\" LangId=\"1033\">" << endl;
		s << "</HelpIndex>";		
	}
	else {
		fprintf(stderr, "\nCreation of Index File failed. Cannot open file!\n");
	}

	file.close();
	file.setFileName(m_OutPath + "\\" + HxKFColFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
        s.setCodec(m_outCodec);
		s << "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" << endl; 
		s << "<!DOCTYPE HelpIndex>" << endl;
		s << "<HelpIndex DTDVersion=\"1.0\" Name=\"F\" Visible=\"No\" LangId=\"1033\">" << endl;
		s << "</HelpIndex>";		
	}
	else {
		fprintf(stderr, "\nCreation of Index File failed. Cannot open file!\n");
	}

	file.close();
	file.setFileName(m_OutPath + "\\" + HxAColFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
        s.setCodec(m_outCodec);
		s << "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" << endl; 
		s << "<!DOCTYPE HelpAttributes>" << endl;
		s << "<HelpAttributes DTDVersion=\"1.0\">" << endl;
		s << "\t<AttName Id=\"1\" Name=\"DocSet\" Display=\"Yes\" UIString=\"DocSet\" AttType=\"Enum\">" << endl;
		s << "\t\t<AttVal Id=\"1_1\" Name=\"" << m_DocSet << "\" Display=\"Yes\" UIString=\"" << m_DocSetTitle << "\" />" << endl;
		s << "\t</AttName>" << endl;
		s << "\t<AttName Id=\"2\" Name=\"LinkGroup\" Display=\"No\" UIString=\"LinkGroup\" AttType=\"Enum\">" << endl;
		s << "\t\t<AttVal Id=\"2_1\" Name=\"" << m_LinkGroup << "\" Display=\"No\" UIString=\"" << m_LinkGroupTitle << "\" />" << endl;
		s << "\t</AttName>" << endl;
		s << "\t<AttName Id=\"3\" Name=\"DevLang\" Display=\"Yes\" UIString=\"DevLang\" AttType=\"Enum\">" << endl;
		s << "\t\t<AttVal Id=\"3_1\" Name=\"C++\" Display=\"Yes\" UIString=\"C++\"/>" << endl;
		s << "\t</AttName>" << endl;
		s << "\t<AttName Id=\"4\" Name=\"Locale\" Display=\"No\" UIString=\"Locale\" AttType=\"Enum\">" << endl;
		s << "\t\t<AttVal Id=\"4_1\" Name=\"kbEnglish\" Display=\"No\" UIString=\"English\"/>" << endl;
		s << "\t</AttName>" << endl;
		s << "\t<AttName Id=\"5\" Name=\"TopicType\" Display=\"No\" UIString=\"TopicType\" AttType=\"Enum\">" << endl;
		s << "\t\t<AttVal Id=\"5_1\" Name=\"kbSyntax\" Display=\"No\" UIString=\"Syntax\"/>" << endl;
		s << "\t\t<AttVal Id=\"5_2\" Name=\"kbArticle\" Display=\"No\" UIString=\"Article\"/>" << endl;
		s << "\t\t<AttVal Id=\"5_3\" Name=\"kbHowTo\" Display=\"No\" UIString=\"How To\"/>" << endl;
		s << "\t</AttName>" << endl;	
		s << "</HelpAttributes>";		
	}
	else {
		fprintf(stderr, "\nCreation of Attribute File failed. Cannot open file!\n");
	}
	file.close();
	fprintf(stdout, " Done.\n");
}

void VSHelpBuilder::writeH2RegFile()
{
    fprintf(stdout, "Creating H2Reg File (%s)...", qPrintable(H2RegFile));
	QFile file(H2RegFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
		writeH2RegCommonDefinitions(s);
		s << ";------- Register -r switch" << endl;
		s << "[Reg_Namespace]" << endl;
		s << ";<nsName>|<nsColfile>|<nsDesc>" << endl;
        if (m_Kind == VSHelpBuilder::VS)
		    s << m_Namespace << "|" << HxCColFile << "|Qt Integration for VS - qt.nokia.com" << endl;
        else
            s << m_Namespace << "|" << HxCColFile << "|Qt Reference Documentaton - qt.nokia.com" << endl;

		s << "[Reg_Title]" << endl;
		s << ";<nsName>|<TitleID>|<LangId>|<HxS_HelpFile>" << endl;
		s << m_Namespace << "|" << m_UniqueID << "|1033|"
			<< HxSFile.right(HxSFile.length() - HxSFile.lastIndexOf("\\") - 1) << endl;

		s << "[Reg_Plugin]" << endl;
		s << ";<nsName_Parent>|<HxT_Parent>|<nsName_Child>|<HxT_Child>|<HxA_Child>" << endl;
		s << "MS.VSCC+|_DEFAULT|" << m_Namespace << "|_DEFAULT|" << HxAColFile << endl;

		s << "[Reg_Filter]" << endl;
		s << ";<nsName>|<FilterName>|<FilterQueryStr>" << endl;
		s << m_Namespace << "|" << m_FilterName << "|(\"docset\"=\"" << m_DocSet << "\")" << endl << endl;

		s << ";------- UnRegister -u switch" << endl;
		s << "[UnReg_Namespace]" << endl;
		s << ";<nsName>" << endl;
		s << m_Namespace << endl;

		s << "[UnReg_Title]" << endl;
		s << ";<nsName>|<TitleID>|<LangId>" << endl;
		s << m_Namespace << "|" << m_UniqueID << "|1033" << endl;

		s << "[UnReg_Plugin]" << endl;
		s << ";<nsName_Parent>|<HxT_Parent>|<nsName_Child>|<HxT_Child>|<HxA_Child>" << endl;
		s << "MS.VSCC+|_DEFAULT|" << m_Namespace << "|_DEFAULT|" << HxAColFile << endl;

		s << "[UnReg_Filter]" << endl;
		s << ";<nsName>|<FilterName>" << endl;
		s << m_Namespace << "|" << m_FilterName;
		fprintf(stdout, " Done.\n");
	} else {
		fprintf(stderr, "\nCreation of H2Reg File failed. Cannot open file!\n");
	}
}

void VSHelpBuilder::writeH2RegCommonDefinitions(QTextStream &s)
{
	s << "[MAIN]" << endl;
	s << "DebugMode=0" << endl;
	s << "DumpNSToLog_before=0" << endl;
	s << "DumpNSToLog_after=0" << endl;
	s << "OKtoReport_FinalRegError=0" << endl;
	s << "OKtoReport_FinalUnRegError=0" << endl;
	s << endl;
	s << "UserDir1=\'\'" << endl;
	s << "UserDir2=\'\'" << endl;
	s << "UserDir3=\'\'" << endl;
	s << "UserDir4=\'\'" << endl;
	s << endl;
	s << "[en] ; English" << endl;
	s << "ErrSt_SysFileNotFound = \'Installation Error. Error reading system file or file not found.|%s\'" << endl;
	s << "ErrSt_MSHelp2RTNotFound = \'MS Help 2.x runtime files are not installed on this PC.\'" << endl;
	s << "ErrSt_NotAdminMode = \'You must be logged on as an Administrator.\'" << endl;
	s << "ErrSt_Extra = \'Installation/registration of Help files cannot proceed.\'" << endl;
	s << endl;
	s << "Msg_Registering = \'Registering Online Documentation Files:\'" << endl;
	s << "Msg_UnRegistering = \'Unregistering Online Documentation Files:\'" << endl;
	s << "Msg_LoggingNSInfo = \'Logging Namespace Info\'" << endl;
	s << "Msg_Registering_Namespaces =  \'Registering Namespaces\'" << endl;
	s << "Msg_Registering_Titles =  \'Registering Titles\'" << endl;
	s << "Msg_Registering_Plugins =  \'Registering Plug-ins\'" << endl;
	s << "Msg_Registering_Filters =  \'Registering Filters\'" << endl;
	s << "Msg_UnRegistering_Namespaces =  \'Unregistering Namespaces\'" << endl;
	s << "Msg_UnRegistering_Titles =  \'Unregistering Titles\'" << endl;
	s << "Msg_UnRegistering_Plugins =  \'Unregistering Plug-ins\'" << endl;
	s << "Msg_UnRegistering_Filters =  \'Unregistering Filters\'" << endl;
	s << endl;
	s << "Msg_Merging_Namespaces = \'Merging Help Indexes. This may take several minutes\'" << endl;
	s << endl;
	s << "PopupSt_FinalRegError=\'There were errors reported while Registering help files.||View Log file?\'" << endl;
	s << "PopupSt_FinalUnRegError=\'There were errors reported while Unregistering help files.||View Log file?\'" << endl;
	s << endl;
	s << "[de] ; German" << endl;
	s << "[ja] ; Japanese" << endl;
	s << "[fr] ; French" << endl;
	s << "[es] ; Spanish" << endl;
	s << "[it] ; Italian" << endl;
	s << "[ko] ; Korean" << endl;
	s << "[cn] ; Chinese (Simplified)" << endl;
	s << "[tw] ; Chinese (Traditional)" << endl;
	s << "[sv] ; Swedish" << endl;
	s << "[nl] ; Dutch" << endl;
	s << "[ru] ; Russian" << endl;
	s << "[ar] ; Arabic" << endl;
	s << "[he] ; Hebrew" << endl;
	s << "[da] ; Danish" << endl;
	s << "[no] ; Norwegian" << endl;
	s << "[fi] ; Finnish" << endl;
	s << "[pt] ; Portuguese" << endl;
	s << "[br] ; Brazilian" << endl;
	s << "[cs] ; Czech" << endl;
	s << "[pl] ; Polish" << endl;
	s << "[hu] ; Hungarian" << endl;
	s << "[el] ; Greek" << endl;
	s << "[tr] ; Turkish" << endl;
	s << "[sl] ; Slovenian" << endl;
	s << "[sk] ; Slovakian" << endl;
	s << "[eu] ; Basque" << endl;
	s << "[ca] ; Catalan" << endl;
	s << endl;
}

void VSHelpBuilder::writeExternalH2RegFile()
{
    fprintf(stdout, "Creating H2Reg File (%s)...", qPrintable(ExtH2RegFile));
	QFile file(ExtH2RegFile);
	if (file.open(QIODevice::WriteOnly))
	{
		QTextStream s(&file);
		s << ";------- Register -r switch" << endl;
		s << "[Reg_Namespace]" << endl;
		s << ";<nsName>|<nsColfile>|<nsDesc>" << endl;
        if (m_Kind == VSHelpBuilder::VS)
		    s << m_Namespace << "|" << HxCColFile << "|Qt Integration for VS - qt.nokia.com" << endl;
        else
            s << m_Namespace << "|" << HxCColFile << "|Qt Reference Documentaton - qt.nokia.com" << endl;

		s << "[Reg_Title]" << endl;
		s << ";<nsName>|<TitleID>|<LangId>|<HxS_HelpFile>" << endl;
		s << m_Namespace << "|" << m_UniqueID << "|1033|"
			<< HxSFile.right(HxSFile.length() - HxSFile.lastIndexOf("\\") - 1) << endl;

		s << "[Reg_Plugin]" << endl;
		s << ";<nsName_Parent>|<HxT_Parent>|<nsName_Child>|<HxT_Child>|<HxA_Child>" << endl;
		s << "MS.VSCC+|_DEFAULT|" << m_Namespace << "|_DEFAULT|" << HxAColFile << endl;

		s << "[Reg_Filter]" << endl;
		s << ";<nsName>|<FilterName>|<FilterQueryStr>" << endl;
		s << m_Namespace << "|" << m_FilterName << "|(\"docset\"=\"" << m_DocSet << "\")" << endl << endl;

		s << ";------- UnRegister -u switch" << endl;
		s << "[UnReg_Namespace]" << endl;
		s << ";<nsName>" << endl;
		s << m_Namespace << endl;

		s << "[UnReg_Title]" << endl;
		s << ";<nsName>|<TitleID>|<LangId>" << endl;
		s << m_Namespace << "|" << m_UniqueID << "|1033" << endl;

		s << "[UnReg_Plugin]" << endl;
		s << ";<nsName_Parent>|<HxT_Parent>|<nsName_Child>|<HxT_Child>|<HxA_Child>" << endl;
		s << "MS.VSCC+|_DEFAULT|" << m_Namespace << "|_DEFAULT|" << HxAColFile << endl;

		s << "[UnReg_Filter]" << endl;
		s << ";<nsName>|<FilterName>" << endl;
		s << m_Namespace << "|" << m_FilterName;
		fprintf(stdout, " Done.\n");
	} else {
		fprintf(stderr, "\nCreation of H2Reg File failed. Cannot open file!\n");
	}
}

void VSHelpBuilder::compile()
{
    QStringList args;
	args << "-p" << HxCFile
		 << "-o" << HxSFile
         << "-l" << logFile;
    fprintf(stdout, "%s\\hxcomp.exe -p %s -o %s\n",qPrintable(m_HelpSDKDir), qPrintable(HxCFile), qPrintable(HxSFile));
	process->start(m_HelpSDKDir + "\\hxcomp.exe", args);
}
