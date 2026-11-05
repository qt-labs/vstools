// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;

namespace QtVsTools.Qrc
{
    /// <summary>
    /// An operation that can be undone and redone.
    /// </summary>
    public interface IUndoCommand
    {
        /// <summary>
        /// Short description of the operation, e.g. for menu entries.
        /// </summary>
        string Text { get; }

        void Redo();
        void Undo();

        /// <summary>
        /// Tries to absorb <paramref name="next"/>, which has already been executed, into this
        /// command. Used to combine consecutive edits of the same text into one undo step.
        /// </summary>
        bool TryMerge(IUndoCommand next);

        /// <summary>
        /// True if the command has no effect anymore, e.g. after merging an edit that restored
        /// the original value. Obsolete commands are removed from the stack.
        /// </summary>
        bool IsObsolete { get; }
    }

    /// <summary>
    /// Stack of executed commands, modeled after QUndoStack. Tracks a clean state, i.e. the
    /// position of the stack that corresponds to the saved document.
    /// </summary>
    public sealed class UndoStack : Observable
    {
        private readonly List<IUndoCommand> commands = new();
        private int index;
        private int cleanIndex;

        public bool CanUndo => index > 0;
        public bool CanRedo => index < commands.Count;
        public bool IsClean => index == cleanIndex;

        public string UndoText => CanUndo ? commands[index - 1].Text : string.Empty;
        public string RedoText => CanRedo ? commands[index].Text : string.Empty;

        /// <summary>
        /// Raised after any change of the stack state.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Executes <paramref name="command"/> and puts it on the stack. Commands that can be
        /// undone after the current position are discarded.
        /// </summary>
        public void Push(IUndoCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Redo();

            if (index < commands.Count) {
                if (cleanIndex > index)
                    cleanIndex = -1; // The clean state can no longer be reached.
                commands.RemoveRange(index, commands.Count - index);
            }

            // Never merge into the command that represents the clean state.
            if (index > 0 && index != cleanIndex && commands[index - 1].TryMerge(command)) {
                if (commands[index - 1].IsObsolete) {
                    commands.RemoveAt(index - 1);
                    index--;
                }
            } else if (!command.IsObsolete) {
                commands.Add(command);
                index++;
            }

            NotifyChanged();
        }

        public void Undo()
        {
            if (!CanUndo)
                return;
            commands[--index].Undo();
            NotifyChanged();
        }

        public void Redo()
        {
            if (!CanRedo)
                return;
            commands[index++].Redo();
            NotifyChanged();
        }

        /// <summary>
        /// Marks the current position as clean, e.g. after saving the document.
        /// </summary>
        public void SetClean()
        {
            cleanIndex = index;
            NotifyChanged();
        }

        /// <summary>
        /// Removes all commands and marks the stack as clean.
        /// </summary>
        public void Clear()
        {
            commands.Clear();
            index = 0;
            cleanIndex = 0;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(IsClean));
            OnPropertyChanged(nameof(UndoText));
            OnPropertyChanged(nameof(RedoText));
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
