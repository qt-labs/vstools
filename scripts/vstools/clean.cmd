::**************************************************************************************************
::Copyright (C) 2024 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::clean.cmd
:: * Deletes all build-time outputs (removes all 'bin' and 'obj' directories)
:: * Generates version log
:: * Restores NuGet packages
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

IF "%BUILD_PLATFORM%" == "" (
    SET BUILD_PLATFORM=%VSCMD_ARG_TGT_ARCH%
)

ECHO.
%##########################%
%##% %BOLD%Deleting output files...%RESET%
%##########################%
RD /S /Q bin > NUL 2>&1
FOR /F %ALL% %%d IN (`DIR /A:D /B /S bin 2^> NUL`) DO (
    RD /S /Q %%d > NUL 2>&1
)
RD /S /Q obj > NUL 2>&1
FOR /F %ALL% %%d IN (`DIR /A:D /B /S obj 2^> NUL`) DO (
    RD /S /Q %%d > NUL 2>&1
)

CALL %SCRIPTLIB%\log_version.cmd

ECHO.
%##########################%
%##% %BOLD%Restoring packages...%RESET%
IF %VERBOSE% (
    %##%  msbuild: vstools.sln
    %##%  msbuild: -t:Restore
    %##%  msbuild: -p:Configuration=%BUILD_CONFIGURATION%
    %##%  msbuild: -p:Platform=%BUILD_PLATFORM%
    %##%  msbuild extras: %MSBUILD_EXTRAS%
)
%##########################%
IF NOT %VERBOSE% ECHO %DARK_GRAY%
msbuild ^
    -nologo ^
    -verbosity:%MSBUILD_VERBOSITY% ^
    -t:Restore ^
    -p:Configuration=%BUILD_CONFIGURATION% ^
    -p:Platform=%BUILD_PLATFORM% ^
    %MSBUILD_EXTRAS% ^
    vstools.sln
ECHO %RESET%
IF %ERRORLEVEL% NEQ 0 (
    CALL %SCRIPTLIB%\error.cmd %ERRORLEVEL% "ERROR restoring packages!"
    EXIT /B %ERRORLEVEL%
)
