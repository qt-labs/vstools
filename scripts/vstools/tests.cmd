:: Copyright (C) 2025 The Qt Company Ltd.
:: SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::tests.cmd
:: * Looks for auto-tests generated during build
:: * Runs the auto-tests that were found
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

IF /I "%VS%"=="2019" GOTO :skip_tests_vs2019

ECHO.
%##########################%
%##% %BOLD%Finding tests...%RESET%

REM Note:
REM - If VSTOOLS_TEST_FILTER is undefined or empty -> run ALL Test_*.dll
REM - If VSTOOLS_TEST_FILTER is set -> only DLLs containing "%VSTOOLS_TEST_FILTER%.dll"
REM   Example:
REM       set VSTOOLS_TEST_FILTER=Test_QtVsTools.Core

DEL %TEMP%\vstools_all.args > NUL 2>&1
DEL %TEMP%\vstools.args > NUL 2>&1

IF %VERBOSE% %##% DIR /S /B /A:D Tests\Test_*
FOR /F %ALL% %%c IN (`DIR /S /B /A:D Tests\Test_*`) DO (
IF %VERBOSE% %##% testProject: %%c
    IF %VERBOSE% %##% WHERE /R %%c\bin\%BUILD_CONFIGURATION% Test_*.dll
    IF %VERBOSE% WHERE /R %%c\bin\%BUILD_CONFIGURATION% Test_*.dll

    REM Always collect ALL test DLLs first
    WHERE /R %%c\bin\%BUILD_CONFIGURATION% Test_*.dll >> %TEMP%\vstools_all.args 2> NUL
)

REM ----------------------------------------------------------------------------
REM Apply filter: if not defined -> use all DLLs, else only matching DLLs
REM ----------------------------------------------------------------------------

IF DEFINED VSTOOLS_TEST_FILTER GOTO :HaveFilter

COPY /Y %TEMP%\vstools_all.args %TEMP%\vstools.args > NUL
GOTO :AfterFilter

:HaveFilter
FINDSTR /I /C:"%VSTOOLS_TEST_FILTER%.dll" %TEMP%\vstools_all.args > %TEMP%\vstools.args 2> NUL

:AfterFilter
IF %VERBOSE% %##% FINDSTR /C:dll %TEMP%\vstools.args
IF %VERBOSE% FINDSTR /C:dll %TEMP%\vstools.args

FINDSTR /C:dll %TEMP%\vstools.args > NUL 2>&1 ^
&& (
    FOR /F %%c in ('TYPE %TEMP%\vstools.args') DO %##%   * %%~nc
    %##########################%
    ECHO.
    %##########################%
    %##% %BOLD%Running tests...%RESET%
    %##########################%
    IF NOT %VERBOSE% ECHO %DARK_GRAY%
    IF %VERBOSE% (
        %##% dotnet test --logger "console;verbosity=normal" @%TEMP%\vstools.args
        dotnet test --logger "console;verbosity=normal" @%TEMP%\vstools.args ^
        || (
            ECHO %RESET%
            GOTO :error
        )
    ) ELSE (
        dotnet test --logger "console;verbosity=normal" @%TEMP%\vstools.args ^
        || (
            ECHO %RESET%
            GOTO :error
        )
    )
) || (
    %##%   * %BOLD%%YELLOW%No tests found.%RESET%
    %##########################%
    GOTO :eof
)

ECHO %RESET%
CALL %SCRIPTLIB%\info.cmd "version"
%##% %BOLD%%GREEN%Test run successful.%RESET%
%##########################%
GOTO :eof

:skip_tests_vs2019
%##########################%
%##% %BOLD%%YELLOW%Skipping tests for Visual Studio 2019 (no longer supported).%RESET%
%##########################%
%##########################%
EXIT /B 0

:error
IF %ERRORLEVEL% NEQ 0 (
    CALL %SCRIPTLIB%\error.cmd %ERRORLEVEL% "Tests failed!"
    EXIT /B %ERRORLEVEL%
)
