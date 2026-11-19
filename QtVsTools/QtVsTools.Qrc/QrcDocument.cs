// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace QtVsTools.Qrc
{
    /// <summary>
    /// A Qt resource collection (.qrc) file.
    /// </summary>
    /// <remarks>
    /// File paths are kept as absolute paths and written relative to the directory of the .qrc
    /// file. All edits go through the <see cref="UndoStack"/>. Unknown attributes, the XML
    /// declaration and the &lt;!DOCTYPE RCC&gt; line are kept when saving; comments and unknown
    /// elements are not. Files are read in the encoding they declare and always written as
    /// UTF-8, with the encoding in the declaration changed accordingly.
    /// </remarks>
    public sealed class QrcDocument : Observable
    {
        private const string NewPrefixFormat = "/new/prefix{0}";

        private readonly ObservableCollection<QrcPrefix> prefixes = new();
        private XDeclaration declaration;
        private bool hasDocType;
        private bool hasByteOrderMark;
        private List<XAttribute> rootAttributes = new();
        private string newLine = "\n";

        public QrcDocument(string filePath)
        {
            this.filePath = Path.GetFullPath(filePath);
            Prefixes = new ReadOnlyObservableCollection<QrcPrefix>(prefixes);
            UndoStack = new UndoStack();
            UndoStack.PropertyChanged += UndoStack_PropertyChanged;
        }

        private string filePath;
        public string FilePath
        {
            get => filePath;
            private set
            {
                if (!SetField(ref filePath, value))
                    return;
                OnPropertyChanged(nameof(Directory));
                foreach (var file in prefixes.SelectMany(x => x.Files))
                    file.NotifyLocationChanged();
            }
        }

        /// <summary>
        /// Directory of the .qrc file; file paths are stored relative to it.
        /// </summary>
        public string Directory => Path.GetDirectoryName(FilePath);

        public ReadOnlyObservableCollection<QrcPrefix> Prefixes { get; }

        public UndoStack UndoStack { get; }

        /// <summary>
        /// True if the document has been modified since it was loaded or saved.
        /// </summary>
        public bool IsDirty => !UndoStack.IsClean;

        private void UndoStack_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UndoStack.IsClean))
                OnPropertyChanged(nameof(IsDirty));
        }

        #region Loading and saving

        /// <summary>
        /// Reads a .qrc file from disk.
        /// </summary>
        /// <exception cref="QrcException">The file cannot be read or is not a .qrc file.</exception>
        public static QrcDocument Load(string filePath)
        {
            byte[] bytes;
            try {
                bytes = File.ReadAllBytes(filePath);
            } catch (Exception exception) when (exception is IOException
                or UnauthorizedAccessException or NotSupportedException) {
                throw new QrcException(exception.Message, exception);
            }
            // A byte order mark decides the encoding, otherwise the XML declaration, otherwise
            // UTF-8. Saving always writes UTF-8 (see ToXml).
            using var reader = new StreamReader(new MemoryStream(bytes),
                DeclaredEncoding(bytes) ?? new UTF8Encoding(false), true);
            var document = Parse(reader.ReadToEnd(), filePath);
            var preamble = reader.CurrentEncoding.GetPreamble();
            document.hasByteOrderMark = preamble.Length > 0
                && bytes.Take(preamble.Length).SequenceEqual(preamble);
            return document;
        }

        private static readonly Regex EncodingDeclarationRegex = new(
            @"^\s*<\?xml[^>]*\bencoding\s*=\s*[""'](?<name>[^""']+)[""']");

        private static Encoding DeclaredEncoding(byte[] bytes)
        {
            // The declaration is ASCII in every encoding without a byte order mark.
            var head = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 256));
            var match = EncodingDeclarationRegex.Match(head);
            if (!match.Success)
                return null;
            try {
                return Encoding.GetEncoding(match.Groups["name"].Value);
            } catch (ArgumentException exception) {
                throw new QrcException(
                    $"Unsupported encoding '{match.Groups["name"].Value}'.", exception);
            }
        }

        /// <summary>
        /// Reads a .qrc file from a string. Relative file paths are resolved against the
        /// directory of <paramref name="filePath"/>.
        /// </summary>
        /// <exception cref="QrcException">The text is not a valid .qrc file.</exception>
        public static QrcDocument Parse(string xml, string filePath)
        {
            XDocument xmlDocument;
            try {
                var settings = new XmlReaderSettings
                {
                    // Accept <!DOCTYPE RCC>, but never resolve external entities.
                    DtdProcessing = DtdProcessing.Parse,
                    XmlResolver = null,
                    MaxCharactersFromEntities = 1024
                };
                using var reader = XmlReader.Create(new StringReader(xml), settings);
                xmlDocument = XDocument.Load(reader);
            } catch (XmlException exception) {
                throw new QrcException($"XML error: {exception.Message}", exception);
            }

            var root = xmlDocument.Root;
            if (root?.Name.LocalName != "RCC")
                throw new QrcException("The <RCC> root element is missing.");

            var document = new QrcDocument(filePath)
            {
                declaration = xmlDocument.Declaration == null ? null
                    : new XDeclaration(xmlDocument.Declaration),
                hasDocType = xmlDocument.DocumentType != null,
                rootAttributes = root.Attributes().Where(x => !x.IsNamespaceDeclaration)
                    .Select(x => new XAttribute(x)).ToList(),
                newLine = xml.Contains("\r\n") ? "\r\n" : "\n"
            };

            foreach (var resource in root.Elements("qresource")) {
                var prefix = new QrcPrefix(
                    (string)resource.Attribute("prefix"),
                    (string)resource.Attribute("lang"),
                    resource.Attributes().Where(x => !IsKnown(x, "prefix", "lang")));

                foreach (var element in resource.Elements("file")) {
                    var text = element.Value.Trim();
                    var path = ToAbsoluteFilePath(document.Directory, text);
                    prefix.InsertFile(prefix.Files.Count, new QrcFile(path,
                        (string)element.Attribute("alias"),
                        element.Attributes().Where(x => !IsKnown(x, "alias")),
                        text, document.Directory));
                }
                document.InsertPrefixCore(document.prefixes.Count, prefix);
            }
            return document;
        }

        private static string ToAbsoluteFilePath(string directory, string path)
        {
            try {
                return QrcPath.ToAbsolute(directory, path);
            } catch (ArgumentException exception) {
                throw new QrcException($"Invalid file path '{path}'.", exception);
            }
        }

        private static bool IsKnown(XAttribute attribute, params string[] names)
        {
            return attribute.IsNamespaceDeclaration || names.Contains(attribute.Name.LocalName);
        }

        // The declaration as found, except that a declared encoding other than UTF-8 is replaced,
        // since the document is always written as UTF-8.
        private string DeclarationForUtf8()
        {
            var encoding = declaration.Encoding;
            if (string.IsNullOrEmpty(encoding)
                || encoding.Equals("utf-8", StringComparison.OrdinalIgnoreCase)
                || encoding.Equals("utf8", StringComparison.OrdinalIgnoreCase)) {
                return declaration.ToString();
            }
            return new XDeclaration(declaration.Version, "UTF-8", declaration.Standalone).ToString();
        }

        /// <summary>
        /// Returns the XML text of the document as it would be written by <see cref="Save"/>.
        /// </summary>
        public string ToXml()
        {
            var root = new XElement("RCC", rootAttributes.Select(x => new XAttribute(x)));
            foreach (var prefix in prefixes) {
                var resource = new XElement("qresource", new XAttribute("prefix", prefix.Name));
                if (!string.IsNullOrEmpty(prefix.Language))
                    resource.Add(new XAttribute("lang", prefix.Language));
                resource.Add(prefix.OtherAttributes.Select(x => new XAttribute(x)));

                foreach (var file in prefix.Files) {
                    var element = new XElement("file");
                    if (!string.IsNullOrEmpty(file.Alias))
                        element.Add(new XAttribute("alias", file.Alias));
                    element.Add(file.OtherAttributes.Select(x => new XAttribute(x)));
                    element.Value = file.RelativePath;
                    resource.Add(element);
                }
                root.Add(resource);
            }

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                IndentChars = "    ",
                NewLineChars = newLine,
                NewLineHandling = NewLineHandling.Replace,
                // Written as found below; the writer would change its spelling and drop
                // 'standalone'.
                OmitXmlDeclaration = true
            };

            using var stream = new MemoryStream();
            using (var writer = XmlWriter.Create(stream, settings)) {
                if (declaration != null)
                    writer.WriteRaw(DeclarationForUtf8() + newLine);
                if (hasDocType) // WriteDocType() would write "<!DOCTYPE RCC >".
                    writer.WriteRaw("<!DOCTYPE RCC>" + newLine);
                root.WriteTo(writer);
            }
            return new UTF8Encoding(false).GetString(stream.ToArray()) + newLine;
        }

        /// <summary>
        /// Writes the document to <see cref="FilePath"/> and marks it as unmodified.
        /// </summary>
        /// <exception cref="QrcException">The file cannot be written.</exception>
        public void Save()
        {
            try {
                File.WriteAllText(FilePath, ToXml(), new UTF8Encoding(hasByteOrderMark));
            } catch (Exception exception) when (exception is IOException
                or UnauthorizedAccessException or NotSupportedException) {
                throw new QrcException(exception.Message, exception);
            }
            UndoStack.SetClean();
        }

        /// <summary>
        /// Writes the document to a new location. File paths are written relative to the new
        /// location from then on.
        /// </summary>
        /// <exception cref="QrcException">The file cannot be written.</exception>
        public void SaveAs(string filePath)
        {
            var previous = FilePath;
            FilePath = Path.GetFullPath(filePath);
            try {
                Save();
            } catch (QrcException) {
                FilePath = previous;
                throw;
            }
        }

        #endregion

        #region Editing

        /// <summary>
        /// Returns the absolute path of a path relative to the document directory.
        /// </summary>
        public string ToAbsolutePath(string path) => QrcPath.ToAbsolute(Directory, path);

        /// <summary>
        /// Returns true if <paramref name="path"/> is not located in the document directory or
        /// one of its subdirectories.
        /// </summary>
        public bool IsOutsideDirectory(string path) => QrcPath.IsOutside(Directory, path);

        /// <summary>
        /// Appends a new prefix with a unique name of the form "/new/prefixN".
        /// </summary>
        public QrcPrefix AddPrefix()
        {
            var number = 1;
            string name;
            while (FindPrefixes(name = string.Format(NewPrefixFormat, number)).Any())
                number++;

            var prefix = new QrcPrefix(name);
            UndoStack.Push(new InsertPrefixCommand(this, prefix, prefixes.Count));
            return prefix;
        }

        /// <summary>
        /// Appends files to a prefix. Files already contained in the prefix and duplicates are
        /// skipped. Returns the added files.
        /// </summary>
        /// <exception cref="ArgumentException">A path is not a valid Windows path. Invalid
        /// paths in a .qrc file are reported by <see cref="Parse"/> as a QrcException
        /// instead.</exception>
        public IReadOnlyList<QrcFile> AddFiles(QrcPrefix prefix, IEnumerable<string> paths)
        {
            CheckOwner(prefix);

            var added = new List<QrcFile>();
            var seen = new HashSet<string>(QrcPath.PathComparer);
            foreach (var path in paths.Select(ToAbsolutePath)) {
                if (!prefix.Contains(path) && seen.Add(path))
                    added.Add(new QrcFile(path));
            }

            if (added.Count > 0)
                UndoStack.Push(new InsertFilesCommand(prefix, added, prefix.Files.Count));
            return added;
        }

        public void RemovePrefix(QrcPrefix prefix)
        {
            CheckOwner(prefix);
            UndoStack.Push(new RemovePrefixCommand(this, prefix));
        }

        public void RemoveFile(QrcFile file)
        {
            if (file?.Prefix == null)
                throw new ArgumentException("The file is not part of a document.", nameof(file));
            CheckOwner(file.Prefix);
            UndoStack.Push(new RemoveFileCommand(file));
        }

        /// <summary>
        /// Returns false if renaming <paramref name="prefix"/> to <paramref name="name"/> would
        /// clash with another prefix that has the same name and language, unless the prefix
        /// was loaded or created with that name and language.
        /// </summary>
        public bool CanRenamePrefix(QrcPrefix prefix, string name)
        {
            CheckOwner(prefix);
            var fixedName = QrcPath.FixPrefix(name);
            return prefix.IsInitial(fixedName, prefix.Language)
                || !FindPrefixes(fixedName).Any(x => x != prefix && x.Language == prefix.Language);
        }

        /// <summary>
        /// Changes the name of a prefix. Consecutive changes with the same non-negative
        /// <paramref name="mergeId"/> are undone in one step. Returns false and leaves the
        /// prefix unchanged if the new name clashes with another prefix.
        /// </summary>
        public bool RenamePrefix(QrcPrefix prefix, string name, int mergeId = -1)
        {
            if (!CanRenamePrefix(prefix, name))
                return false;

            var fixedName = QrcPath.FixPrefix(name);
            if (fixedName != prefix.Name) {
                UndoStack.Push(new SetPropertyCommand("Change Prefix", prefix,
                    nameof(QrcPrefix.Name), prefix.Name, fixedName, mergeId,
                    x => prefix.Name = x));
            }
            return true;
        }

        /// <summary>
        /// Returns false if changing the language of <paramref name="prefix"/> to
        /// <paramref name="language"/> would clash with another prefix that has the same name
        /// and language, unless the prefix was loaded or created with that name and language.
        /// </summary>
        public bool CanSetLanguage(QrcPrefix prefix, string language)
        {
            CheckOwner(prefix);
            language ??= string.Empty;
            return prefix.IsInitial(prefix.Name, language)
                || !FindPrefixes(prefix.Name).Any(x => x != prefix && x.Language == language);
        }

        /// <summary>
        /// Changes the language of a prefix. Consecutive changes with the same non-negative
        /// <paramref name="mergeId"/> are undone in one step. Returns false and leaves the
        /// prefix unchanged if the new language clashes with another prefix.
        /// </summary>
        public bool SetLanguage(QrcPrefix prefix, string language, int mergeId = -1)
        {
            if (!CanSetLanguage(prefix, language))
                return false;

            language ??= string.Empty;
            if (language != prefix.Language) {
                UndoStack.Push(new SetPropertyCommand("Change Language", prefix,
                    nameof(QrcPrefix.Language), prefix.Language, language, mergeId,
                    x => prefix.Language = x));
            }
            return true;
        }

        /// <summary>
        /// Changes the alias of a file. Consecutive changes with the same non-negative
        /// <paramref name="mergeId"/> are undone in one step.
        /// </summary>
        public void SetAlias(QrcFile file, string alias, int mergeId = -1)
        {
            if (file?.Prefix == null)
                throw new ArgumentException("The file is not part of a document.", nameof(file));
            CheckOwner(file.Prefix);
            alias ??= string.Empty;
            if (alias == file.Alias)
                return;
            UndoStack.Push(new SetPropertyCommand("Change Alias", file,
                nameof(QrcFile.Alias), file.Alias, alias, mergeId, x => file.Alias = x));
        }

        private IEnumerable<QrcPrefix> FindPrefixes(string name)
        {
            return prefixes.Where(x => x.Name == name);
        }

        private void CheckOwner(QrcPrefix prefix)
        {
            if (prefix == null)
                throw new ArgumentNullException(nameof(prefix));
            if (prefix.Document != this)
                throw new ArgumentException("The prefix is not part of this document.",
                    nameof(prefix));
        }

        internal void InsertPrefixCore(int index, QrcPrefix prefix)
        {
            prefix.Document = this;
            prefixes.Insert(index, prefix);
        }

        internal void RemovePrefixCore(QrcPrefix prefix)
        {
            prefixes.Remove(prefix);
            prefix.Document = null;
        }

        #endregion
    }
}
