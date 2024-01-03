::**************************************************************************************************
::Copyright (C) 2023 The Qt Company Ltd.
::SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
::**************************************************************************************************

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::banner.cmd
:: * Utility definitions that can be used to print messages inside a frame (i.e. banner).
:: * Example:
::      %##########################%
::      %##% LINE 1
::      %##% LINE 2
::      %##% LINE 3
::      %##########################%
::   Will print out the following:
::      ################################################################################
::      ## LINE 1                                                                     ##
::      ## LINE 2                                                                     ##
::      ## LINE 3                                                                     ##
::      ################################################################################
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

SET BANNER_CHARS=#
SET BANNER_WIDTH=80
SET BANNER_FRAME_CHARS=#
SET BANNER_FRAME_WIDTH=2
SET BANNER_FILE=%TEMP%\vstools.banner

SET ##########################=%BANNER_CHARS%
FOR /L %%i IN (2,1,%BANNER_WIDTH%) DO (
    CALL SET "##########################=%%##########################%%%##########################%"
)
ECHO %##########################%>%BANNER_FILE%
FSUTIL file queryvaliddata %BANNER_FILE% | FOR /F "tokens=2 delims=()" %%a IN ('FINDSTR /R /C:"^[^ ]"') DO @EXIT /B %%a

SET /A "BANNER_LEN = %ERRORLEVEL% - 2"
SET /A "BANNER_FILE_SIZE = BANNER_LEN + 1"

SET ##=%BANNER_FRAME_CHARS%
FOR /L %%i IN (2,1,%BANNER_FRAME_WIDTH%) DO CALL SET "##=%%##%%%##%"
ECHO %##%>%BANNER_FILE%
FSUTIL file queryvaliddata %BANNER_FILE% | FOR /F "tokens=2 delims=()" %%a IN ('FINDSTR /R /C:"^[^ ]"') DO @EXIT /B %%a

SET /A "BANNER_FRAME_LEN = %ERRORLEVEL% - 2"
SET /A "EMPTY_LEN = BANNER_LEN - (2 * BANNER_FRAME_LEN)"

SET EMPTYLN= &
FOR /L %%i IN (2,1,%EMPTY_LEN%) DO CALL SET "EMPTYLN=%%EMPTYLN%%%EMPTYLN%"
ECHO %##%%EMPTYLN%%##%>%BANNER_FILE%
FSUTIL file seteof %BANNER_FILE% %BANNER_FILE_SIZE% > NUL 2>&1

SET ##########################=ECHO %##########################%
SET ##=TYPE %BANNER_FILE% ^& ECHO %##%
