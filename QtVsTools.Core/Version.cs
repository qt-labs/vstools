/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
****************************************************************************************************
<#@output extension="tt.cs" #>
<#@include file="$(SolutionDir)\version.tt" #>
**              <#=WARNING_GENERATED_FILE#>
****************************************************************************
*/

namespace QtVsTools.Core
{
    public static class Version
    {
        public const string PRODUCT_VERSION = "<#=QT_VS_TOOLS_VERSION_MANIFEST#>";
        public const string USER_VERSION = "<#=QT_VS_TOOLS_VERSION_USER#>";
    }
}
