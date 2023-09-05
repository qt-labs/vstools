@ECHO OFF
SETLOCAL

SET SCRIPT=%~n0
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
SET VS2017="-version [15.0^,16.0^)"
SET VS_ALL=%VS2022%,%VS2019%,%VS2017%
SET VS_LATEST="-all"

SET VS_VERSIONS=
SET VS_VERSIONS_DEFAULT=%TRUE%
SET VERBOSE=%FALSE%
SET REBUILD=%FALSE%
SET INIT=%FALSE%
SET CLEAN=%FALSE%
SET BINARYLOG=%FALSE%
SET CONFIGURATION=Release
SET DO_INSTALL=%FALSE%
SET DEPLOY=%FALSE%
SET TRANSFORM_INCREMENTAL=true
SET START_VS=%FALSE%
SET LIST_VERSIONS=%FALSE%

SET PLATFORM_VS2017="Any CPU"
SET PLATFORM_VS2019="Any CPU"
SET PLATFORM_VS2022="x64"

SET FLAG_VS2022=-vs2022
SET FLAG_VS2019=-vs2019
SET FLAG_VS2017=-vs2017

REM ///////////////////////////////////////////////////////////////////////////////////////////////
REM // Process command line arguments
:parseArgs

SET NEXT_ARG=%2
SET NEXT_ARG_FIRST_TOKEN=%NEXT_ARG:~0,1%

IF NOT "%1"=="" (
    IF "%1"=="-init" (
        SET INIT=%TRUE%
    ) ELSE IF "%1"=="-rebuild" (
        SET REBUILD=%TRUE%
    ) ELSE IF "%1"=="-config" (
        IF NOT "%NEXT_ARG_FIRST_TOKEN%"=="-" (
            IF "%NEXT_ARG%"=="" (
                ECHO Unknown argument '%2' 1>&2
                GOTO :usage
            )
            SET CONFIGURATION=%NEXT_ARG%
            SHIFT
        )
    ) ELSE IF "%1"=="-deploy" (
        SET DEPLOY=%TRUE%
        SET DEPLOY_DIR=%~f2
        SHIFT
    ) ELSE IF "%1"=="-install" (
        SET DO_INSTALL=%TRUE%
    ) ELSE IF "%1"=="-build" (
        REM NOOP
    ) ELSE IF "%1"=="-verbose" (
        SET VERBOSE=%TRUE%
    ) ELSE IF "%1"=="-bl" (
        SET BINARYLOG=%TRUE%
    ) ELSE IF "%1"=="-startvs" (
        SET START_VS=%TRUE%
    ) ELSE IF "%1"=="-version" (
        SET VS_VERSIONS=%VS_VERSIONS%,"-version [%2^,%2.65535]"
        SET VS_LATEST="-version [%2^,%2.65535]"
        SHIFT
        SET VS_LATEST="-version [%2^,%2.65535]"
        SET VS_VERSIONS_DEFAULT=%FALSE%
    ) ELSE IF "%1"=="%FLAG_VS2022%" (
        SET VS_VERSIONS=%VS_VERSIONS%,%VS2022%
        SET VS_LATEST=%VS2022%
        SET VS_VERSIONS_DEFAULT=%FALSE%
        SET FLAG_VS2022=
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
    ) ELSE IF "%1"=="-list" (
        SET LIST_VERSIONS=%TRUE%
    ) ELSE IF "%1"=="-all" (
        SET QUERY=%QUERY_ALL%
    ) ELSE IF "%1"=="-help" (
        GOTO :usage
    ) ELSE (
        ECHO Unknown argument '%1' 1>&2
        GOTO :usage
    )
    SHIFT
    GOTO :parseArgs
)

IF NOT EXIST vstools.sln (
    ECHO Error: could not find Qt VS Tools solution file.
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

REM ///////////////////////////////////////////////////////////////////////////////////////////////
REM // Check requirements

IF %VERBOSE% ECHO ## root: %ROOT%

IF NOT EXIST vstools.sln (
    ECHO Error: could not find Qt VS Tools solution file.
    EXIT /B 1
)

IF NOT EXIST %VSWHERE_EXE% (
    ECHO Error: could not find Visual Studio Locator tool.
    EXIT /B 1
)

FOR /F %ALL% %%v IN (`"%VSWHERE% -help"`) DO (
    SET VSWHERE_LOGO=%%v
    GOTO :break_vswhere_help
)
:break_vswhere_help

IF %VERBOSE% ECHO ## vswhere: %VSWHERE_LOGO%

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
    ECHO Error: Visual Studio Locator version must be %VSWHERE_VERSION% or greater.
    EXIT /B 1
)

WHERE /Q git.exe || (
    ECHO Error: could not find git.
    EXIT /B 1
)

REM ///////////////////////////////////////////////////////////////////////////////////////////////
REM // Cycle through installed VS products

IF %VERBOSE% ECHO ## VS versions: %VS_VERSIONS%

FOR %%v IN (%VS_VERSIONS%) DO (
    SETLOCAL

    IF %VERBOSE% ECHO ## Querying VS version: %%v

    IF %VERBOSE% ECHO ##   %VSWHERE% %QUERY% %%~v -property installationPath
    FOR /F %ALL% %%p IN (`"%VSWHERE% %QUERY% %%~v -property installationPath"`) DO (
    IF %VERBOSE% ECHO ## installationPath: %%p

    IF %VERBOSE% ECHO ##   %VSWHERE% -path "%%p" -property catalog_productLineVersion
    FOR /F %ALL% %%e IN (`"%VSWHERE% -path "%%p" -property catalog_productLineVersion"`) DO (
    IF %VERBOSE% ECHO ## catalog_productLineVersion: %%e

    IF %VERBOSE% ECHO ##   %VSWHERE% -path "%%p" -property displayName
    FOR /F %ALL% %%u IN (`"%VSWHERE% -path "%%p" -property displayName"`) DO (
    IF %VERBOSE% ECHO ## displayName: %%u

    IF %VERBOSE% ECHO ##   %VSWHERE% -path "%%p" -property isPrerelease
    FOR /F %ALL% %%b IN (`"%VSWHERE% -path "%%p" -property isPrerelease"`) DO (
    IF %VERBOSE% ECHO ## isPrerelease: %%b

    FOR /F %ALL% %%n IN (`"(ECHO %%b | FINDSTR /C:1 > NUL) && (ECHO %%u PREVIEW) || ECHO %%u"`) DO (
    IF %VERBOSE% ECHO ## friendlyName: %%n

    IF %VERBOSE% ECHO ##   %VSWHERE% -path "%%p" -property installationVersion
    FOR /F %ALL% %%i IN (`"%VSWHERE% -path "%%p" -property installationVersion"`) DO (
    IF %VERBOSE% ECHO ## installationVersion: %%i

    FOR /F %ALL% %%f IN (`CMD /C "ECHO %%PLATFORM_VS%%e%%"`) DO (
    IF %VERBOSE% ECHO ## platform: %%f

    IF %VERBOSE% ECHO ##   git describe --tags
    FOR /F "tokens=1,2,3,4 delims=v.- usebackq" %%q IN (`git describe --tags`) DO (
    IF %VERBOSE% ECHO ## ^( %%q, %%r, %%s, %%t ^)

    IF %LIST_VERSIONS% (
        IF %VERBOSE% ECHO ## listVersion
        ECHO %%n ^(%%i^)
        ECHO ^[%%p^]
    ) ELSE (

        IF "%%e"=="2022" (
            IF %VERBOSE% ECHO ## CALL "%%p\VC\Auxiliary\Build\vcvars64.bat"
            CALL "%%p\VC\Auxiliary\Build\vcvars64.bat" > NUL
        ) ELSE (
            IF %VERBOSE% ECHO ## CALL "%%p\VC\Auxiliary\Build\vcvars32.bat"
            CALL "%%p\VC\Auxiliary\Build\vcvars32.bat" > NUL
        )

        ECHO ################################################################################
        ECHO ## %%n ^(%%i^)
        WHERE /Q msbuild && (
            FOR /F %ALL% %%m IN (`msbuild -version -nologo`) DO (
                ECHO ## msbuild v%%m
            )
        ) || (
            ECHO Error: msbuild not found
            EXIT /B 3
        )
        IF "%%t"=="" (
            ECHO ## Qt VSTools version: %%q.%%r.%%s
        ) ELSE (
            ECHO ## Qt VSTools version: %%q.%%r.%%s ^(rev.%%t^)
        )
        IF NOT "%DEPLOY_DIR%"=="" ECHO ## Deploy to: %DEPLOY_DIR%
        ECHO ################################################################################
        ECHO.

        IF NOT %INIT% (
        IF NOT %REBUILD% (
        IF %START_VS% (
            ECHO ###############################################################################
            ECHO ## Starting Visual Studio...
            ECHO ###############################################################################
            devenv vstools.sln
            EXIT /B 0
        )))

        IF %CLEAN% (
            ECHO ###############################################################################
            ECHO ## Deleting output files...
            ECHO ###############################################################################
            RD /S /Q bin > NUL 2>&1
            FOR /F %ALL% %%d IN (`DIR /A:D /B /S bin 2^> NUL`) DO (
                RD /S /Q %%d > NUL 2>&1
            )
            RD /S /Q obj > NUL 2>&1
            FOR /F %ALL% %%d IN (`DIR /A:D /B /S obj 2^> NUL`) DO (
                RD /S /Q %%d > NUL 2>&1
            )
            ECHO.

            ECHO ###############################################################################
            ECHO ## Logging extension version
            ECHO ###############################################################################
            IF "%%t"=="" (
                ECHO %%q.%%r.%%s.0 > version.log
            ) ELSE (
                ECHO %%q.%%r.%%s.%%t > version.log
            )
            ECHO.

            ECHO ###############################################################################
            ECHO ## Restoring packages...
            ECHO ###############################################################################
            msbuild ^
                -nologo ^
                -verbosity:%MSBUILD_VERBOSITY% ^
                -t:Restore ^
            && (
                ECHO.
            ) || (
                ECHO ###############################################################################
                ECHO ## ERROR restoring packages! 1>&2
                ECHO ###############################################################################
                EXIT /B %ERRORLEVEL%
            )
        )

        IF NOT EXIST version.log (
            ECHO ###############################################################################
            ECHO ## Logging extension version
            ECHO ###############################################################################
            IF "%%t"=="" (
                ECHO %%q.%%r.%%s.0 > version.log
            ) ELSE (
                ECHO %%q.%%r.%%s.%%t > version.log
            )
        )

        IF %INIT% (
            ECHO ################################################################################
            ECHO ## Building pre-requisites...
            IF %VERBOSE% (
                ECHO ## msbuild: vstools.sln
                ECHO ## msbuild: -t:%DEPENDENCIES%
                ECHO ## msbuild: -p:Configuration=%CONFIGURATION%
                ECHO ## msbuild: -p:Platform=%%f
                ECHO ## msbuild: -p:TransformOutOfDateOnly=false
                ECHO ## msbuild extras: %MSBUILD_EXTRAS%
            )
            ECHO ################################################################################
            msbuild ^
                -nologo ^
                -verbosity:%MSBUILD_VERBOSITY% ^
                -maxCpuCount ^
                -t:%DEPENDENCIES% ^
                -p:Configuration=%CONFIGURATION% ^
                -p:Platform=%%f ^
                -p:TransformOutOfDateOnly=false ^
                %MSBUILD_EXTRAS% ^
                vstools.sln ^
            && (
                ECHO.
            ) || (
                ECHO ###############################################################################
                ECHO ## ERROR building pre-requisites 1>&2
                ECHO ###############################################################################
                EXIT /B %ERRORLEVEL%
            )
        )

        ECHO ################################################################################
        ECHO ## msbuild: vstools.sln
        ECHO ## msbuild: -t:%MSBUILD_TARGETS%
        ECHO ## msbuild: -p:Configuration=%CONFIGURATION%
        ECHO ## msbuild: -p:Platform=%%f
        IF %VERBOSE% (
            ECHO ## msbuild: -p:TransformOutOfDateOnly=%TRANSFORM_INCREMENTAL%
            ECHO ## msbuild: -verbosity:%MSBUILD_VERBOSITY%
            ECHO ## msbuild extras: %MSBUILD_EXTRAS%
        )
        ECHO ################################################################################
        msbuild ^
            -nologo ^
            -verbosity:%MSBUILD_VERBOSITY% ^
            -maxCpuCount ^
            -p:Configuration=%CONFIGURATION% ^
            -p:Platform=%%f ^
            -p:TransformOutOfDateOnly=%TRANSFORM_INCREMENTAL% ^
            -t:%MSBUILD_TARGETS% ^
            %MSBUILD_EXTRAS% ^
            vstools.sln ^
        && (
            ECHO ################################################################################
            ECHO ## %%n ^(%%i^)
            IF "%%t"=="" (
                ECHO ## Qt VSTools version: %%q.%%r.%%s
            ) ELSE (
                ECHO ## Qt VSTools version: %%q.%%r.%%s ^(rev.%%t^)
            )
            ECHO ## Solution build successful.
            ECHO ################################################################################
            ECHO.
        ) || (
            ECHO ################################################################################
            ECHO ## %%n ^(%%i^)
            ECHO ## ERROR building solution 1>&2
            ECHO ################################################################################
            EXIT /B %ERRORLEVEL%
        )

        IF %DEPLOY% (
            ECHO ################################################################################
            ECHO ## Deploying to %DEPLOY_DIR%...
            ECHO ################################################################################
            IF "%%t"=="" (
                ECHO   QtVsToolsLegacy.vsix -^> qt-LEGACYvsaddin-msvc%%e-%%q.%%r.%%s.vsix
                MD "%DEPLOY_DIR%\%%q.%%r.%%s.0" > NUL 2>&1
                COPY /Y QtVsTools.Package\bin\Release\QtVsToolsLegacy.vsix ^
                    "%DEPLOY_DIR%\%%q.%%r.%%s.0\qt-LEGACYvsaddin-msvc%%e-%%q.%%r.%%s.vsix"
            ) ELSE (
                ECHO   QtVsToolsLegacy.vsix -^> qt-LEGACYvsaddin-msvc%%e-%%q.%%r.%%s-rev.%%t.vsix
                MD "%DEPLOY_DIR%\%%q.%%r.%%s.%%t" > NUL 2>&1
                COPY /Y QtVsTools.Package\bin\Release\QtVsToolsLegacy.vsix ^
                    "%DEPLOY_DIR%\%%q.%%r.%%s.%%t\qt-LEGACYvsaddin-msvc%%e-%%q.%%r.%%s-rev.%%t.vsix"
            )
            ECHO.
        )

        IF %DO_INSTALL% (
            ECHO ################################################################################
            ECHO ## Installing extension package
            ECHO ################################################################################
            ECHO Removing previous installation...
            IF "%%e"=="2022" (
                VSIXInstaller /uninstall:QtVsTools.f3b37dcd-515b-46f9-9069-aa09693e5f16
            ) ELSE IF "%%e"=="2019" (
                VSIXInstaller /uninstall:QtVsTools.806c5209-1581-4e75-b6c7-e376fa44321e
            )
            ECHO Installing...
            VSIXInstaller QtVsTools.Package\bin\Release\QtVsToolsLegacy.vsix
            ECHO.
        )

        IF %START_VS% (
            ECHO ###############################################################################
            ECHO ## Starting Visual Studio...
            ECHO ###############################################################################
            devenv vstools.sln
            EXIT /B 0
        )

        )
        ECHO.
    ))))))))
    ENDLOCAL
)

EXIT /B 0

:usage

ECHO Usage:
ECHO.
ECHO     %SCRIPT% [VS Versions] [Operation] [Options]
ECHO.
ECHO == 'VS Versions' can be one or more of the following:
ECHO  -vs2022 ................ Select the latest version of Visual Studio 2022
ECHO  -vs2019 ................ Select the latest version of Visual Studio 2019
ECHO  -vs2017 ................ Select the latest version of Visual Studio 2017
ECHO  -version ^<X^>.^<Y^> ....... Select version X.Y of Visual Studio
ECHO                           Can be specified several times
ECHO.
ECHO  If no version is specified, the most recent version of VS is selected.
ECHO.
ECHO == 'Operation' can be one of the following:
ECHO  -build ......... Incremental build of solution
ECHO  -rebuild ....... Clean build of solution
ECHO  -init .......... Initialize vstools solution for the specified version of VS
ECHO                   If multiple versions are specified, the last one is selected
ECHO  -startvs ....... Open vstools solution in selected VS version
ECHO  -list .......... Print list of Visual Studio installations
ECHO  -help .......... Print tool usage instructions
ECHO.
ECHO  If no operation is specified, -build is assumed by default.
ECHO.
ECHO == 'Options' can be one or more of the following
ECHO  -config ^<CONFIG_NAME^> ....... Select CONFIG_NAME as the build configuration
ECHO                                Defaults to the 'Release' configuration
ECHO                                Only valid with -build or -rebuild
ECHO  -deploy ^<DEPLOY_DIR^> ........ Deploy installation package to DEPLOY_DIR
ECHO                                Only valid with -build or -rebuild
ECHO  -install .................... Install extension to selected VS version(s)
ECHO                                Only valid with -build or -rebuild
ECHO  -startvs .................... Open vstools solution in selected VS version
ECHO                                If multiple versions are specified, the last one is selected
ECHO  -all ........................ Include all VS installations
ECHO                                By default, the latest installation is selected
ECHO  -verbose .................... Print more detailed log information
ECHO  -bl ......................... Generate MSBuild binary log
ECHO                                Only valid with -build or -rebuild
ECHO.

EXIT /B 1
