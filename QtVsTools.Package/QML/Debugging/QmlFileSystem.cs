/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace QtVsTools.Qml.Debug
{
    using Core;
    using static Core.Common.Utils;

    internal class FileSystem : Concurrent
    {
        private Dictionary<string, string> qrcToLocalFileMap;

        public static FileSystem Create()
        {
            return new FileSystem
            {
                qrcToLocalFileMap = new Dictionary<string, string>()
            };
        }

        private static readonly string[] KnownExtensions = { ".qml", ".js" };

        private FileSystem()
        { }

        public void RegisterRccFile(string rccFilePath)
        {
            XDocument rccXml;
            try {
                var xmlText = File.ReadAllText(rccFilePath, Encoding.UTF8);
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore
                };
                using var reader = XmlReader.Create(new StringReader(xmlText), settings);
                rccXml = XDocument.Load(reader);
            } catch (Exception exception) {
                exception.Log();
                return;
            }

            var files = rccXml
                .Elements("RCC")
                .Elements("qresource")
                .SelectMany(x => x.Elements("file")
                    .Select(y => new
                    {
                        Prefix = x.Attribute("prefix"),
                        Alias = y.Attribute("alias"),
                        Path = HelperFunctions.ToNativeSeparator((string)y)
                    })
                    .Where(z => KnownExtensions.Contains(Path.GetExtension(z.Path), CaseIgnorer)));

            var rccFileDir = Path.GetDirectoryName(rccFilePath);
            foreach (var file in files) {
                string qrcPath;
                if (file.Alias != null)
                    qrcPath = (string)file.Alias;
                else if (!Path.IsPathRooted(file.Path))
                    qrcPath = HelperFunctions.FromNativeSeparators(file.Path);
                else
                    continue;

                var qrcPathPrefix = file.Prefix != null ? (string)file.Prefix : "";
                if (!string.IsNullOrEmpty(qrcPathPrefix) && !qrcPathPrefix.EndsWith("/"))
                    qrcPathPrefix += Path.AltDirectorySeparatorChar;
                qrcPathPrefix = RemoveLeadingAltDirectorySeparators(qrcPathPrefix);
                qrcToLocalFileMap[$"qrc:///{qrcPathPrefix}{qrcPath}"] =
                    HelperFunctions.ToNativeSeparator(Path.Combine(rccFileDir!, file.Path));
            }
        }

        public string ToFilePath(string path)
        {
            if (path.StartsWith("qrc:", IgnoreCase))
                return FromQrcPath(path);
            if (path.StartsWith("file:", IgnoreCase))
                return FromFileUrl(path);
            return FromFilePath(path);
        }

        private string FromQrcPath(string qrcPath)
        {
            // Normalize qrc path:
            //  - Only pre-condition is that qrcPath have a "qrc:" prefix
            //  - It might have any number of '/' after that, or none at all
            //  - A "qrc:///" prefix is required to match the mapping key
            //  - to enforce this, the "qrc:" prefix is removed, as well as any leading '/'
            //  - then the "normalized" prefix "qrc:///" is added
            if (!qrcPath.StartsWith("qrc:"))
                return default;
            qrcPath = RemoveLeadingAltDirectorySeparators(qrcPath.Substring("qrc:".Length));
            qrcPath = $"qrc:///{qrcPath}";
            return qrcToLocalFileMap.TryGetValue(qrcPath, out var filePath) ? filePath : default;
        }

        private static string FromFileUrl(string fileUrl)
        {
            var path = RemoveLeadingAltDirectorySeparators(fileUrl.Substring("file://".Length));
            return File.Exists(path) ? HelperFunctions.ToNativeSeparator(path) : default;
        }

        private static string FromFilePath(string filePath)
        {
            try {
                var fullPath = Path.GetFullPath(filePath);
                return File.Exists(fullPath) ? new Uri(fullPath).AbsoluteUri : default;
            } catch {
                return default;
            }
        }

        private static string RemoveLeadingAltDirectorySeparators(string path)
        {
            while (!string.IsNullOrEmpty(path) && path[0] == Path.AltDirectorySeparatorChar)
                path = path.Substring(1);
            return path;
        }
    }
}
