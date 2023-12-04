::**************************************************************************************************
::Copyright (C) 2023 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::log_version.cmd
:: * Generates log file with extension version, based on the latest repository tag
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

ECHO.
%##########################%
%##% Logging extension version
%##########################%
IF "%VERSION_REV%" == "" (
    ECHO %VERSION%.0 > version.log
) ELSE (
    ECHO %VERSION%.%VERSION_REV% > version.log
)
