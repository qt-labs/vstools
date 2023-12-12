::**************************************************************************************************
::Copyright (C) 2023 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::validate.cmd
:: * Calculates additional definitions based on arguments passed to the script
:: * Checks if all requirements are present to build the solution
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

IF %VERBOSE% CALL %SCRIPTLIB%\info.cmd "args"

IF NOT EXIST vstools.sln (
    ECHO %BOLD%%RED%Error: could not find Qt VS Tools solution file.%RESET%
    EXIT /B 1
)

IF %INIT% (
    SET CLEAN=%TRUE%
    SET MSBUILD_TARGETS=Clean
    SET MSBUILD_EXTRAS=%MSBUILD_EXTRAS% -p:CleanDependsOn=TransformDuringBuild
    SET VS_VERSIONS=%VS_LATEST%
    SET DO_INSTALL=%FALSE%
    SET TRANSFORM_INCREMENTAL=false
) ELSE IF %REBUILD% (
    SET CLEAN=%TRUE%
    SET INIT=%TRUE%
    SET MSBUILD_TARGETS=Rebuild
    IF %VS_VERSIONS_DEFAULT% SET VS_VERSIONS=%VS_ALL%
    SET TRANSFORM_INCREMENTAL=false
) ELSE (
    SET MSBUILD_TARGETS=Build
    IF %VS_VERSIONS_DEFAULT% SET VS_VERSIONS=%VS_ALL%
)

IF %VERBOSE% (
    SET MSBUILD_VERBOSITY=normal
) ELSE (
    SET MSBUILD_VERBOSITY=minimal
)

IF %BINARYLOG% (
    SET MSBUILD_EXTRAS=%MSBUILD_EXTRAS% -bl
)

IF NOT EXIST vstools.sln (
    ECHO %BOLD%%RED%Error: could not find Qt VS Tools solution file.%RESET%
    EXIT /B 1
)

IF NOT EXIST %VSWHERE_EXE% (
    ECHO %BOLD%%RED%Error: could not find Visual Studio Locator tool.%RESET%
    EXIT /B 1
)

FOR /F %ALL% %%v IN (`"%VSWHERE% -help"`) DO (
    SET VSWHERE_LOGO=%%v
    GOTO :break_vswhere_help
)
:break_vswhere_help

IF %VERBOSE% %##% vswhere: %VSWHERE_LOGO%
SET VSWHERE_OK=%TRUE%
FOR /F "tokens=5,6,7 delims=.+ " %%v IN ("%VSWHERE_LOGO%") DO (
    IF %%v LSS %VSWHERE_MAJOR% (
        SET VSWHERE_OK=%FALSE%
    ) ELSE IF %%v EQU %VSWHERE_MAJOR% (
        IF %%w LSS %VSWHERE_MINOR% (
            SET VSWHERE_OK=%FALSE%
        ) ELSE IF %%w EQU %VSWHERE_MINOR% IF %%x LSS %VSWHERE_PATCH% (
            SET VSWHERE_OK=%FALSE%
        )
    )
)
IF NOT %VSWHERE_OK% (
    ECHO %BOLD%%RED%Error: Visual Studio Locator version must be %VSWHERE_VERSION% or greater.%RESET%
    EXIT /B 1
)

WHERE /Q git.exe || (
    ECHO %BOLD%%RED%Error: could not find git.%RESET%
    EXIT /B 1
)
IF %VERBOSE% %##% git describe --tags
FOR /F "tokens=1,2,3,4 delims=v.- usebackq" %%q IN (`git describe --tags`) DO (
    SET VERSION_MAJOR=%%~q
    SET VERSION=%%~q.%%~r.%%~s
    SET VERSION_REV=%%~t
)

IF %VERBOSE% CALL %SCRIPTLIB%\info.cmd "vars"
