/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Add-in.
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

#include <QCoreApplication>
#include <QDir>

#include "vshelpbuilder.h"

int main(int argc, char *argv[])
{
	QCoreApplication app(argc, argv);

    QString outDir;
    QString inDir;
    QString version;
    QString title;
    VSHelpBuilder::Kind kind = VSHelpBuilder::Qt;

    for (int i=1; i<argc; ++i)
    {
        QString arg = QString::fromLocal8Bit(argv[i]);
        if (arg.startsWith("/out:")) {
            outDir = QDir::cleanPath(arg.right(arg.length() - 5));
            outDir.replace("/", "\\");
        } else if (arg.startsWith("/in:")) {
            inDir = arg.right(arg.length() - 4);
            QDir d(inDir);
            if (!d.exists()) {
                fprintf(stderr, "The specified input directory does not exist!\n");
                return -1;
            }
            inDir = QDir::cleanPath(d.absolutePath());
            outDir.replace("/", "\\");
        } else if(arg.startsWith("/version:")) {
            version = arg.right(arg.length() - 9);
        } else if (arg.startsWith("/title:")) {
            title = arg.right(arg.length() - 7);
        } else if (arg.startsWith("/type:")) {
            if (arg.right(arg.length() - 6).toLower() == "vs")
                kind = VSHelpBuilder::VS;
        } else if (arg == "/?") {
            fprintf(stdout, "Usage: VSHelpGenerator OPTIONS\n\n"
                "/in:         Input directory. Must match a Qt root directory.\n"
                "/out:        Output directory.\n"
                "/title:      Title of the documentation.\n"
                "/version:    Version of the documentation.\n"
                "/type:       Documentation type, either \'Qt\' or \'VS\'. If no\n"
                "             or an unrecognized type is specified, \'Qt\' is\n"
                "             chosen.\n"
                "/?           Displays the help.\n\n"
                "Note: All options except help and type are mandatory!");
                return 0;
        } else {
            fprintf(stderr, "Unknown option %s!\n\nEnter /? for help.\n",
                arg.toLocal8Bit().constData());
            return -1;
        }
    }

    if (inDir.isEmpty() || outDir.isEmpty() || title.isEmpty() || version.isEmpty()) {
        fprintf(stderr, "Missing arguments! Enter /? for help.\n");
        return -1;
    }

    VSHelpBuilder hfw(kind, inDir, version, title);
	QObject::connect(&hfw, SIGNAL(finished()), &app, SLOT(quit()));
    if (!hfw.init()) {
        fprintf(stderr, "Cannot initialize VSHelpBuilder!\n");
        return -1;
    }
	hfw.setOutPath(outDir);
    hfw.createDocumentation(VSHelpBuilder::BuildMode(VSHelpBuilder::All));
	return app.exec();
}
