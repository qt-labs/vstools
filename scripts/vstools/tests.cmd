::**************************************************************************************************
::Copyright (C) 2023 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::tests.cmd
:: * Looks for auto-tests generated during build
:: * Runs the auto-tests that were found
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

ECHO.
%##########################%
%##% Finding tests...

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
    IF %VERBOSE% (
        %##% vstest.console /logger:console;verbosity=detailed @%TEMP%\vstools.args
        vstest.console /logger:console;verbosity=detailed @%TEMP%\vstools.args ^
        || (
            GOTO :return
        )
    ) ELSE (
        vstest.console @%TEMP%\vstools.args ^
        || (
            GOTO :return
        )
    )
) || (
    %##%   * No tests found.
    %##########################%
    GOTO :eof
)

CALL %SCRIPTLIB%\info.cmd "version"
%##% Tests completed successfully
%##########################%

:return
IF %ERRORLEVEL% NEQ 0 (
    CALL %SCRIPTLIB%\error.cmd %ERRORLEVEL% "Tests failed!"
    EXIT /B %ERRORLEVEL%
)
