:: Copyright (C) 2025 The Qt Company Ltd.
:: SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

@ECHO OFF
SETLOCAL

PUSHD "%~dp0"
IF ERRORLEVEL 1 EXIT /B 1

SET SCRIPT=%~n0
SET "SCRIPTLIB=%CD%\scripts\%SCRIPT%"

CALL "%SCRIPTLIB%\globals.cmd"

CALL "%SCRIPTLIB%\args.cmd" %*
IF %ERRORLEVEL% NEQ 0 GOTO :done

CALL "%SCRIPTLIB%\validate.cmd"
IF %ERRORLEVEL% NEQ 0 GOTO :done

CALL "%SCRIPTLIB%\vs_versions.cmd"

:done
SET "SCRIPT_EXIT_CODE=%ERRORLEVEL%"
POPD
EXIT /B %SCRIPT_EXIT_CODE%
