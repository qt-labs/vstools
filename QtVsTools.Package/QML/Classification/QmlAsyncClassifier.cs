/****************************************************************************
**
** Copyright (C) 2018 The Qt Company Ltd.
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

/// This file contains the definition of the abstract class QmlAsyncClassifier which is the base
/// class for asynchronous implementations of text classifiers, e.g. for syntax highlighting and
/// syntax error annotations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace QtVsTools.Qml.Classification
{
    using HelperTypes;

    /// <summary>
    /// A SharedTagList is a list of tracking tags (instances of TrackingTag), sorted by starting
    /// location. It works as write-once-read-many data storage. Write access must be requested and
    /// will only be granted to the first object/thread, which will be responsible for filling in
    /// the data. Once writing is complete, concurrent read-only access will then be allowed.
    /// </summary>
    class SharedTagList : Concurrent
    {
        readonly SortedList<int, TrackingTag> data = new SortedList<int, TrackingTag>();
        object owner;

        public bool Ready { get; private set; }

        public enum AccessType { ReadOnly, ReadWrite }
        public AccessType RequestWriteAccess(object client)
        {
            EnterCriticalSection();
            if (owner == null) {
                owner = client;
                return AccessType.ReadWrite;
            } else {
                LeaveCriticalSection();
                return AccessType.ReadOnly;
            }
        }

        public void WriteComplete(object client)
        {
            if (owner != client)
                return;
            Ready = true;
            try {
                LeaveCriticalSection();
            } catch { }
        }

        public void AddRange(object client, IEnumerable<TrackingTag> tags)
        {
            if (owner != client || Ready)
                return;
            foreach (var tag in tags)
                Add(client, tag);
        }

        public void Add(object client, TrackingTag tag)
        {
            if (owner != client || Ready)
                return;
            data[tag.Start] = tag;
        }

        class TrackingTagComparer : Comparer<TrackingTag>
        {
            readonly ITextSnapshot snapshot;
            public TrackingTagComparer(ITextSnapshot snapshot)
            {
                this.snapshot = snapshot;
            }
            public override int Compare(TrackingTag t1, TrackingTag t2)
            {
                int t1Version = t1.Snapshot.Version.VersionNumber;
                int t2Version = t2.Snapshot.Version.VersionNumber;
                if (t1Version == t2Version && t2Version == snapshot.Version.VersionNumber)
                    return Comparer<int>.Default.Compare(t1.Start, t2.Start);

                var t1Mapped = t1.MapToSnapshot(snapshot);
                var t2Mapped = t2.MapToSnapshot(snapshot);
                return Comparer<int>.Default.Compare(t1Mapped.Span.Start, t2Mapped.Span.Start);
            }
        }

        /// <summary>
        /// Perform a binary search to find the tag whose start precedes a given location relative
        /// to a text snapshot. If the tags in the list are relative to another version of the
        /// text, their location will be mapped to the given snapshot.
        /// </summary>
        /// <param name="snapshot">Text snapshot</param>
        /// <param name="location">Location in the given snapshot</param>
        /// <returns>
        /// Index of the tag in the list; -1 indicates error
        /// </returns>
        public int FindTagAtLocation(ITextSnapshot snapshot, int location)
        {
            if (!Ready)
                return -1;

            var firstTag = data.Values.FirstOrDefault();
            if (firstTag == null)
                return -1;

            bool sameVersion =
                (firstTag.Snapshot.Version.VersionNumber == snapshot.Version.VersionNumber);

            int? idx = null;
            if (sameVersion) {
                idx = data.Keys.BinarySearch(location);
            } else {
                if (location >= snapshot.Length)
                    return -1;
                var locationTag = new TrackingTag(snapshot, location, 1);
                var comparer = new TrackingTagComparer(snapshot);
                idx = data.Values.BinarySearch(locationTag, comparer);
            }
            if (idx == null)
                return -1;

            if (idx < 0) {
                // location was not found; idx has the bitwise complement of the smallest element
                // that is after location, or the bitwise complement of the list count in case all
                // elements are before location.

                if (~idx == 0) // first tag starts after location
                    return -1;

                // Because we are looking for the nearest tag that starts before location, we will
                // return the element that precedes the one found
                idx = ~idx - 1;
            }

            return idx.Value;
        }

        public IList<TrackingTag> Values
        {
            get
            {
                if (!Ready)
                    return new List<TrackingTag>();
                return data.Values;
            }
        }
    }

    /// <summary>
    /// Base class for QML classifier classes implementing the ITagger interface. This interface
    /// is used in the Visual Studio text editor extensibility for e.g. syntax highlighting. The
    /// processing of the QML source code is done asynchronously in a background thread in order
    /// to prevent the UI thread from blocking.
    ///
    /// The result of the processing is a list of tracking tags that is stored in a SharedTagList
    /// and is made available to any instances of QmlAsyncClassifier working on the same source
    /// code. This prevents the processing being invoked more than once for any given version of
    /// that source code.
    ///
    /// Derived classes are required to implement the processing of the source code as well as the
    /// conversion from TrackingTag to the type expected by the Visual Studio text editor.
    /// </summary>
    /// <typeparam name="T">
    /// Type of classification tag expected by the Visual Studio text editor extensibility
    /// </typeparam>
    abstract class QmlAsyncClassifier<T> : ITagger<T> where T : ITag
    {
        protected enum ClassificationRefresh
        {
            FullText,
            TagsOnly
        }

        /// <summary>
        /// Process QML source code. Implementations will override this method with the specific
        /// processing required to convert the parser results into a list of tracking tags
        /// </summary>
        /// <param name="snapshot">The current version of the source code</param>
        /// <param name="parseResult">The result of parsing the source code</param>
        /// <param name="tagList">Shared list of tracking tags</param>
        /// <param name="writeAccess">
        /// If true, the instance is required to populate the list of tags;
        /// otherwise, the instance has read-only access and cannot modify the list.
        /// </param>
        /// <returns>
        /// Hint on how to notify Visual Studio concerning the tags in the list
        ///     FullText: refresh the entire contents of the text editor
        ///     TagsOnly: refresh only the spans pointed to by the tags
        /// </returns>
        protected abstract ClassificationRefresh ProcessText(
            ITextSnapshot snapshot,
            Parser parseResult,
            SharedTagList tagList,
            bool writeAccess);

        /// <summary>
        /// Conversion from TrackingTag to the type T of classification tag expected by the
        /// Visual Studio text editor extensibility.
        /// </summary>
        /// <param name="tag">TrackingTag to convert</param>
        /// <returns>Instance of T corresponding to the given TrackingTag</returns>
        protected abstract T GetClassification(TrackingTag tag);

        protected ITextView TextView { get; }
        protected ITextBuffer Buffer { get; }

        readonly object criticalSection = new object();
        readonly string classificationType;
        ParserKey currentParserKey;
        TagListKey currentTagListKey;
        SharedTagList currentTagList;
        readonly Dispatcher dispatcher;
        readonly DispatcherTimer timer;
        bool flag = false;

        protected QmlAsyncClassifier(
            string classificationType,
            ITextView textView,
            ITextBuffer buffer)
        {
            TextView = textView;
            textView.Closed += TextView_Closed;
            Buffer = buffer;
            buffer.Changed += Buffer_Changed;

            dispatcher = Dispatcher.CurrentDispatcher;
            timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle, dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            timer.Tick += Timer_Tick;

            currentParserKey = null;
            currentTagListKey = null;
            currentTagList = null;
            this.classificationType = classificationType;

            AsyncParse(buffer.CurrentSnapshot);
        }

        private void TextView_Closed(object sender, EventArgs e)
        {
            if (currentParserKey != null) {
                ParserStore.Instance.Release(this, currentParserKey);
                currentParserKey = null;
            }
            if (currentTagListKey != null) {
                TagListStore.Instance.Release(this, currentTagListKey);
                currentTagListKey = null;
            }
            currentTagList = null;
        }

        private void Buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            timer.Stop();
            AsyncParse(e.After);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            AsyncParse(Buffer.CurrentSnapshot);
        }

        private async void AsyncParse(ITextSnapshot snapshot)
        {
            lock (criticalSection) {
                if (flag)
                    return;
                flag = true;
            }

            var newParserKey = new ParserKey(snapshot);
            var newTagListKey = new TagListKey(classificationType, snapshot);
            if (newParserKey == currentParserKey || newTagListKey == currentTagListKey)
                return;

            ParserKey oldParserKey = null;
            TagListKey oldTagListKey = null;

            await Task.Run(() =>
            {
                var parser = ParserStore.Instance.Get(this, newParserKey);

                var tagList = TagListStore.Instance.Get(this, newTagListKey);
                var refresh = ClassificationRefresh.FullText;
                try {
                    var accessType = tagList.RequestWriteAccess(this);
                    refresh = ProcessText(snapshot, parser, tagList,
                        accessType == SharedTagList.AccessType.ReadWrite);
                } finally {
                    tagList.WriteComplete(this);
                }

                oldParserKey = currentParserKey;
                currentParserKey = newParserKey;
                oldTagListKey = currentTagListKey;
                currentTagListKey = newTagListKey;
                currentTagList = tagList;

                RefreshClassification(snapshot, refresh, tagList);

                var currentVersion = Buffer.CurrentSnapshot.Version;
                if (snapshot.Version.VersionNumber == currentVersion.VersionNumber)
                    timer.Stop();
                else
                    timer.Start();
            });

            lock (criticalSection) {
                flag = false;
            }

            await Task.Run(() =>
            {
                if (oldParserKey != null)
                    ParserStore.Instance.Release(this, oldParserKey);
                if (oldTagListKey != null)
                    TagListStore.Instance.Release(this, oldTagListKey);
            });
        }

        private void RefreshClassification(
            ITextSnapshot snapshot,
            ClassificationRefresh refresh,
            SharedTagList tagList)
        {
            var tagsChangedHandler = TagsChanged;
            if (refresh == ClassificationRefresh.FullText) {
                var span = new SnapshotSpan(Buffer.CurrentSnapshot,
                    0, Buffer.CurrentSnapshot.Length);
                if (tagsChangedHandler != null)
                    tagsChangedHandler.Invoke(this, new SnapshotSpanEventArgs(span));
            } else {
                foreach (var tag in tagList.Values) {
                    var tagMapped = tag.MapToSnapshot(snapshot);
                    if (tagsChangedHandler != null)
                        tagsChangedHandler.Invoke(this, new SnapshotSpanEventArgs(tagMapped.Span));
                }
            }
        }

        public IEnumerable<ITagSpan<T>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (currentTagList == null || !currentTagList.Ready)
                yield break;

            var firstTag = currentTagList.Values.FirstOrDefault();
            if (firstTag == null)
                yield break;

            var snapshot = spans[0].Snapshot;

            bool sameVersion =
                (firstTag.Snapshot.Version.VersionNumber == snapshot.Version.VersionNumber);

            foreach (var span in spans) {

                int idx = currentTagList.FindTagAtLocation(snapshot, span.Start);
                if (idx == -1)
                    continue;

                for (; idx < currentTagList.Values.Count; idx++) {

                    var tag = currentTagList.Values[idx];

                    if (sameVersion && tag.Start > span.End)
                        break;

                    var tagMapped = tag.MapToSnapshot(snapshot);
                    if (tagMapped.Span.Length == 0)
                        continue;

                    if (!sameVersion && tagMapped.Span.Start > span.End)
                        break;

                    if (!span.IntersectsWith(tagMapped.Span))
                        continue;

                    var classification = GetClassification(tag);
                    if (classification == null)
                        continue;

                    var tracking = tagMapped.Tag.Span;
                    yield return
                        new TagSpan<T>(tracking.GetSpan(snapshot), classification);
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }

    namespace HelperTypes
    {
        public static class BinarySearchExtensions
        {
            /// <summary>
            /// Generic BinarySearch method that will work on any IList(T),
            /// based on Microsoft’s ownArray.BinarySearch(T) implementation
            /// Adapted from http://philosopherdeveloper.com/posts/whats-annoying-about-sorted-list-index-of-key.html
            /// </summary>
            /// <returns>
            /// The index of the specified value in the specified list, if value is found;
            /// otherwise, a negative number. If value is not found and value is less than one or
            /// more elements in list, the negative number returned is the bitwise complement of
            /// the index of the first element that is larger than value. If value is not found and
            /// value is greater than all elements in list, the negative number returned is the
            /// bitwise complement of (the index of the last element plus 1). If this method is
            /// called with a non-sorted list, the return value can be incorrect and a negative
            /// number could be returned, even if value is present in list.
            /// (cf. https://docs.microsoft.com/en-us/dotnet/api/system.array.binarysearch)
            ///
            /// In case of error, returns null.
            /// </returns>
            public static int? BinarySearch<T>(
                this IList<T> list,
                int index,
                int length,
                T value,
                IComparer<T> comparer)
            {
                if (list == null)
                    return null;
                if (index < 0 || length < 0)
                    return null;
                if (list.Count - index < length)
                    return null;

                int lower = index;
                int upper = (index + length) - 1;

                while (lower <= upper) {
                    int adjustedIndex = lower + ((upper - lower) >> 1);
                    int comparison = comparer.Compare(list[adjustedIndex], value);
                    if (comparison == 0)
                        return adjustedIndex;
                    else if (comparison < 0)
                        lower = adjustedIndex + 1;
                    else
                        upper = adjustedIndex - 1;
                }

                return ~lower;
            }

            public static int? BinarySearch<T>(this IList<T> list, T value, IComparer<T> comparer)
            {
                return list.BinarySearch(0, list.Count, value, comparer);
            }

            public static int? BinarySearch<T>(this IList<T> list, T value)
                where T : IComparable<T>
            {
                return list.BinarySearch(value, Comparer<T>.Default);
            }
        }

        /// <summary>
        /// Base class for thread-safe, indexed data storage. References to stored values are
        /// explicitly tracked to allow for timely disposal as soon as a value becomes
        /// unreferenced. Shared data stores are intended to be used as singletons. For this
        /// purpose, classes that inherit from SharedDataStore will include a static instance
        /// member.
        /// </summary>
        /// <typeparam name="TKey">Value key type</typeparam>
        /// <typeparam name="TValue">Value type</typeparam>
        /// <typeparam name="TInstance">
        /// Type of singleton instance, i.e. the same class that is derived from SharedDataStore
        /// </typeparam>
        abstract class SharedDataStore<TKey, TValue, TInstance>
            where TInstance : SharedDataStore<TKey, TValue, TInstance>, new()
        {
            protected abstract TValue GetDefaultValue(TKey key);

            class ValueRef
            {
                public TValue Value { get; set; }
                public HashSet<object> ClientObjects { get; set; }
            }
            readonly Dictionary<TKey, ValueRef> data = new Dictionary<TKey, ValueRef>();

            static readonly object staticCriticalSection = new object();
            readonly object criticalSection = new object();

            protected SharedDataStore()
            {
                data = new Dictionary<TKey, ValueRef>();
            }

            public TValue Get(object client, TKey key)
            {
                lock (criticalSection) {
                    ValueRef valueRef;
                    if (!data.TryGetValue(key, out valueRef)) {
                        valueRef = new ValueRef
                        {
                            Value = GetDefaultValue(key),
                            ClientObjects = new HashSet<object> { client }
                        };
                        data.Add(key, valueRef);
                    } else {
                        valueRef.ClientObjects.Add(client);
                    }
                    return valueRef.Value;
                }
            }

            public void Release(object client, TKey key)
            {
                IDisposable disposable = null;
                lock (criticalSection) {
                    ValueRef valueRef;
                    if (data.TryGetValue(key, out valueRef)) {
                        valueRef.ClientObjects.Remove(client);
                        if (valueRef.ClientObjects.Count == 0) {
                            data.Remove(key);
                            disposable = valueRef.Value as IDisposable;
                        }
                    }
                }
                if (disposable != null)
                    disposable.Dispose();
            }

            private static TInstance instance = null;
            public static TInstance Instance
            {
                get
                {
                    lock (staticCriticalSection) {
                        if (instance == null) {
                            instance = new TInstance();
                        }
                        return instance;
                    }
                }
            }
        }

        class TagListKey
        {
            public string Classification { get; }
            public ITextSnapshot Snapshot { get; }
            public TagListKey(string classification, ITextSnapshot snapshot)
            {
                Classification = classification;
                Snapshot = snapshot;
            }

            public override bool Equals(object obj)
            {
                var that = obj as TagListKey;
                if (that == null)
                    return false;
                if (Classification != that.Classification)
                    return false;
                if (Snapshot.TextBuffer != that.Snapshot.TextBuffer)
                    return false;
                if (Snapshot.Version.VersionNumber != that.Snapshot.Version.VersionNumber)
                    return false;
                return true;
            }

            public override int GetHashCode()
            {
                var hashBase = new Tuple<string, ITextBuffer, int>(
                    Classification, Snapshot.TextBuffer,
                    Snapshot.Version.VersionNumber);
                return hashBase.GetHashCode();
            }
        }

        class TagListStore : SharedDataStore<TagListKey, SharedTagList, TagListStore>
        {
            protected override SharedTagList GetDefaultValue(TagListKey key)
            {
                return new SharedTagList();
            }
        }

        class ParserKey
        {
            public ITextSnapshot Snapshot { get; }
            public ParserKey(ITextSnapshot snapshot)
            {
                Snapshot = snapshot;
            }

            public override bool Equals(object obj)
            {
                var that = obj as ParserKey;
                if (that == null)
                    return false;
                if (Snapshot.TextBuffer != that.Snapshot.TextBuffer)
                    return false;
                if (Snapshot.Version.VersionNumber != that.Snapshot.Version.VersionNumber)
                    return false;
                return true;
            }

            public override int GetHashCode()
            {
                var hashBase = new Tuple<ITextBuffer, int>(
                    Snapshot.TextBuffer, Snapshot.Version.VersionNumber);
                return hashBase.GetHashCode();
            }
        }

        class ParserStore : SharedDataStore<ParserKey, Parser, ParserStore>
        {
            protected override Parser GetDefaultValue(ParserKey key)
            {
                return Parser.Parse(key.Snapshot.GetText());
            }
        }
    }
}
