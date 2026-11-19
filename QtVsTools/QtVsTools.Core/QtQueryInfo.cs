// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

namespace QtVsTools.Core
{
    /// <summary>
    /// Snapshot of qmake/qtpaths query values validated for required fields.
    /// </summary>
    internal sealed class QtQueryInfo
    {
        private readonly QtBuildToolQuery query;

        public string InstallPrefix { get; }
        public string InstallArchData { get; }
        public string QMakeXSpec { get; }
        public string QtVersion { get; }
        public string LibExecs { get; }
        public string InstallDocs { get; }
        public string InstallPrefixSrc { get; }
        public string InstallArchDataSrc { get; }

        public string GetValue(string name) => query[name];

        /// <summary>
        /// Tries to build a validated snapshot from the Qt install directory.
        /// </summary>
        public static bool TryCreate(string qtDir, out QtQueryInfo info)
        {
            info = null;
            var query = QtBuildToolQuery.Get(qtDir);
            if (query == null)
                return false;

            var installPrefix = query["QT_INSTALL_PREFIX"];
            var installArchData = query["QT_INSTALL_ARCHDATA"];
            var qmakeXSpec = query["QMAKE_XSPEC"];
            if (string.IsNullOrEmpty(installPrefix)
                || string.IsNullOrEmpty(installArchData)
                || string.IsNullOrEmpty(qmakeXSpec)) {
                return false;
            }

            info = new QtQueryInfo(
                query,
                installPrefix,
                installArchData,
                qmakeXSpec,
                query["QT_VERSION"],
                query["QT_INSTALL_LIBEXECS"],
                query["QT_INSTALL_DOCS"],
                query["QT_INSTALL_PREFIX/src"],
                query["QT_INSTALL_ARCHDATA/src"]);
            return true;
        }

        private QtQueryInfo(QtBuildToolQuery query, string installPrefix,
            string installArchData, string qmakeXSpec, string qtVersion, string libExecs,
            string installDocs, string installPrefixSrc, string installArchDataSrc)
        {
            this.query = query;
            InstallPrefix = installPrefix;
            InstallArchData = installArchData;
            QMakeXSpec = qmakeXSpec;
            QtVersion = qtVersion;
            LibExecs = libExecs;
            InstallDocs = installDocs;
            InstallPrefixSrc = installPrefixSrc;
            InstallArchDataSrc = installArchDataSrc;
        }
    }
}
