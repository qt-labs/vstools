::**************************************************************************************************
::Copyright (C) 2024 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::globals.cmd
:: * Global definitions
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

SET ROOT=%CD%
SET TRUE="0"=="0"
SET FALSE="0"=="1"
SET ALL="tokens=* usebackq"
SET VSWHERE_EXE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
SET VSWHERE=%VSWHERE_EXE%
SET VSWHERE=%VSWHERE:(=^(%
SET VSWHERE=%VSWHERE:)=^)%
SET QUERY_LATEST=-latest -prerelease
SET QUERY_ALL=-prerelease
SET QUERY=%QUERY_LATEST%
SET VSWHERE_MAJOR=2
SET VSWHERE_MINOR=7
SET VSWHERE_PATCH=1
SET VSWHERE_VERSION=%VSWHERE_MAJOR%.%VSWHERE_MINOR%.%VSWHERE_PATCH%

SET DEPENDENCIES=QtVsTools_RegExpr;QtMsBuild:TransformAll

SET VS2022="-version [17.0^,18.0^)"
SET VS2019="-version [16.0^,17.0^)"
SET VS_ALL=%VS2022%,%VS2019%
SET VS_LATEST="-all"

SET VS_VERSIONS=
SET VS_VERSIONS_DEFAULT=%TRUE%
SET VERBOSE=%FALSE%
SET REBUILD=%FALSE%
SET INIT=%FALSE%
SET CLEAN=%FALSE%
SET BINARYLOG=%FALSE%
SET BUILD_CONFIGURATION=Release
SET DO_INSTALL=%FALSE%
SET VSIX_INSTALLER_ARG=
SET DEPLOY=%FALSE%
SET AUTOTEST=%FALSE%
SET TRANSFORM_INCREMENTAL=true
SET START_VS=%FALSE%
SET START_CMD=%FALSE%
SET LIST_VERSIONS=%FALSE%

SET FLAG_VS2022=-vs2022
SET FLAG_VS2019=-vs2019

CALL %SCRIPTLIB%\banner.cmd
CALL %SCRIPTLIB%\console.cmd
