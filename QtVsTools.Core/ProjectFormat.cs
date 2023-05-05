// ************************************************************************************************
// Copyright (C) 2023 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
// ************************************************************************************************

using System;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core
{
    public static class ProjectFormat
    {
        /// <summary>
        /// An enumeration containing the version numbers used during the Qt VS Tools development
        /// cycle.
        /// </summary>
        public enum Version
        {
            /// <summary>
            /// The version number cannot be read or is out of range.
            /// </summary>
            Unknown = 0,
            /// <summary>
            /// Deprecated, do not use.
            /// </summary>
            V1 = 100,
            /// <summary>
            /// Deprecated, do not use.
            /// </summary>
            V2 = 200,
            /// <summary>
            /// Minimum format version for Qt settings as project properties.
            /// </summary>
            V3 = 300,
            /// <summary>
            /// Minimum format version for shared compiler properties
            /// </summary>
            V3ClProperties = 300,
            /// <summary>
            /// Minimum format version for global QtMsBuild property.
            /// </summary>
            V3GlobalQtMsBuildProperty = 302,
            /// <summary>
            /// Minimum format version for correct ordering of property evaluation. (QTVSADDINBUG-787)
            /// </summary>
            V3PropertyEval = 303,
            /// <summary>
            /// Latest version of Qt VS Tools, also used as right part of the version tag.
            /// <para>See also: <seealso cref="QtVsVersionTag"/> </para>
            /// </summary>
            Latest = 304
        }

        // Old Qt VS project tag
        public const string KeywordV2 = "Qt4VS";

        /// <summary>
        /// The latest left part of the Qt VS Tools version tag.
        /// </summary>
        public const string KeywordLatest = "QtVS";

        /// <summary>
        /// Qt VS tool version tag used as 'Keyword' inside project files.
        /// Combination of latest keyword and format version.
        /// <para>See also:
        /// <seealso cref="KeywordLatest"/> and <seealso cref="Version.Latest"/>
        /// </para>
        /// </summary>
        public static readonly string QtVsVersionTag = $"{KeywordLatest}_v{(int)Version.Latest}";

        /// <summary>
        /// Tries to retrieve the format version from the project.
        /// <para>Attempts to find the format version based on the following conditions:</para>
        /// <example>
        /// <code>
        /// * The project is a C++ project.
        /// * The project contains a Qt specific attribute that starts with Qt4VS.
        /// * The project contains a Qt specific attribute that starts with QtVS.
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="project">The project to get the format version from.</param>
        /// <returns></returns>
        public static Version GetVersion(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var keyword = GetKeyword(project);
            if (string.IsNullOrEmpty(keyword))
                return Version.Unknown;

            if (keyword.StartsWith(KeywordV2, StringComparison.Ordinal)) {
                if (project is not { Globals: { VariableNames: string[] variables } })
                    return Version.V1;
                return variables.Any(var => HelperFunctions.HasQt5Version(var, project))
                    ? Version.V2 : Version.V1;
            }

            if (!keyword.StartsWith(KeywordLatest, StringComparison.Ordinal))
                return Version.Unknown;

            if (!int.TryParse(keyword.Substring(6), out var tmp))
                return Version.Unknown;
            return (Version)tmp is >= Version.V3 and <= Version.Latest ? (Version)tmp : Version.Unknown;
        }

        /// <inheritdoc cref="GetVersion(EnvDTE.Project)"/>>
        public static Version GetVersion(Microsoft.VisualStudio.VCProjectEngine.VCProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetVersion(project?.Object as EnvDTE.Project);
        }

        /// <summary>
        /// Tries to retrieve the attribute 'Keyword' from the project.
        /// </summary>
        /// <param name="project">The project to get the attribute from.</param>
        /// <returns>
        /// <see langword="null" /> if the attribute doesn't exist.
        /// </returns>
        private static string GetKeyword(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return project is { Kind: "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}" }
                ? (project.Object as VCProject)?.keyword : null;
        }
    }
}
