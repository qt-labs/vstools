/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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
