// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace QtVsTools.Package.QML.Language.Lsp
{
    using QtVsTools.Core.Common;

    internal static class MessageParser
    {
        private const string ContentLengthHeader = "Content-Length:";

        public static JObject Deserialize(StreamData data)
        {
            // Decode the provided portion of the byte array
            var headerAndBody = Encoding.UTF8.GetString(data.Bytes, data.Offset, data.Count);

            // Find the header-body separator
            var separatorIndex = headerAndBody.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (separatorIndex == -1)
                return null;

            // Extract header lines
            var header = headerAndBody.Substring(0, separatorIndex);
            var headerLines = header.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var contentLength = 0;
            foreach (var line in headerLines) {
                if (!line.StartsWith(ContentLengthHeader, Utils.IgnoreCase))
                    continue;
                var value = line.Substring(ContentLengthHeader.Length).Trim();
                if (!int.TryParse(value, out contentLength))
                    return null;
                break;
            }

            // If no valid content-length header was found, return early
            if (contentLength <= 0)
                return null;

            // The JSON body starts after the header separator
            var jsonStart = separatorIndex + 4;
            // Ensure we don't read past the available string length
            if (jsonStart + contentLength > headerAndBody.Length)
                contentLength = headerAndBody.Length - jsonStart;

            var json = headerAndBody.Substring(jsonStart, contentLength);
            try {
                return JObject.Parse(json);
            } catch {
                return null;
            }
        }

        public static StreamData Serialize(JObject message)
        {
            var json = message.ToString(Newtonsoft.Json.Formatting.None);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            // Build the header
            var header = $"{ContentLengthHeader} {jsonBytes.Length}\r\n\r\n";
            var headerBytes = Encoding.UTF8.GetBytes(header);

            // Allocate a new buffer for the header plus JSON body
            var fullBuffer = new byte[headerBytes.Length + jsonBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, fullBuffer, 0, headerBytes.Length);
            Buffer.BlockCopy(jsonBytes, 0, fullBuffer, headerBytes.Length, jsonBytes.Length);

            return new StreamData { Bytes = fullBuffer, Offset = 0, Count = fullBuffer.Length };
        }
    }
}
