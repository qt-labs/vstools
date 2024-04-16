::**************************************************************************************************
::Copyright (C) 2024 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
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
    start "Uninstalling 2022" /WAIT VSIXInstaller %VSIX_INSTALLER_ARG% /force /quiet /shutdownprocesses /uninstall:QtVsTools.8e827d74-6fc4-40a6-a3aa-faf19652b3b8
) ELSE IF "%VS%" == "2019" (
    start "Uninstalling 2019" /WAIT VSIXInstaller %VSIX_INSTALLER_ARG% /force /quiet /shutdownprocesses /uninstall:QtVsTools.bf3c71c0-ab41-4427-ada9-9b3813d89ff5
)

ECHO Installing...
start "Installing" /WAIT VSIXInstaller %VSIX_INSTALLER_ARG% /force /quiet /shutdownprocesses QtVsTools.Package\bin\%BUILD_CONFIGURATION%\QtVsTools.vsix
EXIT /B %ERRORLEVEL%
