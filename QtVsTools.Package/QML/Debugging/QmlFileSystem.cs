/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
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

        static readonly string[] KNOWN_EXTENSIONS = new string[] { ".qml", ".js" };

        private FileSystem()
        { }

        public IEnumerable<string> QrcPaths => files.Values.GroupBy(x => x.QrcPath).Select(x => x.Key);

        string QrcPath(string prefix, string filePath)
        {
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("/"))
                prefix += Path.AltDirectorySeparatorChar;

            while (!string.IsNullOrEmpty(prefix) && prefix[0] == Path.AltDirectorySeparatorChar)
                prefix = prefix.Substring(1);

            return string.Format("qrc:///{0}{1}", prefix, filePath);
        }

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
                    .Where(z => KNOWN_EXTENSIONS.Contains(
                        Path.GetExtension(z.Path), StringComparer.InvariantCultureIgnoreCase)));;

            foreach (var file in files) {
                string qrcPath;
                if (file.Alias != null)
                    qrcPath = (string)file.Alias;
                else if (!Path.IsPathRooted(file.Path))
                    qrcPath = HelperFunctions.FromNativeSeparators(file.Path);
                else
                    continue;

                string qrcPathPrefix = (file.Prefix != null) ? ((string)file.Prefix) : "";
                if (!string.IsNullOrEmpty(qrcPathPrefix) && !qrcPathPrefix.EndsWith("/"))
                    qrcPathPrefix += Path.AltDirectorySeparatorChar;

                while (!string.IsNullOrEmpty(qrcPathPrefix) && qrcPathPrefix[0] == Path.AltDirectorySeparatorChar)
                    qrcPathPrefix = qrcPathPrefix.Substring(1);

                var qmlFile = new QmlFile
                {
                    FilePath = Path.Combine(Path.GetDirectoryName(rccFilePath), file.Path),
                    QrcPath = string.Format("qrc:///{0}{1}", qrcPathPrefix, qrcPath)
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
                return default(QmlFile);
            qrcPath = qrcPath.Substring("qrc:".Length);

            while (!string.IsNullOrEmpty(qrcPath) && qrcPath[0] == Path.AltDirectorySeparatorChar)
                qrcPath = qrcPath.Substring(1);

            qrcPath = string.Format("qrc:///{0}", qrcPath);

            if (!files.TryGetValue(qrcPath, out QmlFile file))
                return default(QmlFile);

            return file;
        }

        QmlFile FromFileUrl(string fileUrl)
        {
            string filePath = fileUrl.Substring("file://".Length);

            while (!string.IsNullOrEmpty(filePath) && filePath[0] == Path.AltDirectorySeparatorChar)
                filePath = filePath.Substring(1);

            if (!File.Exists(filePath))
                return default(QmlFile);

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
                return default(QmlFile);
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
                if (path.StartsWith("qrc:", StringComparison.InvariantCultureIgnoreCase))
                    return FromQrcPath(path.ToLower());
                else if (path.StartsWith("file:", StringComparison.InvariantCultureIgnoreCase))
                    return FromFileUrl(path);
                else
                    return FromFilePath(path.ToUpper());
            }
        }
    }
}
