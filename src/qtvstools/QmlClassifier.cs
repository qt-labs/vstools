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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;

namespace QmlClassifier
{
    /// <summary>
    /// Classifier that classifies all text as an instance of the OrinaryClassifierType
    /// </summary>
    internal class Classifier : IClassifier
    {
        // Multi-line comments need some extra care
        protected List<MultilineCommentToken> multiLineCommentTokens;

        protected List<string> keywords; // Qml keywords
        protected List<string> jsKeywords; // Javascript keywords
        protected List<string> types; // Qml types
        protected List<string> properties; // Qml keyword 'property xxx'

        protected List<char> separators;
        protected char[] whiteSpaceChars;

        //#pragma warning disable 67
        // This event gets raised if a non-text change would affect the classification in some way,
        // for example typing /* would cause the classification to change in C# without directly
        // affecting the span.
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
        //#pragma warning restore 67
        readonly IClassificationTypeRegistryService classificationRegistryService;

        internal Classifier(IClassificationTypeRegistryService registry)
        {
            classificationRegistryService = registry;
            multiLineCommentTokens = new List<MultilineCommentToken>();

            whiteSpaceChars = new char[] { ' ', '\t' };
            separators = new List<char>
            {
                ' ', '\t',
                ';', ':',
                ',', '.',
                '{', '}',
                '[', ']',
                '(', ')',
                '='
            };

            keywords = new List<string>
            {
                "property",
                "alias",
                "signal",
                "readonly",
                "import",
                "on"
            };

            properties = new List<string>
            {
                "property action",
                "property bool",
                "property color",
                "property date",
                "property double",
                "property enumeration",
                "property font",
                "property int",
                "property list",
                "property point",
                "property real",
                "property rect",
                "property size",
                "property string",
                "property time",
                "property url",
                "property variant",
                "property var",
                "property vector3d"
            };

            jsKeywords = new List<string>
            {
                "break",
                "case",
                "catch",
                "continue",
                "debugger",
                "default",
                "delete",
                "do",
                "else",
                "finally",
                "for",
                "function",
                "if",
                "in",
                "instanceof",
                "new",
                "return",
                "switch",
                "this",
                "throw",
                "try",
                "typeof",
                "var",
                "void",
                "while",
                "with"
            };

            types = new List<string>
            {
                /* TODO
                "action",
                "bool",
                "color",
                "date",
                "double",
                "enumeration",
                "font",
                "int",
                "list",
                "point",
                "real",
                "rect",
                "size",
                "string",
                "time",
                "url",
                "variant",
                "var",
                "vector3d",
                 * */
            };

        }

        private void OnClassificationChanged(SnapshotSpan changeSpan)
        {
            if (ClassificationChanged != null)
                ClassificationChanged(this, new ClassificationChangedEventArgs(changeSpan));
        }

        // Re-classify given span
        protected void Invalidate(SnapshotSpan span)
        {
            if (ClassificationChanged != null)
                ClassificationChanged(this, new ClassificationChangedEventArgs(span));
        }

        protected Token ReadMultiLineEOL(string text, int index, int length)
        {
            var token = new MultilineCommentToken();
            for (int i = index; i < length; i++) {
                try {
                    char ch = text[i];
                    char next_ch = text[i + 1];
                    if (ch == '*' && next_ch == '/') {
                        token.Length = index + i + 2;
                        return token;
                    }
                } catch (System.IndexOutOfRangeException) {
                    // end not found :(
                }
            }
            // End not found, use full length
            token.Length = length - index;
            return token;
        }

        protected Token ReadString(string text, int index, int length)
        {
            var token = new StringToken();
            for (int i = index; i < length; i++) {
                try {
                    char ch = text[i];
                    char next_ch = text[i + 1];
                    if (next_ch == '\"') {
                        token.Length = i - index + 2;
                        return token;
                    }
                } catch (System.IndexOutOfRangeException) {
                    // end not found :(
                }
            }
            // End not found, use full length
            token.Length = length - index;
            return token;
        }

        protected Token GetToken(string word, bool colonComing = false, string following = "")
        {
            if (jsKeywords.Contains(word))
                return new JsKeywordToken();

            if (keywords.Contains(word)) {
                if (colonComing)
                    return new OtherToken();

                following = following.TrimStart(whiteSpaceChars);
                if (following.Length > 0 && following[0] == ':')
                    return new OtherToken();

                return new KeywordToken();
            }

            if (properties.Contains(word))
                return new PropertyToken();

            if (types.Contains(word))
                return new TypeToken();

            return new OtherToken();
        }

        protected Token Scan(string text, int index, int length, TokenType tokenType = TokenType.None,
                             int state = 0)
        {
            // End of multi-line comment not reached yet, so find it.
            if (tokenType == TokenType.MultilineComment && state != 0)
                return ReadMultiLineEOL(text, index, length);

            // Special case for finding property stuff
            if (text.Substring(index).StartsWith("property ", StringComparison.Ordinal)) {
                var possibly_property_type = text.Substring(index, length - index);
                int value_len = 0;
                try {
                    foreach (var value in properties) {
                        if (possibly_property_type.StartsWith(value, StringComparison.Ordinal)) {
                            if (possibly_property_type.Length == value.Length) {
                                // Same length --> match
                                value_len = value.Length;
                                break;
                            }
                            char ch = possibly_property_type[value.Length];
                            if (ch == ' ' || ch == '\r' || ch == '\n') {
                                // Space or line break --> match
                                value_len = value.Length;
                                break;
                            }
                        }
                    }
                    if (value_len > 0) {
                        var token = new PropertyToken();
                        token.Length = value_len;
                        return token;
                    }
                } catch (System.IndexOutOfRangeException) {
                    // pass
                }

            }

            bool inMultilineComment = false;
            for (int i = index; i < length; i++) {
                char ch = text[i];

                char next_ch = '\0'; // doesn't matter which the default is until not '*' or '/'
                try {
                    next_ch = text[i + 1];
                } catch (System.IndexOutOfRangeException) {
                    // pass
                }
                // If we are in beginning of search and not inside multi-line comment parsing
                if (i == index && !inMultilineComment) {
                    if (ch == '\r' || ch == '\n') {
                        continue;
                    }
                    // If string start found, read rest of it
                    if (ch == '\"') {
                        return ReadString(text, index, length);
                    }
                    // Single line comment start found,
                    else if (ch == '/' && next_ch == '/') {
                        // One line comment starting
                        var token = new CommentToken();
                        token.Length = length - index;
                        return token;
                    }
                    // Cut now ??
                    else if (separators.Contains(ch)) {
                        var token = new OtherToken();
                        token.Length = 1;
                        return token;
                    }
                    // Multi-line comment start found (or sure can be one liner also)
                    else if (ch == '/' && next_ch == '*') {
                        // Multi-line comment starting, perhaps
                        inMultilineComment = true;
                        // Skip over comment starts '/*'
                        for (int j = i + 2; j < length; j++) {
                            i = j;
                            try {
                                ch = text[j];
                                next_ch = text[j + 1];
                                next_ch = text[j + 1];
                                if (ch == '*' && next_ch == '/') {
                                    // This was one liner so using normal comment token
                                    var token = new CommentToken();
                                    token.Length = j - index + 2;
                                    return token;
                                }
                            } catch (System.IndexOutOfRangeException) {
                                if (ch == '\r' || ch == '\n') {
                                    continue;
                                }
                            }
                        }
                    }
                }
                // Cut now if string starting next
                if (separators.Contains(ch) && next_ch == '\"' && !inMultilineComment) {
                    Token token = null;
                    var tmp = text.Substring(index, i - index);
                    token = GetToken(tmp);
                    token.Length = i - index;
                    return token;
                } else if (separators.Contains(ch)) {
                    Token token = null;
                    if (inMultilineComment) {
                        token = new MultilineCommentToken();
                        token.State = 1;
                    } else {
                        var tmp = text.Substring(index, i - index);
                        var follows = text.Substring(index + tmp.Length);
                        token = GetToken(tmp, ch == ':' || next_ch == ':', follows);
                    }
                    token.Length = i - index;
                    return token;
                } else if (ch == '\r' || ch == '\n') {
                    Token token = null;
                    if (inMultilineComment) {
                        token = new MultilineCommentToken();
                        token.State = 1;
                    } else {
                        token = GetToken(text.Substring(index, i - index));
                    }
                    token.Length = i - index + 1;
                    return token;
                }
                  // There is a comment coming so this token ends now
                  else if (ch == '/' && next_ch == '/') {
                    Token token = null;
                    token = GetToken(text.Substring(index, i - index));
                    token.Length = i - index; // +1;
                    return token;
                }
                  // Multi-line comment start found (or sure can be one liner also)
                  // so this token ends now
                  else if (ch == '/' && next_ch == '*') {
                    // Multi-line comment starting, perhaps
                    inMultilineComment = true;
                    var tmp = text.Substring(index, i - index);
                    tmp = tmp.Trim();
                    if (tmp.Length != 0) {
                        if (jsKeywords.Contains(tmp)) {
                            var token = new JsKeywordToken();
                            token.Length = index + i;
                            return token;
                        } else {
                            var token = new OtherToken();
                            token.Length = i - index;
                            return token;
                        }
                    }
                }
            }

            // End not found yet
            if (inMultilineComment) {
                var token = new MultilineCommentToken();
                token.Length = length - index;
                token.State = 1;
                return token;
            } else {
                var tmp = text.Substring(index, length - index);
                char[] trimChars = { ' ', ';' };
                tmp = tmp.Trim(trimChars);
                if (jsKeywords.Contains(tmp)) {
                    var token = new JsKeywordToken();
                    token.Length = length - index;
                    return token;
                }
            }

            var ret = new OtherToken();
            ret.Length = length - index;
            return ret;
        }

        /// <summary>
        /// This method scans the given SnapshotSpan for potential matches for this classification.
        /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
        /// </summary>
        /// <param name="span">The span currently being classified</param>
        /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification</returns>
        IList<ClassificationSpan> IClassifier.GetClassificationSpans(SnapshotSpan span)
        {
            // create a list to hold the results
            var classifications = new List<ClassificationSpan>();

            var insideMultiLineComment = false;
            // Scan all known multi-line comments to check if incoming span intersects
            for (int i = multiLineCommentTokens.Count - 1; i >= 0; i--) {
                var multilineSpan = multiLineCommentTokens[i].Tracking().GetSpan(span.Snapshot);
                if (multilineSpan.Length != 0) {
                    if (span.IntersectsWith(multilineSpan)) {
                        // Check if multi-line comment is changed
                        if (span.Snapshot.Version != multiLineCommentTokens[i].Version()) {
                            // re-classify multi-line span
                            multiLineCommentTokens.RemoveAt(i);
                            Invalidate(multilineSpan);
                        } else {
                            insideMultiLineComment = true;
                            // re-classify multi-line span using current classification
                            classifications.Add(new ClassificationSpan(multilineSpan,
                                multiLineCommentTokens[i].Classification()));
                        }
                    }
                } else {
                    multiLineCommentTokens.RemoveAt(i);
                }
            }

            if (!insideMultiLineComment) {
                var start = 0;
                var end = 0;
                var offset = 0;
                var text = span.GetText();

                Token token = null;
                do {
                    start = span.Start.Position + offset;
                    end = start;

                    token = Scan(text, offset, text.Length);
                    if (token != null) {
                        end = start + token.Length;
                        // If token not ending in the current span, continue reading text
                        // until the whole token is read
                        while (end < span.Snapshot.Length && token.State != 0 && token != null) {
                            var bufferSize = Math.Min(span.Snapshot.Length - end, 2048);
                            text = span.Snapshot.GetText(end, bufferSize);
                            // Scan next token, continuing from previous
                            token = Scan(text, 0, text.Length, token.Type, token.State);
                            if (token != null)
                                end += token.Length;
                        }
                        // Add new classification
                        var tokenSpan = new SnapshotSpan(span.Snapshot, start, (end - start));
                        var classification = GetClassificationType(token);
                        classifications.Add(new ClassificationSpan(tokenSpan, classification));

                        // If we have multi-line comment in out hands add it into the list
                        if (token.Type == TokenType.MultilineComment) {
                            bool alreadyFound = false;
                            foreach (var mlToken in multiLineCommentTokens) {
                                if (mlToken.Tracking().GetSpan(span.Snapshot).Span == tokenSpan.Span) {
                                    alreadyFound = true;
                                    break;
                                }
                            }
                            if (!alreadyFound) {
                                multiLineCommentTokens.Add(new MultilineCommentToken(classification,
                                    span.Snapshot.CreateTrackingSpan(tokenSpan.Span,
                                    SpanTrackingMode.EdgeExclusive), span.Snapshot.Version));
                                // If token text longer than current span do re-classify
                                if (tokenSpan.End > span.End)
                                    Invalidate(new SnapshotSpan(span.End + 1, tokenSpan.End));
                            }
                        }
                        offset += (end - start);
                    }
                }
                while (token != null && offset < text.Length);
            }
            return classifications;
        }

        private IClassificationType GetClassificationType(Token token)
        {
            var classifierType = string.Empty;
            switch (token.Type) {
            case TokenType.MultilineComment:
            case TokenType.Comment:
                classifierType = ClassifierTypes.Comment;
                break;
            case TokenType.Keyword:
                classifierType = ClassifierTypes.Keyword;
                break;
            case TokenType.JsKeyword:
                classifierType = ClassifierTypes.JsKeyword;
                break;
            case TokenType.Type:
                classifierType = ClassifierTypes.Type;
                break;
            case TokenType.String:
                classifierType = ClassifierTypes.String;
                break;
            case TokenType.Other:
                classifierType = ClassifierTypes.Other;
                break;
            case TokenType.Property:
                classifierType = ClassifierTypes.Property;
                break;
            }
            return classificationRegistryService.GetClassificationType(classifierType);
        }
    }
}
