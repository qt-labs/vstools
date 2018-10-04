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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace QtVsTools.Qml.Debug
{
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

        private FileSystem()
        { }

        public IEnumerable<string> QrcPaths
        {
            get
            {
                return files.Values
                    .GroupBy(x => x.QrcPath)
                    .Select(x => x.Key);
            }
        }

        string QrcPath(string prefix, string filePath)
        {
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("/"))
                prefix += "/";

            while (!string.IsNullOrEmpty(prefix) && prefix[0] == '/')
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
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(
                    e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
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
                        Path = ((string)y)
                            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                    })
                );

            foreach (var file in files) {
                if (!Path.GetExtension(file.Path)
                    .Equals(".qml", StringComparison.InvariantCultureIgnoreCase)) {
                    continue;
                }

                string qrcPath;
                if (file.Alias != null)
                    qrcPath = (string)file.Alias;
                else if (!Path.IsPathRooted(file.Path))
                    qrcPath = file.Path.Replace(@"\", "/");
                else
                    continue;

                string qrcPathPrefix = (file.Prefix != null) ? ((string)file.Prefix) : "";
                if (!string.IsNullOrEmpty(qrcPathPrefix) && !qrcPathPrefix.EndsWith("/"))
                    qrcPathPrefix += "/";

                while (!string.IsNullOrEmpty(qrcPathPrefix) && qrcPathPrefix[0] == '/')
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

            while (!string.IsNullOrEmpty(qrcPath) && qrcPath[0] == '/')
                qrcPath = qrcPath.Substring(1);

            qrcPath = string.Format("qrc:///{0}", qrcPath);

            QmlFile file;
            if (!files.TryGetValue(qrcPath, out file))
                return default(QmlFile);

            return file;
        }

        QmlFile FromFileUrl(string fileUrl)
        {
            string filePath = fileUrl.Substring("file://".Length);

            while (!string.IsNullOrEmpty(filePath) && filePath[0] == '/')
                filePath = filePath.Substring(1);

            if (!File.Exists(filePath))
                return default(QmlFile);

            return new QmlFile
            {
                QrcPath = fileUrl,
                FilePath = filePath
                    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
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

            QmlFile file;
            if (!files.TryGetValue(fullPath, out file))
                return default(QmlFile);

            return file;
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
