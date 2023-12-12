::**************************************************************************************************
::Copyright (C) 2023 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::console.cmd
:: * Definitions that enable use of console features (e.g. color text)
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

SET ESC=
SET [=%ESC%[
SET ]=m

SET RESET=%[%0%]%

SET BOLD=%[%1%]%
SET UNDERLINE=%[%4%]%
SET NO_UNDERLINE=%[%24%]%
SET INVERSE=%[%7%]%
SET NO_INVERSE=%[%27%]%

SET BLACK=%[%30%]%
SET DARK_RED=%[%31%]%
SET DARK_GREEN=%[%32%]%
SET DARK_YELLOW=%[%33%]%
SET DARK_BLUE=%[%34%]%
SET DARK_MAGENTA=%[%35%]%
SET DARK_CYAN=%[%36%]%
SET DARK_GRAY=%[%90%]%
SET GRAY=%[%37%]%
SET RED=%[%91%]%
SET GREEN=%[%92%]%
SET YELLOW=%[%93%]%
SET BLUE=%[%94%]%
SET MAGENTA=%[%95%]%
SET CYAN=%[%96%]%
SET WHITE=%[%97%]%

SET BKG_BLACK=%[%40%]%
SET BKG_DARK_RED=%[%41%]%
SET BKG_DARK_GREEN=%[%42%]%
SET BKG_DARK_YELLOW=%[%43%]%
SET BKG_DARK_BLUE=%[%44%]%
SET BKG_DARK_MAGENTA=%[%45%]%
SET BKG_DARK_CYAN=%[%46%]%
SET BKG_DARK_GRAY=%[%100%]%
SET BKG_GRAY=%[%47%]%
SET BKG_RED=%[%101%]%
SET BKG_GREEEN=%[%102%]%
SET BKG_YELLOW=%[%103%]%
SET BKG_BLUE=%[%104%]%
SET BKG_MAGENTA=%[%105%]%
SET BKG_CYAN=%[%106%]%
SET BKG_WHITE=%[%107%]%
