::**************************************************************************************************
::Copyright (C) 2023 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::args.cmd
:: * Parses command-line arguments, sets corresponding variables
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
SET ARGS=%*

:parseArgs
SET ARG=%1
IF "%ARG%" == "" GOTO :eof
IF "%ARG%" == "--" (
    SET MSBUILD_EXTRAS=%MSBUILD_EXTRAS% %ARGS:* -- =%
    GOTO :eof
)

SET ORIGINAL_ARG=%ARG%
IF "%ARG:~0,2%" == "--" (
    SET ARG=-%ARG:~2%
) ELSE IF "%ARG:~0,1%" == "/" (
    SET ARG=-%ARG:~1%
)

IF "%ARG%" == "-init" (
    SET INIT=%TRUE%
) ELSE IF "%ARG%" == "-build" (
    REM NOOP
) ELSE IF "%ARG%" == "-rebuild" (
    SET REBUILD=%TRUE%
) ELSE IF "%ARG%" == "-version" (
    IF "%2" == "" (
        %##########################%
        %##% %BOLD%%RED%Missing argument after '%ORIGINAL_ARG%'.%RESET% 1>&2
        %##########################%
        CALL %SCRIPTLIB%\usage.cmd
        EXIT /B 1
    )
    SET VS_VERSIONS=%VS_VERSIONS%,"-version [%2^,%2.65535]"
    SET VS_LATEST="-version [%2^,%2.65535]"
    SHIFT
    SET VS_VERSIONS_DEFAULT=%FALSE%
) ELSE IF "%ARG%" == "%FLAG_VS2022%" (
    SET VS_VERSIONS=%VS_VERSIONS%,%VS2022%
    SET VS_LATEST=%VS2022%
    SET VS_VERSIONS_DEFAULT=%FALSE%
    SET FLAG_VS2022=
) ELSE IF "%ARG%" == "%FLAG_VS2019%" (
    SET VS_VERSIONS=%VS_VERSIONS%,%VS2019%
    SET VS_LATEST=%VS2019%
    SET VS_VERSIONS_DEFAULT=%FALSE%
    SET FLAG_VS2019=
) ELSE IF "%ARG%" == "-startvs" (
    SET START_VS=%TRUE%
) ELSE IF "%ARG%" == "-list" (
    SET LIST_VERSIONS=%TRUE%
) ELSE IF "%ARG%" == "-vcvars" (
    IF "%~2" == "" (
        %##########################%
        %##% %BOLD%%RED%Missing argument after '%ORIGINAL_ARG%'.%RESET% 1>&2
        %##########################%
        CALL %SCRIPTLIB%\usage.cmd
        EXIT /B 1
    )
    SET VCVARS_ARCH=%~2
    SHIFT
) ELSE IF "%ARG%" == "-config" (
    IF "%~2" == "" (
        %##########################%
        %##% %BOLD%%RED%Missing argument after '%ORIGINAL_ARG%'.%RESET% 1>&2
        %##########################%
        CALL %SCRIPTLIB%\usage.cmd
        EXIT /B 1
    )
    SET BUILD_CONFIGURATION=%~2
    SHIFT
) ELSE IF "%ARG%" == "-platform" (
    IF "%~2" == "" (
        %##########################%
        %##% %BOLD%%RED%Missing argument after '%ORIGINAL_ARG%'.%RESET% 1>&2
        %##########################%
        CALL %SCRIPTLIB%\usage.cmd
        EXIT /B 1
    )
    SET BUILD_PLATFORM=%~2
    SHIFT
) ELSE IF "%ARG%" == "-test" (
    SET AUTOTEST=%TRUE%
) ELSE IF "%ARG%" == "-deploy" (
    SET DEPLOY=%TRUE%
    SET DEPLOY_DIR=%~f2
    SHIFT
) ELSE IF "%ARG%" == "-install" (
    SET DO_INSTALL=%TRUE%
) ELSE IF "%ARG%" == "-all" (
    SET QUERY=%QUERY_ALL%
) ELSE IF "%ARG%" == "-verbose" (
    SET VERBOSE=%TRUE%
) ELSE IF "%ARG%" == "-bl" (
    SET BINARYLOG=%TRUE%
) ELSE IF "%ARG%" == "-help" (
    CALL %SCRIPTLIB%\usage.cmd
    EXIT /B 1
) ELSE (
    %##########################%
    %##% %BOLD%%RED%Unknown argument '%ORIGINAL_ARG%'.%RESET% 1>&2
    %##########################%
    CALL %SCRIPTLIB%\usage.cmd
    EXIT /B 1
)
SHIFT
GOTO :parseArgs
