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
%##% %BOLD%Deploying to %DEPLOY_DIR%\%VERSION%.%VERSION_REV%...%RESET%
IF "%VERSION_REV%" == "" (
    %##%   QtVsTools.vsix -^> qt-vsaddin-msvc%VS%-%VCVARS_ARCH%-%VERSION%.vsix
    MD "%DEPLOY_DIR%\%VERSION%.0" > NUL 2>&1
    COPY /Y QtVsTools.Package\bin\Release\QtVsTools.vsix ^
        "%DEPLOY_DIR%\%VERSION%.0\qt-vsaddin-msvc%VS%-%VCVARS_ARCH%-%VERSION%.vsix" > NUL ^
    || (%##%   %BOLD%%RED%Error copying package to deploy dir.%RESET%)
) ELSE (
    %##%   QtVsTools.vsix -^> qt-vsaddin-msvc%VS%-%VCVARS_ARCH%-%VERSION%-rev.%VERSION_REV%.vsix
    MD "%DEPLOY_DIR%\%VERSION%.%VERSION_REV%" > NUL 2>&1
    COPY /Y QtVsTools.Package\bin\Release\QtVsTools.vsix ^
        "%DEPLOY_DIR%\%VERSION%.%VERSION_REV%\qt-vsaddin-msvc%VS%-%VCVARS_ARCH%-%VERSION%-rev.%VERSION_REV%.vsix" > NUL ^
    || (%##%   %BOLD%%RED%Error copying package to deploy dir.%RESET%)
)
%##########################%
