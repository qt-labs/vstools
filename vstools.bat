@ECHO OFF
SETLOCAL

SET SCRIPT=%~n0

SET "USAGE=%SCRIPT%"
SET "USAGE=%USAGE% [ -init ^| [-rebuild] [-deploy ^<DEPLOY_DIR^>] [-install] ]"
SET "USAGE=%USAGE% [-transform_incremental]"
SET "USAGE=%USAGE% [-vs2022]"
SET "USAGE=%USAGE% [-vs2019]"
SET "USAGE=%USAGE% [-vs2017]"
SET "USAGE=%USAGE% [-startvs]"
SET "USAGE=%USAGE% [-version ^<MAJOR_VS_VERSION^>.^<MINOR_VS_VERSION^>]"
SET "USAGE=%USAGE% [-verbose]"
SET "USAGE=%USAGE% [-bl]"

SET TRUE="0"=="0"
SET FALSE="0"=="1"
SET ALL="tokens=* usebackq"

SET VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
SET VSWHERE=%VSWHERE:(=^(%
SET VSWHERE=%VSWHERE:)=^)%
SET QUERY=-latest -prerelease

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
SET DO_INSTALL=%FALSE%
SET TRANSFORM_INCREMENTAL=false
SET START_VS=%FALSE%

SET PLATFORM_VS2017="Any CPU"
SET PLATFORM_VS2019="Any CPU"
SET PLATFORM_VS2022="x64"

SET FLAG_VS2022=-vs2022
SET FLAG_VS2019=-vs2019
SET FLAG_VS2017=-vs2017

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

IF %VERBOSE% ECHO ## VS versions: %VS_VERSIONS%

FOR %%v IN (%VS_VERSIONS%) DO (
    SETLOCAL

    IF %VERBOSE% ECHO ## Querying VS version: %%v

    IF %VERBOSE% ECHO ## "%VSWHERE% %QUERY% %%~v -property installationPath"
    FOR /F %ALL% %%p IN (`"%VSWHERE% %QUERY% %%~v -property installationPath"`) DO (
    IF %VERBOSE% ECHO ## installationPath: %%p

    IF %VERBOSE% ECHO ## "%VSWHERE% -path "%%p" -property catalog_productLineVersion"
    FOR /F %ALL% %%e IN (`"%VSWHERE% -path "%%p" -property catalog_productLineVersion"`) DO (
    IF %VERBOSE% ECHO ## catalog_productLineVersion: %%e

    IF %VERBOSE% ECHO ## "%VSWHERE% -path "%%p" -property displayName"
    FOR /F %ALL% %%n IN (`"%VSWHERE% -path "%%p" -property displayName"`) DO (
    IF %VERBOSE% ECHO ## displayName: %%n

    IF %VERBOSE% ECHO ## "%VSWHERE% -path "%%p" -property installationVersion"
    FOR /F %ALL% %%i IN (`"%VSWHERE% -path "%%p" -property installationVersion"`) DO (
    IF %VERBOSE% ECHO ## installationVersion: %%i

    IF %VERBOSE% ECHO ## CMD /C "ECHO %%PLATFORM_VS%%e%%"
    FOR /F %ALL% %%f IN (`CMD /C "ECHO %%PLATFORM_VS%%e%%"`) DO (
    IF %VERBOSE% ECHO ## platform: %%f

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
        IF NOT "%QtVSToolsDeployTarget%"=="" ECHO ## Deploy to: %QtVSToolsDeployTarget%
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

        IF %INIT% (
            ECHO ################################################################################
            ECHO ## Building pre-requisites...
            ECHO ################################################################################
            msbuild ^
                -nologo ^
                -verbosity:%MSBUILD_VERBOSITY% ^
                -maxCpuCount ^
                -t:%DEPENDENCIES% ^
                -p:Configuration=Release ^
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
        ECHO ## msbuild: -p:Configuration=Release
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
            -p:Configuration=Release ^
            -p:Platform=%%f ^
            -p:TransformOutOfDateOnly=%TRANSFORM_INCREMENTAL% ^
            -t:%MSBUILD_TARGETS% ^
            %MSBUILD_EXTRAS% ^
            vstools.sln ^
        && (
            ECHO ################################################################################
            ECHO ## %%n ^(%%i^)
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

        IF %DO_INSTALL% (
            ECHO ################################################################################
            ECHO ## Installing VSIX package
            ECHO ################################################################################
            ECHO Removing previous installation...
            IF "%%e"=="2022" (
                VSIXInstaller /uninstall:QtVsTools.8e827d74-6fc4-40a6-a3aa-faf19652b3b8
            ) ELSE IF "%%e"=="2019" (
                VSIXInstaller /uninstall:QtVsTools.bf3c71c0-ab41-4427-ada9-9b3813d89ff5
            ) ELSE IF "%%e"=="2017" (
                VSIXInstaller /uninstall:QtVsTools.13121978-cd02-4fd0-89bd-e36f85abe16a
            )
            ECHO Installing...
            VSIXInstaller QtVsTools.Package\bin\Release\QtVsTools.vsix
            ECHO.
        )

        IF %START_VS% (
            ECHO ###############################################################################
            ECHO ## Starting Visual Studio...
            ECHO ###############################################################################
            devenv vstools.sln
            EXIT /B 0
        )

        ECHO.
    )))))
    ENDLOCAL
)

EXIT /B 0

:usage
ECHO Syntax: %USAGE%
EXIT /B 1
