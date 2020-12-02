@ECHO OFF
SETLOCAL

SET SCRIPT=%~n0

SET "USAGE=%SCRIPT%"
SET "USAGE=%USAGE% [ -init ^| [-rebuild] [-deploy ^<DEPLOY_DIR^>] [-install] ]"
SET "USAGE=%USAGE% [-transform_incremental]"
SET "USAGE=%USAGE% [-vs2019]"
SET "USAGE=%USAGE% [-vs2017]"
SET "USAGE=%USAGE% [-vs2015]"
SET "USAGE=%USAGE% [-verbose]"
SET "USAGE=%USAGE% [-bl]"

SET TRUE="0"=="0"
SET FALSE=NOT %TRUE%

SET VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -legacy -latest

SET VS2019="-version [16.0^,17.0^)"
SET VS2017="-version [15.0^,16.0^)"
SET VS2015="-version [14.0^,15.0^)"
SET VS_ALL=%VS2019%,%VS2017%,%VS2015%
SET VS_LATEST="-version [14.0^,17.0^)"

SET VS_VERSIONS=
SET VS_VERSIONS_DEFAULT=%TRUE%
SET VERBOSE=%FALSE%
SET REBUILD=%FALSE%
SET INIT=%FALSE%
SET BINARYLOG=%FALSE%
SET DO_INSTALL=%FALSE%
SET TRANSFORM_INCREMENTAL=false

SET FLAG_VS2019=-vs2019
SET FLAG_VS2017=-vs2017
SET FLAG_VS2015=-vs2015

:parseArgs
IF NOT "%1"=="" (
    IF "%1"=="-init" (
        SET INIT=%TRUE%
    ) ELSE IF "%1"=="-rebuild" (
        SET REBUILD=%TRUE%
    ) ELSE IF "%1"=="-deploy" (
        SET QtVSToolsDeployTarget=%~f2
        SHIFT
    ) ELSE IF "%1"=="-install" (
        SET DO_INSTALL=%TRUE%
    ) ELSE IF "%1"=="-transform_incremental" (
        SET TRANSFORM_INCREMENTAL=true
    ) ELSE IF "%1"=="-verbose" (
        SET VERBOSE=%TRUE%
    ) ELSE IF "%1"=="-bl" (
        SET BINARYLOG=%TRUE%
    ) ELSE IF "%1"=="%FLAG_VS2019%" (
        SET VS_VERSIONS=%VS_VERSIONS%,%VS2019%
        SET VS_LATEST=%VS2019%
        SET VS_VERSIONS_DEFAULT=%FALSE%
        SET FLAG_VS2019=
    ) ELSE IF "%1"=="%FLAG_VS2017%" (
        SET VS_VERSIONS=%VS_VERSIONS%,%VS2017%
        SET VS_LATEST=%VS2017%
        SET VS_VERSIONS_DEFAULT=%FALSE%
        SET FLAG_VS2017=
    ) ELSE IF "%1"=="%FLAG_VS2015%" (
        SET VS_VERSIONS=%VS_VERSIONS%,%VS2015%
        SET VS_LATEST=%VS2015%
        SET VS_VERSIONS_DEFAULT=%FALSE%
        SET FLAG_VS2015=
    ) ELSE IF "%1"=="-help" (
        GOTO :usage
    ) ELSE (
        ECHO Unknown argument '%1' 1>&2
        GOTO :usage
    )
    SHIFT
    GOTO :parseArgs
)

IF %INIT% (
    SET MSBUILD_TARGETS=Clean
    SET MSBUILD_EXTRAS=%MSBUILD_EXTRAS% /p:CleanDependsOn=TransformDuringBuild
    SET VS_VERSIONS=%VS_ALL%
    SET VS_VERSIONS=%VS_LATEST%
    SET DO_INSTALL=%FALSE%
    SET TRANSFORM_INCREMENTAL=false
) ELSE IF %REBUILD% (
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
    SET MSBUILD_EXTRAS=%MSBUILD_EXTRAS% /bl
)

FOR %%v IN (%VS_VERSIONS%) DO (
    FOR /F "tokens=* usebackq" %%n IN (`%VSWHERE% %%~v -property installationVersion`) DO (
        ECHO.
        ECHO.
        ECHO ################################################################################
        ECHO ## Visual Studio %%n
        IF NOT "%QtVSToolsDeployTarget%"=="" ECHO ## Deploy to: %QtVSToolsDeployTarget%
        ECHO ################################################################################
        ECHO.
    )

    FOR /F "tokens=* usebackq" %%p IN (`%VSWHERE% %%~v -property installationPath`) DO (
        SETLOCAL
        IF EXIST "%%p\VC\Auxiliary\Build\vcvars32.bat" (
            IF %VERBOSE% ECHO ## CALL "%%p\VC\Auxiliary\Build\vcvars32.bat"
            CALL "%%p\VC\Auxiliary\Build\vcvars32.bat" > NUL
        ) ELSE IF EXIST "%%p\VC\vcvarsall.bat" (
            IF %VERBOSE% ECHO ## CALL "%%p\VC\vcvarsall.bat" x86 8.1
            CALL "%%p\VC\vcvarsall.bat" x86 8.1 > NUL
        ) ELSE (
            ECHO Error running VC Vars script 1>&2
            EXIT /B 2
        )

        msbuild ^
            /verbosity:%MSBUILD_VERBOSITY% ^
            /maxCpuCount ^
            /p:Configuration=Release ^
            /p:Platform="Any CPU" ^
            /p:TransformOutOfDateOnly=%TRANSFORM_INCREMENTAL% ^
            /t:%MSBUILD_TARGETS% ^
            %MSBUILD_EXTRAS% ^
            QtVSTools.sln

        IF NOT "%ERRORLEVEL%"=="0" (
            ECHO Error building solution 1>&2
            EXIT /B %ERRORLEVEL%
        )

        IF %DO_INSTALL% (
            ECHO Removing previous installation...
            IF "%%~v"=="%VS2019:"=%" (
                VSIXInstaller /uninstall:QtVsTools.bf3c71c0-ab41-4427-ada9-9b3813d89ff5
            ) ELSE IF "%%~v"=="%VS2017:"=%" (
                VSIXInstaller /uninstall:QtVsTools.13121978-cd02-4fd0-89bd-e36f85abe16a
            ) ELSE IF "%%~v"=="%VS2015:"=%" (
                VSIXInstaller /uninstall:QtVsTools.30112013-cd02-4fd0-89bd-e36f85abe16a
            )
            ECHO Installing...
            VSIXInstaller qtvstools\bin\Release\QtVsTools.vsix
        )

        ENDLOCAL
    )
)

EXIT /B 0

:usage
ECHO Syntax: %USAGE%
EXIT /B 1
