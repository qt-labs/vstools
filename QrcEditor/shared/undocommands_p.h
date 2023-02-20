/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#ifndef UNDO_COMMANDS_H
#define UNDO_COMMANDS_H

#include "resourceview.h"

#include <QtCore/QString>
#include <QtWidgets/QUndoCommand>

QT_BEGIN_NAMESPACE
class QModelIndex;
QT_END_NAMESPACE

namespace SharedTools {

/*!
    \class ViewCommand

    Provides a base for \l ResourceView-related commands.
*/
class ViewCommand : public QUndoCommand
{
protected:
    ResourceView *m_view;

    ViewCommand(ResourceView *view);
    virtual ~ViewCommand();
};

/*!
    \class ModelIndexViewCommand

    Provides a mean to store/restore a \l QModelIndex as it cannot
    be stored safely in most cases. This is an abstract class.
*/
class ModelIndexViewCommand : public ViewCommand
{
    int m_prefixArrayIndex;
    int m_fileArrayIndex;

protected:
    ModelIndexViewCommand(ResourceView *view);
    virtual ~ModelIndexViewCommand();
    void storeIndex(const QModelIndex &index);
    QModelIndex makeIndex() const;
};

/*!
    \class ModifyPropertyCommand

    Modifies the name/prefix/language property of a prefix/file node.
*/
class ModifyPropertyCommand : public ModelIndexViewCommand
{
    ResourceView::NodeProperty m_property;
    QString m_before;
    QString m_after;
    int m_mergeId;

public:
    ModifyPropertyCommand(ResourceView *view, const QModelIndex &nodeIndex,
            ResourceView::NodeProperty property, const int mergeId, const QString &before,
            const QString &after = QString());

private:
    int id() const { return m_mergeId; }
    bool mergeWith(const QUndoCommand * command);
    void undo();
    void redo();
};

/*!
    \class RemoveEntryCommand

    Removes a \l QModelIndex including all children from a \l ResourceView.
*/
class RemoveEntryCommand : public ModelIndexViewCommand
{
    EntryBackup *m_entry;
    bool m_isExpanded;

public:
    RemoveEntryCommand(ResourceView *view, const QModelIndex &index);
    ~RemoveEntryCommand();

private:
    void redo();
    void undo();
    void freeEntry();
};

/*!
    \class AddFilesCommand

    Adds a list of files to a given prefix node.
*/
class AddFilesCommand : public ViewCommand
{
    int m_prefixIndex;
    int m_cursorFileIndex;
    int m_firstFile;
    int m_lastFile;
    const QStringList m_fileNames;

public:
    AddFilesCommand(ResourceView *view, int prefixIndex, int cursorFileIndex,
            const QStringList &fileNames);

private:
    void redo();
    void undo();
};

/*!
    \class AddEmptyPrefixCommand

    Adds a new, empty prefix node.
*/
class AddEmptyPrefixCommand : public ViewCommand
{
    int m_prefixArrayIndex;

public:
    AddEmptyPrefixCommand(ResourceView *view);

private:
    void redo();
    void undo();
};

} // namespace SharedTools

#endif // UNDO_COMMANDS_H
