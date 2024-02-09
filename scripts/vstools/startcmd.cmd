::**************************************************************************************************
::Copyright (C) 2024 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::startvs.cmd
:: * Start a command prompt with the current environment
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

%##% %BOLD%Starting command prompt...%RESET%
%##########################%
START %COMSPEC% /K "PROMPT $E[90m[%VS_VERSION%]$S$E[0m$E[1m%VCVARS_ARCH%$E[0m$_$P$G"
