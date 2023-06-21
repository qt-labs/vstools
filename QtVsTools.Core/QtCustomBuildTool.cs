/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core
{
    using MsBuild;

    internal class QtCustomBuildTool
    {
        private readonly QtMsBuildContainer qtMsBuild;
        private readonly VCFileConfiguration vcConfig;
        private readonly VCCustomBuildTool customTool;

        private enum FileItemType { Other = 0, CustomBuild, QtMoc, QtRcc, QtRepc, QtUic }
        private readonly FileItemType itemType = FileItemType.Other;

        public QtCustomBuildTool(VCFileConfiguration config, QtMsBuildContainer container = null)
        {
            vcConfig = config;
            qtMsBuild = container ?? new QtMsBuildContainer(new VcPropertyStorageProvider());

            if (vcConfig?.File is VCFile vcFile) {
                itemType = vcFile.ItemType switch
                {
                    "CustomBuild" => FileItemType.CustomBuild,
                    QtMoc.ItemTypeName => FileItemType.QtMoc,
                    QtRcc.ItemTypeName => FileItemType.QtRcc,
                    QtRepc.ItemTypeName => FileItemType.QtRepc,
                    QtUic.ItemTypeName => FileItemType.QtUic,
                    _ => itemType
                };
            }
            if (itemType == FileItemType.CustomBuild)
                customTool = HelperFunctions.GetCustomBuildTool(vcConfig);
        }

        public string CommandLine => itemType switch
        {
            FileItemType.CustomBuild => customTool?.CommandLine ?? "",
            FileItemType.QtMoc => qtMsBuild.GenerateQtMocCommandLine(vcConfig),
            FileItemType.QtRcc => qtMsBuild.GenerateQtRccCommandLine(vcConfig),
            FileItemType.QtRepc => qtMsBuild.GenerateQtRepcCommandLine(vcConfig),
            FileItemType.QtUic => qtMsBuild.GenerateQtUicCommandLine(vcConfig),
            _ => ""
        };

        public string Outputs => itemType switch
        {
            FileItemType.CustomBuild => customTool?.Outputs ?? "",
            FileItemType.QtMoc => qtMsBuild.GetPropertyValue(vcConfig, QtMoc.Property.OutputFile),
            FileItemType.QtRcc => qtMsBuild.GetPropertyValue(vcConfig, QtRcc.Property.OutputFile),
            FileItemType.QtRepc => qtMsBuild.GetPropertyValue(vcConfig, QtRepc.Property.OutputFile),
            FileItemType.QtUic => qtMsBuild.GetPropertyValue(vcConfig, QtUic.Property.OutputFile),
            _ => ""
        };
    }
}
