/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
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
