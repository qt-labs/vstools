// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace QtVsTools.QrcEditor
{
    /// <summary>
    /// File name extensions and the kinds of files they denote.
    /// </summary>
    internal static class FileKinds
    {
        public static readonly HashSet<string> Images = Set(
            ".bmp", ".gif", ".ico", ".jpeg", ".jpg", ".png", ".svg", ".svgz", ".tif", ".tiff",
            ".webp", ".pbm", ".pgm", ".ppm", ".xbm", ".xpm");

        // Images that WPF can decode for a preview.
        public static readonly HashSet<string> Previewable = Set(
            ".bmp", ".gif", ".ico", ".jpeg", ".jpg", ".png", ".tif", ".tiff");

        public static readonly HashSet<string> Code = Set(
            ".qml", ".js", ".mjs", ".json", ".frag", ".vert", ".glsl", ".comp", ".geom",
            ".tesc", ".tese", ".qsb", ".hlsl", ".py", ".lua");

        public static readonly HashSet<string> Text = Set(
            ".txt", ".md", ".xml", ".html", ".htm", ".css", ".qss", ".ui", ".ini", ".conf",
            ".csv", ".ts", ".qmltypes", ".qmlproject", ".yaml", ".yml", ".log");

        public static readonly HashSet<string> Fonts = Set(".ttf", ".otf", ".ttc", ".woff", ".woff2");

        public static readonly HashSet<string> Translations = Set(".qm");

        private static HashSet<string> Set(params string[] extensions)
        {
            return new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Converts the path of an image file into a small preview image; returns null for other
    /// files, or if the image cannot be read.
    /// </summary>
    public sealed class FilePreviewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (value is not string path
                || !FileKinds.Previewable.Contains(Path.GetExtension(path))
                || !File.Exists(path)) {
                return null;
            }

            try {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.DecodePixelWidth = 16;
                image.CacheOption = BitmapCacheOption.OnLoad; // do not keep the file open
                image.EndInit();
                image.Freeze();
                return image;
            } catch (Exception exception) when (exception is IOException or FormatException
                or NotSupportedException or UnauthorizedAccessException
                or ArgumentException or InvalidOperationException
                or System.Runtime.InteropServices.COMException) {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts the path of a file into a symbol of the Segoe MDL2 Assets font, shown when no
    /// preview image is available.
    /// </summary>
    public sealed class FileGlyphConverter : IValueConverter
    {
        private const string Code = "\uE943";
        private const string Document = "\uE8A5";
        private const string Folder = "\uF12B";
        private const string Picture = "\uEB9F";
        private const string Font = "\uE8D2";
        private const string Globe = "\uE774";
        private const string Binary = "\uE7B8"; // package; the font has no binary file symbol

        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (value is not string path)
                return Binary;

            // A qmldir file describes the QML module of the directory it is in.
            if (string.Equals(Path.GetFileName(path), "qmldir", StringComparison.OrdinalIgnoreCase))
                return Folder;

            var extension = Path.GetExtension(path);
            var kinds = new (HashSet<string> Extensions, string Glyph)[] {
                (FileKinds.Images, Picture),
                (FileKinds.Code, Code),
                (FileKinds.Text, Document),
                (FileKinds.Fonts, Font),
                (FileKinds.Translations, Globe)
            };
            return kinds.FirstOrDefault(x => x.Extensions.Contains(extension)).Glyph ?? Binary;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
