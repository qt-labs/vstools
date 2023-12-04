::**************************************************************************************************
::Copyright (C) 2023 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::deploy.cmd
:: * Copies installation package to deployment directory
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

ECHO.
%##########################%
%##% Deploying to %DEPLOY_DIR%...
%##########################%
IF "%VERSION_REV%" == "" (
    ECHO   QtVsTools.vsix -^> qt-vsaddin-msvc%VS%-%VERSION%.vsix
    MD "%DEPLOY_DIR%\%VERSION%.0" > NUL 2>&1
    COPY /Y QtVsTools.Package\bin\Release\QtVsTools.vsix ^
        "%DEPLOY_DIR%\%VERSION%.0\qt-vsaddin-msvc%VS%-%VERSION%.vsix"
) ELSE (
    ECHO   QtVsTools.vsix -^> qt-vsaddin-msvc%VS%-%VERSION%-rev.%VERSION_REV%.vsix
    MD "%DEPLOY_DIR%\%VERSION%.%VERSION_REV%" > NUL 2>&1
    COPY /Y QtVsTools.Package\bin\Release\QtVsTools.vsix ^
        "%DEPLOY_DIR%\%VERSION%.%VERSION_REV%\qt-vsaddin-msvc%VS%-%VERSION%-rev.%VERSION_REV%.vsix"
)
