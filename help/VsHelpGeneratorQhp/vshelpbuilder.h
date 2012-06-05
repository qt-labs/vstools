/****************************************************************************
**
** Copyright (C) 2012 Digia Plc and/or its subsidiary(-ies).
** Contact: http://www.qt-project.org/legal
**
** This file is part of the Qt VS Add-in.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia. For licensing terms and
** conditions see http://qt.digia.com/licensing. For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file. Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights. These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file. Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

#ifndef VSHELPBUILDER_H
#define VSHELPBUILDER_H

#include <QtCore/QObject>
#include <QtCore/QProcess>

#include "qhelpprojectdata_p.h"

class QProcess;
class QTextStream;
class QTextCodec;

class VSHelpBuilder : public QObject
{
	Q_OBJECT
public:
	enum BuildMode  { CopyModifyFiles		= 0x1,
					  CreateCollectionFiles = 0x2,
					  Compile				= 0x4,
					  All					= 0x7 };
    enum Kind { Qt, VS };
    VSHelpBuilder(VSHelpBuilder::Kind kind, const QString &qtDir,
        const QString &version, const QString &title);
	bool init();
	void setOutPath(const QString &path);
	bool createDocumentation(BuildMode mode = VSHelpBuilder::All);

signals:
	void finished();

private:
	bool copyAndModifyFiles();
	void compile();
	void writeProjectFile();
	void writeHelpCollectionFile();
	void writeHelpFileListFile();	
	void writeIndexFiles();
	void writeTableOfContents();
	void writeVirtualTopics();
	void writeLinkGroup();
	void writeCollectionLevelFiles();
	void writeH2RegFile();
	void writeH2RegCommonDefinitions(QTextStream &s);
    void writeExternalH2RegFile();
	QString getIndent(int depth);
    void writeContentItem(QTextStream *s, QHelpDataContentItem *item, int depth);
    QString quote(const QString &str) const;
    QString fixRef(const QString &str) const;

private slots:
	void compilationStarted();
	void compilationFinished(int);
	void compilationProcessError(QProcess::ProcessError error);
    
private:
    Kind m_Kind;
	QString m_Version;
	QString m_SrcDir;
	QString m_OutPath;
	QString m_FileRoot;
	QString m_Title;
	QString m_DocSet;
	QString m_DocSetTitle;
	QString m_FilterName;
	QString m_FileVersion;
	QString m_Namespace;
	QString m_UniqueID;
	QString m_LinkGroup;
	QString m_LinkGroupTitle;
	QString m_LinkGroupPriority;
	QString m_DynamicHelpDefaultLink;
    QString m_HelpSDKDir;
	QString projectFile, HxSFile, HxCFile, HxFFile, HxTFile, HxKKFile, HxKFFile, HxVFile, groupDefFile;
	QString HxCColFile, HxTColFile,	HxKKColFile, HxKFColFile, HxAColFile;
	QString H2RegFile;
    QString ExtH2RegFile;
    QString logFile;
	QProcess *process;
    QHelpProjectData m_helpData;
    QTextCodec* m_outCodec;
};

#endif
