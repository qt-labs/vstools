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

#ifndef QRCEDITOR_H
#define QRCEDITOR_H

#include "ui_qrceditor.h"
#include "resourceview.h"

#include <QtWidgets/QWidget>
#include <QtWidgets/QUndoStack>

namespace SharedTools {

class QrcEditor : public QWidget
{
    Q_OBJECT

public:
    QrcEditor(QWidget *parent = 0);
    virtual ~QrcEditor();

    bool load(const QString &fileName);
    bool save();

    bool isDirty();
    void setDirty(bool dirty);

    QString fileName() const;
    void setFileName(const QString &fileName);

    void setResourceDragEnabled(bool e);
    bool resourceDragEnabled() const;

    void setDefaultAddFileEnabled(bool enable);
    bool defaultAddFileEnabled() const;

    void addFile(const QString &prefix, const QString &file);
//    void removeFile(const QString &prefix, const QString &file);

signals:
    void dirtyChanged(bool dirty);
    void addFilesTriggered(const QString &prefix);

private slots:
    void updateCurrent();
    void updateHistoryControls();

private:
    void resolveLocationIssues(QStringList &files);

private slots:
    void onAliasChanged(const QString &alias);
    void onPrefixChanged(const QString &prefix);
    void onLanguageChanged(const QString &language);
    void onRemove();
    void onAddFiles();
    void onAddPrefix();

signals:
    void undoStackChanged(bool canUndo, bool canRedo);

public slots:
    void onUndo();
    void onRedo();

private:
    Ui::QrcEditor m_ui;
    QUndoStack m_history;
    ResourceView *m_treeview;
    QAction *m_addFileAction;

    QString m_currentAlias;
    QString m_currentPrefix;
    QString m_currentLanguage;
};

}

#endif
