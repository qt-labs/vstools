// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace QtVsTools.Qrc
{
    /// <summary>
    /// A &lt;qresource&gt; element: a resource prefix, an optional language and its files.
    /// </summary>
    public sealed class QrcPrefix : Observable
    {
        private readonly ObservableCollection<QrcFile> files = new();

        internal QrcPrefix(string name, string language = null,
            IEnumerable<XAttribute> otherAttributes = null)
        {
            this.name = QrcPath.FixPrefix(name);
            this.language = language ?? string.Empty;
            InitialName = this.name;
            InitialLanguage = this.language;
            Files = new ReadOnlyObservableCollection<QrcFile>(files);
            OtherAttributes = otherAttributes?.Select(x => new XAttribute(x)).ToList()
                ?? new List<XAttribute>();
        }

        /// <summary>
        /// Name and language the prefix was loaded or created with. A .qrc file may contain
        /// several prefixes with the same name and language, so returning to them is no clash.
        /// </summary>
        internal string InitialName { get; }
        internal string InitialLanguage { get; }

        internal bool IsInitial(string name, string language)
            => name == InitialName && language == InitialLanguage;

        /// <summary>
        /// The document this prefix belongs to; null while the prefix is not part of a document.
        /// </summary>
        public QrcDocument Document
        {
            get => document;
            internal set
            {
                if (!SetField(ref document, value))
                    return;
                foreach (var file in files)
                    file.NotifyLocationChanged();
            }
        }
        private QrcDocument document;

        private string name;
        /// <summary>
        /// The normalized prefix, e.g. "/" or "/images".
        /// </summary>
        public string Name
        {
            get => name;
            internal set
            {
                if (!SetField(ref name, QrcPath.FixPrefix(value)))
                    return;
                OnPropertyChanged(nameof(DisplayName));
                foreach (var file in files)
                    file.NotifyPrefixChanged();
            }
        }

        private string language;
        /// <summary>
        /// The language the resources are selected for; empty if language independent.
        /// </summary>
        public string Language
        {
            get => language;
            internal set
            {
                if (SetField(ref language, value ?? string.Empty))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary>
        /// Text shown in the editor tree: the prefix, followed by the language, if any.
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(Language)
            ? Name : $"{Name} ({Language})";

        public ReadOnlyObservableCollection<QrcFile> Files { get; }

        /// <summary>
        /// Attributes other than 'prefix' and 'lang', kept when saving.
        /// </summary>
        internal List<XAttribute> OtherAttributes { get; }

        internal void InsertFile(int index, QrcFile file)
        {
            file.Prefix = this;
            file.NotifyLocationChanged();
            files.Insert(index, file);
        }

        internal void RemoveFile(QrcFile file)
        {
            files.Remove(file);
            file.Prefix = null;
        }

        /// <summary>
        /// Returns true if the prefix contains a file with the given absolute path.
        /// </summary>
        public bool Contains(string path)
        {
            return files.Any(x => QrcPath.PathComparer.Equals(x.Path, path));
        }
    }
}
