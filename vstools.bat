::**************************************************************************************************
::Copyright (C) 2024 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
::**************************************************************************************************
@ECHO OFF
SETLOCAL

SET SCRIPT=%~n0
SET SCRIPTLIB=%CD%\scripts\%SCRIPT%

CALL %SCRIPTLIB%\globals.cmd

CALL %SCRIPTLIB%\args.cmd %*
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%

CALL %SCRIPTLIB%\validate.cmd
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%

CALL %SCRIPTLIB%\vs_versions.cmd
EXIT /B %ERRORLEVEL%
