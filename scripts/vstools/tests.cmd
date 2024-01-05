::**************************************************************************************************
::Copyright (C) 2024 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::tests.cmd
:: * Looks for auto-tests generated during build
:: * Runs the auto-tests that were found
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

ECHO.
%##########################%
%##% %BOLD%Finding tests...%RESET%

DEL %TEMP%\vstools.args > NUL 2>&1

IF %VERBOSE% %##% DIR /S /B /A:D Tests\Test_*
FOR /F %ALL% %%c IN (`DIR /S /B /A:D Tests\Test_*`) DO (
IF %VERBOSE% %##% testProject: %%c
    IF %VERBOSE% %##% WHERE /R %%c\bin\%BUILD_CONFIGURATION% Test_*.dll
    IF %VERBOSE% WHERE /R %%c\bin\%BUILD_CONFIGURATION% Test_*.dll
    WHERE /R %%c\bin\%BUILD_CONFIGURATION% Test_*.dll >> %TEMP%\vstools.args 2> NUL
)

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
        %##% vstest.console /logger:console;verbosity=detailed @%TEMP%\vstools.args
        vstest.console /logger:console;verbosity=detailed @%TEMP%\vstools.args ^
        || (
            ECHO %RESET%
            GOTO :error
        )
    ) ELSE (
        vstest.console @%TEMP%\vstools.args ^
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

:error
IF %ERRORLEVEL% NEQ 0 (
    CALL %SCRIPTLIB%\error.cmd %ERRORLEVEL% "Tests failed!"
    EXIT /B %ERRORLEVEL%
)
