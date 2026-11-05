// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.Linq;

namespace QtVsTools.Qrc
{
    // Undo commands for edits of a QrcDocument. Commands keep references to the affected
    // objects, so that undo and redo restore the very same instances (and thereby the state
    // of any view bound to them).

    internal abstract class QrcCommand : IUndoCommand
    {
        protected QrcCommand(string text)
        {
            Text = text;
        }

        public string Text { get; }
        public abstract void Redo();
        public abstract void Undo();
        public virtual bool TryMerge(IUndoCommand next) => false;
        public virtual bool IsObsolete => false;
    }

    internal sealed class InsertPrefixCommand : QrcCommand
    {
        private readonly QrcDocument document;
        private readonly QrcPrefix prefix;
        private readonly int index;

        public InsertPrefixCommand(QrcDocument document, QrcPrefix prefix, int index)
            : base("Add Prefix")
        {
            this.document = document;
            this.prefix = prefix;
            this.index = index;
        }

        public override void Redo() => document.InsertPrefixCore(index, prefix);
        public override void Undo() => document.RemovePrefixCore(prefix);
    }

    internal sealed class RemovePrefixCommand : QrcCommand
    {
        private readonly QrcDocument document;
        private readonly QrcPrefix prefix;
        private int index;

        public RemovePrefixCommand(QrcDocument document, QrcPrefix prefix)
            : base("Remove Prefix")
        {
            this.document = document;
            this.prefix = prefix;
        }

        public override void Redo()
        {
            index = document.Prefixes.IndexOf(prefix);
            document.RemovePrefixCore(prefix);
        }

        public override void Undo() => document.InsertPrefixCore(index, prefix);
    }

    internal sealed class InsertFilesCommand : QrcCommand
    {
        private readonly QrcPrefix prefix;
        private readonly List<QrcFile> files;
        private readonly int index;

        public InsertFilesCommand(QrcPrefix prefix, IEnumerable<QrcFile> files, int index)
            : base("Add Files")
        {
            this.prefix = prefix;
            this.files = files.ToList();
            this.index = index;
        }

        public override void Redo()
        {
            for (var i = 0; i < files.Count; i++)
                prefix.InsertFile(index + i, files[i]);
        }

        public override void Undo()
        {
            foreach (var file in files)
                prefix.RemoveFile(file);
        }
    }

    internal sealed class RemoveFileCommand : QrcCommand
    {
        private readonly QrcPrefix prefix;
        private readonly QrcFile file;
        private int index;

        public RemoveFileCommand(QrcFile file)
            : base("Remove File")
        {
            prefix = file.Prefix;
            this.file = file;
        }

        public override void Redo()
        {
            index = prefix.Files.IndexOf(file);
            prefix.RemoveFile(file);
        }

        public override void Undo() => prefix.InsertFile(index, file);
    }

    /// <summary>
    /// Changes a text property (prefix, language or alias). Consecutive changes of the same
    /// property of the same object with the same merge id are combined into one undo step.
    /// </summary>
    internal sealed class SetPropertyCommand : QrcCommand
    {
        private readonly object target;
        private readonly string property;
        private readonly int mergeId;
        private readonly Action<string> setter;
        private readonly string before;
        private string after;

        public SetPropertyCommand(string text, object target, string property, string before,
            string after, int mergeId, Action<string> setter)
            : base(text)
        {
            this.target = target;
            this.property = property;
            this.before = before;
            this.after = after;
            this.mergeId = mergeId;
            this.setter = setter;
        }

        public override void Redo() => setter(after);
        public override void Undo() => setter(before);

        public override bool TryMerge(IUndoCommand next)
        {
            if (mergeId < 0 || next is not SetPropertyCommand other)
                return false;
            if (other.mergeId != mergeId || !ReferenceEquals(other.target, target)
                || other.property != property) {
                return false;
            }
            after = other.after;
            return true;
        }

        public override bool IsObsolete => before == after;
    }
}
