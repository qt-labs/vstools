@ECHO OFF

SET TOTAL=0
SET FAIL=0
SET RATE=0
SET USER_QUIT=0
SET PAD=..00

:loop
    CLS
    SET RATE=%PAD%%RATE%
    ECHO ################################################################################
    ECHO # Total: %TOTAL%, Failed: %FAIL%...%RATE:~-4,-2%,%RATE:~-2%%%
    ECHO ################################################################################
    IF %USER_QUIT% EQU 1 GOTO quit
    SET /A "TOTAL+=1"
    msbuild %* ^
        /m /bl /v:m /nologo ^
    && (
        DEL last_build_ok.binlog 2> NUL
        COPY msbuild.binlog last_build_ok.binlog > NUL
    ) || (
        SET /A "FAIL+=1"
        COPY msbuild.binlog error_build_%TOTAL%.binlog > NUL
    )
    SET /A "RATE=(FAIL*100*100)/(TOTAL)"
    ECHO ################################################################################
    CHOICE /C QNOP /N /T 1 /D N /M "# [N]ext / [O]pen Log / [P]ause / [Q]uit ?"
    IF %ERRORLEVEL% EQU 3 (
        START "" msbuild.binlog
        PAUSE
    )
    IF %ERRORLEVEL% EQU 4 PAUSE
    SET USER_QUIT=%ERRORLEVEL%
    GOTO :loop
:quit
