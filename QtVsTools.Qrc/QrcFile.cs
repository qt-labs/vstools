// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace QtVsTools.Qrc
{
    /// <summary>
    /// A &lt;file&gt; entry of a resource prefix.
    /// </summary>
    public sealed class QrcFile : Observable
    {
        /// <param name="path">Absolute path of the file.</param>
        /// <param name="alias">Alias, if any.</param>
        /// <param name="otherAttributes">Attributes other than 'alias', kept when saving.</param>
        /// <param name="writtenPath">Text of the &lt;file&gt; element of a loaded file.</param>
        /// <param name="writtenDirectory">Directory of the .qrc file it was loaded from.</param>
        internal QrcFile(string path, string alias = null,
            IEnumerable<XAttribute> otherAttributes = null,
            string writtenPath = null, string writtenDirectory = null)
        {
            Path = path;
            this.alias = alias ?? string.Empty;
            OtherAttributes = otherAttributes?.Select(x => new XAttribute(x)).ToList()
                ?? new List<XAttribute>();
            this.writtenPath = writtenPath;
            this.writtenDirectory = writtenDirectory;
        }

        // rcc names a resource after the <file> text as written, so a loaded entry keeps its
        // text as long as the .qrc file stays in the directory it was loaded from.
        private readonly string writtenPath;
        private readonly string writtenDirectory;

        /// <summary>
        /// The prefix this file belongs to; null while the file is not part of a document.
        /// </summary>
        public QrcPrefix Prefix { get; internal set; }

        /// <summary>
        /// Absolute path of the file on disk.
        /// </summary>
        public string Path { get; }

        private string alias;
        /// <summary>
        /// Alias under which the file is accessible in the resource system; empty if none.
        /// </summary>
        public string Alias
        {
            get => alias;
            internal set
            {
                if (!SetField(ref alias, value ?? string.Empty))
                    return;
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(ResourcePath));
            }
        }

        /// <summary>
        /// Path as written to the .qrc file: the text it was loaded with, while the .qrc file is
        /// in the same directory; otherwise the path relative to the directory of the .qrc file.
        /// </summary>
        public string RelativePath
        {
            get
            {
                var directory = Prefix?.Document?.Directory;
                if (writtenPath != null && directory != null
                    && QrcPath.PathComparer.Equals(directory, writtenDirectory)) {
                    return writtenPath;
                }
                return QrcPath.ToRelative(directory, Path);
            }
        }

        /// <summary>
        /// Text shown in the editor tree: the relative path, followed by the alias, if any.
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(Alias)
            ? RelativePath : $"{RelativePath} ({Alias})";

        /// <summary>
        /// Path under which the file is accessible at runtime, e.g. ":/images/logo.png". As in
        /// rcc, "../shared/icon.png" under "/images" is ":/images/shared/icon.png".
        /// </summary>
        public string ResourcePath
        {
            get
            {
                var prefix = Prefix?.Name ?? "/";
                var separator = prefix.EndsWith("/") ? string.Empty : "/";
                var name = QrcPath.ToResourceName(string.IsNullOrEmpty(Alias) ? RelativePath : Alias);
                return $":{prefix}{separator}{name}";
            }
        }

        /// <summary>
        /// Attributes other than 'alias' (e.g. 'compress' or 'threshold'), kept when saving.
        /// </summary>
        internal List<XAttribute> OtherAttributes { get; }

        internal void NotifyPrefixChanged()
        {
            OnPropertyChanged(nameof(ResourcePath));
        }

        internal void NotifyLocationChanged()
        {
            OnPropertyChanged(nameof(RelativePath));
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(ResourcePath));
        }
    }
}
