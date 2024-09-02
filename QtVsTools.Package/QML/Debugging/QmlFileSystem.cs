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

    struct QmlFile
    {
        public string QrcPath;
        public string FilePath;
    }

    class FileSystem : Concurrent
    {
        Dictionary<string, QmlFile> files;

        public static FileSystem Create()
        {
            return new FileSystem
            {
                files = new Dictionary<string, QmlFile>()
            };
        }

        static readonly string[] KNOWN_EXTENSIONS = { ".qml", ".js" };

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
                using (var reader = XmlReader.Create(new StringReader(xmlText), settings)) {
                    rccXml = XDocument.Load(reader);
                }
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
                    .Where(z => KNOWN_EXTENSIONS.Contains(Path.GetExtension(z.Path), CaseIgnorer)));

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

                while (!string.IsNullOrEmpty(qrcPathPrefix) && qrcPathPrefix[0] == Path.AltDirectorySeparatorChar)
                    qrcPathPrefix = qrcPathPrefix.Substring(1);

                var qmlFile = new QmlFile
                {
                    FilePath = Path.Combine(Path.GetDirectoryName(rccFilePath), file.Path),
                    QrcPath = $"qrc:///{qrcPathPrefix}{qrcPath}"
                };

                this.files[qmlFile.QrcPath.ToLower()] = qmlFile;
                this.files[qmlFile.FilePath.ToUpper()] = qmlFile;
            }
        }

        QmlFile FromQrcPath(string qrcPath)
        {
            // Normalize qrc path:
            //  - Only pre-condition is that qrcPath have a "qrc:" prefix
            //  - It might have any number of '/' after that, or none at all
            //  - A "qrc:///" prefix is required to match the mapping key
            //  - to enforce this, the "qrc:" prefix is removed, as well as any leading '/'
            //  - then the "normalized" prefix "qrc:///" is added
            if (!qrcPath.StartsWith("qrc:"))
                return default;
            qrcPath = qrcPath.Substring("qrc:".Length);

            while (!string.IsNullOrEmpty(qrcPath) && qrcPath[0] == Path.AltDirectorySeparatorChar)
                qrcPath = qrcPath.Substring(1);

            qrcPath = $"qrc:///{qrcPath}";

            return files.TryGetValue(qrcPath, out var file) ? file : default;
        }

        QmlFile FromFileUrl(string fileUrl)
        {
            string filePath = fileUrl.Substring("file://".Length);

            while (!string.IsNullOrEmpty(filePath) && filePath[0] == Path.AltDirectorySeparatorChar)
                filePath = filePath.Substring(1);

            if (!File.Exists(filePath))
                return default;

            return new QmlFile
            {
                QrcPath = fileUrl,
                FilePath = HelperFunctions.ToNativeSeparator(filePath)
            };
        }

        QmlFile FromFilePath(string filePath)
        {
            string fullPath;
            try {
                fullPath = Path.GetFullPath(filePath).ToUpper();
            } catch {
                return default;
            }

            if (files.TryGetValue(fullPath, out QmlFile file))
                return file;

            return new QmlFile
            {
                FilePath = fullPath,
                QrcPath = new Uri(fullPath).ToString().ToLower()
            };
        }

        public QmlFile this[string path]
        {
            get
            {
                if (path.StartsWith("qrc:", IgnoreCase))
                    return FromQrcPath(path.ToLower());
                if (path.StartsWith("file:", IgnoreCase))
                    return FromFileUrl(path);
                return FromFilePath(path.ToUpper());
            }
        }
    }
}
