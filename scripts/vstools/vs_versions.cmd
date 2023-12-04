::**************************************************************************************************
::Copyright (C) 2023 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::vs_versions.cmd
:: * Loops through all selected VS versions
:: * Invokes requested operations for each version
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

IF %VERBOSE% %##% VS versions: %VS_VERSIONS%

SETLOCAL
SET ERRORCODE=0
SET BREAKLOOP=%FALSE%
SET EMPTY_LOOP=%TRUE%
FOR %%v IN (%VS_VERSIONS%) DO (
    CALL :loop_vs_versions %%v
)
IF %EMPTY_LOOP% (
    %##########################%
    %##% No matching Visual Studio instances were found
    %##########################%
    EXIT /B 1
)

EXIT /B %ERRORCODE%
ENDLOCAL

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:loop_vs_versions
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
SET VERSION_QUERY=%~1
IF %VERBOSE% %##% Querying VS version: %VERSION_QUERY%

IF %VERBOSE% %##%   %VSWHERE% %QUERY% %VERSION_QUERY% -property installationPath
FOR /F %ALL% %%p IN (`"%VSWHERE% %QUERY% %VERSION_QUERY% -property installationPath"`) DO (
IF %VERBOSE% %##% installationPath: %%p

IF %VERBOSE% %##%   %VSWHERE% -path "%%p" -property catalog_productLineVersion
FOR /F %ALL% %%e IN (`"%VSWHERE% -path "%%p" -property catalog_productLineVersion"`) DO (
IF %VERBOSE% %##% catalog_productLineVersion: %%e

IF %VERBOSE% %##%   %VSWHERE% -path "%%p" -property displayName
FOR /F %ALL% %%u IN (`"%VSWHERE% -path "%%p" -property displayName"`) DO (
IF %VERBOSE% %##% displayName: %%u

IF %VERBOSE% %##%   %VSWHERE% -path "%%p" -property isPrerelease
FOR /F %ALL% %%b IN (`"%VSWHERE% -path "%%p" -property isPrerelease"`) DO (
IF %VERBOSE% %##% isPrerelease: %%b

FOR /F %ALL% %%n IN (`"(ECHO %%b | FINDSTR /C:1 > NUL) && (ECHO %%u PREVIEW) || ECHO %%u"`) DO (
IF %VERBOSE% %##% friendlyName: %%n

IF %VERBOSE% %##%   %VSWHERE% -path "%%p" -property installationVersion
FOR /F %ALL% %%i IN (`"%VSWHERE% -path "%%p" -property installationVersion"`) DO (
IF %VERBOSE% %##% installationVersion: %%i

CALL :vs_version "%%~e" "%%~p" "%%~n" "%%~b" "%%~i"

))))))
EXIT /B

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:vs_version
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
IF %ERRORCODE% NEQ 0 EXIT /B
IF %BREAKLOOP% EXIT /B
SET EMPTY_LOOP=%FALSE%
SETLOCAL
SET VS=%~1
SET VS_PATH=%~2
SET VS_NAME=%~3
SET VS_PREVIEW=%~4
SET VS_VERSION=%~5
IF %VERBOSE% %##% VS instance: %VS_VERSION%

IF %LIST_VERSIONS% (
    CALL %SCRIPTLIB%\info.cmd "vs_version"
    ENDLOCAL
    EXIT /B
)

IF "%VCVARS_ARCH%" == "" (
    IF "%VS%" == "2022" (
        SET VCVARS_ARCH=x64
    ) ELSE (
        SET VCVARS_ARCH=x86
    )
)

IF %VERBOSE% %##% CALL "%VS_PATH%\VC\Auxiliary\Build\vcvarsall.bat %VCVARS_ARCH%"
CALL "%VS_PATH%\VC\Auxiliary\Build\vcvars64.bat" %VCVARS_ARCH% > NUL

WHERE /Q msbuild && (
    FOR /F %ALL% %%m IN (`msbuild -version -nologo`) DO SET MSBUILD_VERSION=%%m
) || (
    ECHO ERRORCODE: msbuild not found
    ENDLOCAL
    SET ERRORCODE=3
    EXIT /B
)

CALL %SCRIPTLIB%\info.cmd "version"
IF %VERBOSE% SET

IF NOT %INIT% IF NOT %REBUILD% IF %START_VS% (
    CALL %SCRIPTLIB%\startvs.cmd
    ENDLOCAL
    SET BREAKLOOP=%TRUE%
    EXIT /B
)

IF %CLEAN% CALL %SCRIPTLIB%\clean.cmd
IF %ERRORLEVEL% NEQ 0 GOTO :return

IF NOT EXIST version.log CALL %SCRIPTLIB%\log_version.cmd

IF %INIT% CALL %SCRIPTLIB%\init.cmd
IF %ERRORLEVEL% NEQ 0 GOTO :return

CALL %SCRIPTLIB%\msbuild.cmd
IF %ERRORLEVEL% NEQ 0 GOTO :return

IF %AUTOTEST% CALL %SCRIPTLIB%\tests.cmd
IF %ERRORLEVEL% NEQ 0 GOTO :return

IF %DEPLOY% CALL %SCRIPTLIB%\deploy.cmd
IF %ERRORLEVEL% NEQ 0 GOTO :return

IF %DO_INSTALL% CALL %SCRIPTLIB%\install.cmd
IF %ERRORLEVEL% NEQ 0 GOTO :return

IF %START_VS% (
    CALL %SCRIPTLIB%\startvs.cmd
    ENDLOCAL
    SET BREAKLOOP=%TRUE%
    EXIT /B
)

:return
ENDLOCAL
SET ERRORCODE=%ERRORLEVEL%
EXIT /B
