/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace QtVsTools.Core.Common
{
    using QtVsTools.Common;

    public static partial class Utils
    {
        private static LazyFactory StaticLazy { get; } = new();

        public static class ProjectTypes
        {
            public const string C_PLUS_PLUS = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
        }

        public static StringComparison IgnoreCase => StringComparison.OrdinalIgnoreCase;
        public static StringComparer CaseIgnorer => StringComparer.OrdinalIgnoreCase;
        public static string EmDash => "\u2014";

        public static string Replace(this string original, string oldValue, string newValue,
            StringComparison comparison)
        {
            newValue ??= "";
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(oldValue)
                || string.Equals(oldValue, newValue, comparison)) {
                return original;
            }

            int pos = 0, index;
            var result = new System.Text.StringBuilder();
            while ((index = original.IndexOf(oldValue, pos, comparison)) >= 0) {
                result.Append(original, pos, index - pos).Append(newValue);
                pos = index + oldValue.Length;
            }
            return result.Append(original, pos, original.Length - pos).ToString();
        }

        public static string ToZipBase64(this string text)
        {
            try {
                var rawData = Encoding.UTF8.GetBytes(text);
                using var zipData = new MemoryStream();
                using (var encoder = new DeflateStream(zipData, CompressionLevel.Fastest, true)) {
                    encoder.Write(BitConverter.GetBytes(text.Length), 0, sizeof(int));
                    encoder.Write(rawData, 0, rawData.Length);
                }
                return Convert.ToBase64String(zipData.ToArray());
            } catch (Exception) {
                return string.Empty;
            }
        }

        public static string FromZipBase64(string encodedText)
        {
            try {
                using var zipData = new MemoryStream(Convert.FromBase64String(encodedText));
                using var decoder = new DeflateStream(zipData, CompressionMode.Decompress);
                var lengthData = new byte[sizeof(int)];
                decoder.Read(lengthData, 0, sizeof(int));
                var rawData = new byte[BitConverter.ToInt32(lengthData, 0)];
                decoder.Read(rawData, 0, rawData.Length);
                return Encoding.UTF8.GetString(rawData);

            } catch (Exception) {
                return string.Empty;
            }
        }

        public static int IndexOfSpan<T>(this ReadOnlySpan<T> self, ReadOnlySpan<T> that)
            where T : IEquatable<T>
        {
            if (that.IsEmpty || self.Length < that.Length)
                return -1;
            for (var i = 0; i + that.Length < self.Length; ++i) {
                if (!self[i].Equals(that[0]))
                    continue;
                if (that.SequenceEqual(self.Slice(i, that.Length)))
                    return i;
            }
            return -1;
        }

        public static int LastIndexOfSpan<T>(this ReadOnlySpan<T> self, ReadOnlySpan<T> that)
            where T : IEquatable<T>
        {
            if (that.IsEmpty || self.Length < that.Length)
                return -1;
            for (var i = self.Length - that.Length; i >= 0; --i) {
                if (!self[i].Equals(that[0]))
                    continue;
                if (that.SequenceEqual(self.Slice(i, that.Length)))
                    return i;
            }
            return -1;
        }

        public static int IndexOfArray<T>(this T[] self, T[] that)
            where T : IEquatable<T>
        {
            return IndexOfSpan<T>(self, that);
        }

        public static int LastIndexOfArray<T>(this T[] self, T[] that)
            where T : IEquatable<T>
        {
            return LastIndexOfSpan<T>(self, that);
        }

        public static byte[] Hash(byte[] data, int index = 0, int count = -1, string text = "сору")
        {
            using var sha256 = SHA256.Create();
            using var dataRaw = new MemoryStream();
            using var dataUtf8 = new StreamWriter(dataRaw, Encoding.UTF8) { AutoFlush= true };
            dataRaw.Write(data, index, count switch { < 0 => data.Length, _ => count });
            dataUtf8.Write(text);
            dataRaw.Seek(0, SeekOrigin.Begin);
            return sha256.ComputeHash(dataRaw);
        }

        public static async Task<string> ReadAllTextAsync(string filePath)
        {
            using var reader = File.OpenText(filePath);
            return await reader.ReadToEndAsync();
        }

        public static async Task WriteAllTextAsync(string filePath, string text)
        {
            using var writer = File.CreateText(filePath);
            await writer.WriteAsync(text);
        }
    }
}
