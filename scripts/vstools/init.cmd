:: Copyright (C) 2025 The Qt Company Ltd.
:: SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::init.cmd
:: * Build pre-requisites before calling the main build
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

IF "%BUILD_PLATFORM%" == "" (
    SET BUILD_PLATFORM=%VSCMD_ARG_TGT_ARCH%
)

ECHO.
%##########################%
%##% %BOLD%Building pre-requisites...%RESET%
IF %VERBOSE% (
    %##%  msbuild: %VSTOOLS_SOLUTION%
    %##%  msbuild: -t:%DEPENDENCIES%
    %##%  msbuild: -p:Configuration=%BUILD_CONFIGURATION%
    %##%  msbuild: -p:Platform=%BUILD_PLATFORM%
    %##%  msbuild: -p:TransformOutOfDateOnly=false
    %##%  msbuild extras: %MSBUILD_EXTRAS%
)
%##########################%
IF NOT %VERBOSE% ECHO %DARK_GRAY%
msbuild ^
    -nologo ^
    -verbosity:%MSBUILD_VERBOSITY% ^
    -maxCpuCount ^
    -t:%DEPENDENCIES% ^
    -p:Configuration=%BUILD_CONFIGURATION% ^
    -p:Platform=%BUILD_PLATFORM% ^
    -p:TransformOutOfDateOnly=false ^
    %MSBUILD_EXTRAS% ^
    %VSTOOLS_SOLUTION%
ECHO %RESET%
IF %ERRORLEVEL% NEQ 0 (
    CALL %SCRIPTLIB%\error.cmd %ERRORLEVEL% "ERROR building pre-requisites"
    EXIT /B %ERRORLEVEL%
)
