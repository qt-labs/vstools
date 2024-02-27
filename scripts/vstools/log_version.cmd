::**************************************************************************************************
::Copyright (C) 2024 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::log_version.cmd
:: * Generates log file with extension version, based on the latest repository tag
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

ECHO.
%##########################%
%##% %BOLD%Logging extension version%RESET%
IF "%VERSION_REV%" == "" (
    ECHO %VERSION%.0 > version.log
    %##% %BOLD%%DARK_CYAN%%VERSION%%RESET%
) ELSE (
    ECHO %VERSION%.%VERSION_REV% > version.log
    %##% %BOLD%%DARK_CYAN%%VERSION% ^(rev.%VERSION_REV%^)%RESET%
)
%##########################%
