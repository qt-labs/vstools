:: Copyright (C) 2025 The Qt Company Ltd.
:: SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::startvs.cmd
:: * Start a command prompt with the current environment
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

%##% %BOLD%Starting command prompt...%RESET%
%##########################%
START %COMSPEC% /K "PROMPT $E[90m[%VS_VERSION%]$S$E[0m$E[1m%VCVARS_ARCH%$E[0m$_$P$G"
