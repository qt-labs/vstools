/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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
            => string.Format("{0}_v{1}", qtProjectKeyword, qtProjectFormatVersion);

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

        public const string uic4Command = "$(QTDIR)\\bin\\uic.exe";

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
