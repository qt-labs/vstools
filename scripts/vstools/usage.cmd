::**************************************************************************************************
::Copyright (C) 2023 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::usage.cmd
:: * Print tool usage instructions
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
ECHO.
%##########################%
%##%
%##% Usage:
%##%
%##%     %SCRIPT% [VS Versions] [Operation] [Options] [ -- ^<MSBuild Options^> ]
%##%
%##########################%
%##%
%##% == 'VS Versions' can be one or more of the following:
%##%  -vs2022 ................ Select the latest version of Visual Studio 2022
%##%  -vs2019 ................ Select the latest version of Visual Studio 2019
%##%  -version ^<X^>.^<Y^> ....... Select version X.Y of Visual Studio
%##%                           Can be specified several times
%##%
%##%  If no version is specified, the most recent version of VS is selected.
%##%
%##% == 'Operation' can be one of the following:
%##%  -build .... Incremental build of solution
%##%  -rebuild .. Clean build of solution
%##%  -init ..... Initialize vstools solution for the specified version of VS
%##%              If multiple versions are specified, the last one is selected
%##%  -startvs .. Open vstools solution in selected VS version
%##%  -list ..... Print list of Visual Studio installations
%##%  -help ..... Print tool usage instructions
%##%
%##%  If no operation is specified, -build is assumed by default.
%##%
%##% == 'Options' can be one or more of the following
%##%  -vcvars ^<ARCH^> ......... Select ARCH as the argument to the vcvars script
%##%                           Can be one of: x86, amd64, x86_amd64, x86_arm,
%##%                           x86_arm64, amd64_x86, amd64_arm, amd64_arm64
%##%  -config ^<NAME^> ......... Select NAME as the build configuration
%##%                           Defaults to the 'Release' configuration
%##%                           Only valid with -build or -rebuild
%##%  -platform ^<NAME^> ....... Select NAME as the build platform
%##%                           Only valid with -build or -rebuild
%##%  -test .................. Run auto-tests after successful build
%##%                           Only valid with -build or -rebuild
%##%  -deploy ^<DEPLOY_DIR^> ... Deploy installation package to DEPLOY_DIR
%##%                           Only valid with -build or -rebuild
%##%  -install ............... Install extension to selected VS version(s)
%##%                           Pops up VSIX installer dialog for confirmation
%##%  -startvs ............... Open vstools solution in selected VS version
%##%                           If multiple versions are specified, the last one
%##%                           is selected
%##%  -all ................... Include all VS installations
%##%                           By default, the latest installation is selected
%##%  -verbose ............... Print more detailed log information
%##%  -bl .................... Generate MSBuild binary log
%##%                           Only valid with -build or -rebuild
%##%
%##% == All arguments after '--' are passed verbatim to MSBuild
%##%  Example:
%##%      vstools -version 17.4 -rebuild -- -p:WarningLevel=3
%##%
%##########################%
