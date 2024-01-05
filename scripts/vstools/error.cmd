::**************************************************************************************************
::Copyright (C) 2024 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::error.cmd
:: * Print error message to STDERR
:: * Preserves %ERRORLEVEL% passed as %1 argument
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

ECHO.
%##########################%
%##% %BOLD%%RED%%~2%RESET% 1>&2
%##########################%
EXIT /B %1
