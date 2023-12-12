::**************************************************************************************************
::Copyright (C) 2023 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::info.cmd
:: * Print information, according to %1 param
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

IF "%~1" == "version"    CALL :info_version
IF "%~1" == "vs_version" CALL :info_vs_version
IF "%~1" == "vars"       CALL :info_vars
IF "%~1" == "args"       CALL :info_args
EXIT /B

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:info_version
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
ECHO.
%##########################%
%##% %BOLD%%VS_NAME%%RESET% ^(%VS_VERSION%^)
IF NOT "%MSBUILD_VERSION%" == "" (
    IF "%VSCMD_ARG_HOST_ARCH%" == "%VSCMD_ARG_TGT_ARCH%" (
        %##% MSBuild v%MSBUILD_VERSION% ^(%VSCMD_ARG_TGT_ARCH%^)
    ) ELSE (
        %##% MSBuild v%MSBUILD_VERSION% ^(%VSCMD_ARG_HOST_ARCH% -^> %VSCMD_ARG_TGT_ARCH%^)
    )
)
IF "%VERSION_REV%" == "" (
    %##% Qt VS Tools version: %BOLD%%VERSION%%RESET%
) ELSE (
    %##% Qt VS Tools version: %BOLD%%VERSION% ^(rev.%VERSION_REV%^)%RESET%
)
IF NOT "%DEPLOY_DIR%" == "" %##% Deploy to: %DEPLOY_DIR%
%##########################%
EXIT /B

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:info_vs_version
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
ECHO.
%##########################%
%##% %BOLD%%VS_NAME%%RESET% ^(%VS_VERSION%^)
%##% %DARK_GRAY%%VS_PATH%%RESET%
%##########################%
EXIT /B

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:info_args
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
ECHO.
%##########################%
CALL :info_var_string   SCRIPT
CALL :info_var_string   ARGS
CALL :info_var_string   ROOT
CALL :info_var_string   SCRIPTLIB
%##########################%
EXIT /B

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:info_vars
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
ECHO.
%##########################%
CALL :info_var_string   VERSION
CALL :info_var_string   VERSION_MAJOR
CALL :info_var_string   VERSION_REV
CALL :info_var_string   VSWHERE
CALL :info_var_string   QUERY
CALL :info_var_string   VS_VERSIONS
CALL :info_var_string   VS_LATEST
CALL :info_var_string   VCVARS_ARCH
CALL :info_var_string   BUILD_CONFIGURATION
CALL :info_var_string   BUILD_PLATFORM
CALL :info_var_string   MSBUILD_TARGETS
CALL :info_var_string   MSBUILD_EXTRAS
CALL :info_var_string   MSBUILD_VERBOSITY
CALL :info_var_string   DEPENDENCIES
CALL :info_var_string   TRANSFORM_INCREMENTAL
CALL :info_var_string   DEPLOY_DIR
%##########################%
CALL :info_var_bool     INIT
CALL :info_var_bool     CLEAN
CALL :info_var_bool     REBUILD
CALL :info_var_bool     AUTOTEST
CALL :info_var_bool     DEPLOY
CALL :info_var_bool     DO_INSTALL
CALL :info_var_bool     START_VS
CALL :info_var_bool     LIST_VERSIONS
CALL :info_var_bool     BINARYLOG
CALL :info_var_bool     VS_VERSIONS_DEFAULT
CALL :info_var_bool     VERBOSE
%##########################%
EXIT /B

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:info_var_bool
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
CALL CMD /C "IF %%%1%% (%##% [X] %1)"
CALL CMD /C "IF NOT %%%1%% (%##% [ ] %1)"
EXIT /B

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:info_var_string
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
CALL CMD /C "%##% %1 = %%%1%%"
EXIT /B
