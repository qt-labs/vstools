/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace QtVsTools.Core
{
    /// <summary>
    /// Summary description for Resources.
    /// </summary>
    public static class Resources
    {
        // Old Qt VS project tag
        public const string qtProjectV2Keyword = "Qt4VS";

        // Qt VS project tag and format version
        public const string qtProjectKeyword = "QtVS";
        public const int qtProjectFormatVersion = 304;
        public static string QtVSVersionTag
            => $"{qtProjectKeyword}_v{qtProjectFormatVersion}";

        // Min. format version for Qt settings as project properties
        public const int qtMinFormatVersion_Settings = 300;

        // Min. format version for shared compiler properties
        public const int qtMinFormatVersion_ClProperties = 300;

        // Min. format version for global QtMsBuild property
        public const int qtMinFormatVersion_GlobalQtMsBuildProperty = 302;

        // Min. format version for correct ordering of property evaluation (QTVSADDINBUG-787)
        public const int qtMinFormatVersion_PropertyEval = 303;

        // Project properties labels
        public const string projLabelQtSettings = "QtSettings";

        // If those directories do not equal to the project directory
        // they have to be added to the include directories for
        // compiling!
        public const string generatedFilesDir = "GeneratedFiles";

        public const string mocDirKeyword = "MocDir";
        public const string uicDirKeyword = "UicDir";
        public const string rccDirKeyword = "RccDir";

        public const string registryRootPath = "Digia";

#if (VS2017 || VS2019 || VS2022)
        public const string registryPackagePath = registryRootPath + "\\Qt5VS2017";
#else
#error Unknown Visual Studio version!
#endif
        public const string registryVersionPath = registryRootPath + "\\Versions";
    }
}
