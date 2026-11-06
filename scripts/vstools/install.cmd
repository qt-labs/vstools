:: Copyright (C) 2025 The Qt Company Ltd.
:: SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

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

IF "%BUILD_PLATFORM%" == "" (
    SET BUILD_PLATFORM=%VSCMD_ARG_TGT_ARCH%
)
IF /I "%BUILD_PLATFORM%" == "arm64" (
    SET VSTOOLS_PACKAGE_ID=QtVsTools.32c0f213-ae14-4cc2-bae9-8fa56ad8bddf
) ELSE (
    SET VSTOOLS_PACKAGE_ID=QtVsTools.8e827d74-6fc4-40a6-a3aa-faf19652b3b8
)
start "Uninstalling" /WAIT VSIXInstaller %VSIX_INSTALLER_ARG% /force /quiet /shutdownprocesses /uninstall:%VSTOOLS_PACKAGE_ID%

ECHO Installing...
start "Installing" /WAIT VSIXInstaller %VSIX_INSTALLER_ARG% /force /quiet /shutdownprocesses QtVsTools.Package\bin\%BUILD_CONFIGURATION%\QtVsTools.vsix
EXIT /B %ERRORLEVEL%
