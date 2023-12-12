::**************************************************************************************************
::Copyright (C) 2023 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::install.cmd
:: * Removes previously installed extension, if any
:: * Installs newly generated package
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

ECHO.
%##########################%
%##% %BOLD%Installing extension package%RESET%
%##########################%

ECHO Removing previous installation...
IF "%VS%" == "2022" (
    VSIXInstaller /uninstall:QtVsTools.8e827d74-6fc4-40a6-a3aa-faf19652b3b8
) ELSE IF "%VS%" == "2019" (
    VSIXInstaller /uninstall:QtVsTools.bf3c71c0-ab41-4427-ada9-9b3813d89ff5
)

ECHO Installing...
VSIXInstaller QtVsTools.Package\bin\Release\QtVsTools.vsix
EXIT /B %ERRORLEVEL%
