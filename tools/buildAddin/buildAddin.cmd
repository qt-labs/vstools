@echo off
setlocal
set CONFIGSCRIPT=buildAddin_config.cmd
if exist %CONFIGSCRIPT% goto configScriptAvailable
echo %CONFIGSCRIPT% doesn't exist. Please create this file.
echo There is an example called %CONFIGSCRIPT%_example.
echo Use this as template.
goto :eof

:configScriptAvailable
call %CONFIGSCRIPT%
cd /d %DEV_DIR%
if exist VS*_FAILED.txt del VS*_FAILED.txt
if exist build*.log del build*.log
set IBMAKEOPTIONS=/Rebuild
if "%VS2008%"=="yes" call :buildCommonStuff
if errorlevel 1 exit /b %ERRORLEVEL%
if "%VS2005%"=="yes" call :buildVSI 2005
if "%VS2008%"=="yes" call :buildVSI 2008
if "%VS2010%"=="yes" call :buildVSI 2010
if errorlevel 1 exit /b %ERRORLEVEL%
if "%BUILDINSTALLER%"=="yes" (
    if not exist VS*_FAILED.txt call :buildInstaller
)
endlocal
goto :eof

:buildCommonStuff
    setlocal
    set LOGFILE=%DEV_DIR%\buildcommon.log
    set FAILEDFILE=COMMON_FAILED.txt
    set QMAKESPEC=win32-msvc2008
    set PATH=%QTDIR%\bin;%PATH%
    call :setenv2008

    echo.
    call :msg Building common stuff
    call :msg =============================================================
    echo.
    call :msg qmakewrapper
    pushd vs2008\qtvstools\Qt4VS2003\ComWrappers\qmakewrapper
    qmake                                                                  >> %LOGFILE% 2>&1
    jom /nologo clean release                                              >> %LOGFILE% 2>&1
    if errorlevel 1 exit /b %ERRORLEVEL%
    popd

    copy vs2008\qtvstools\Qt4VS2003\ComWrappers\qmakewrapper\qmakewrapper1Lib.dll vs2005\qtvstools\Qt4VS2003\ComWrappers\qmakewrapper\qmakewrapper1Lib.dll 
::    copy vs2008\qtvstools\Qt4VS2003\ComWrappers\qmakewrapper\release\qmakewrapper1.dll vs2005\qtvstools\Qt4VS2003\ComWrappers\qmakewrapper\release\qmakewrapper1.dll

    call :msg qtappwrapper
    pushd vs2008\qtvstools\tools\qtappwrapper
    devenv qtappwrapper2008.sln /useenv /Clean Release                     >> %LOGFILE% 2>&1
    devenv qtappwrapper2008.sln /useenv /Build Release                     >> %LOGFILE% 2>&1
    if errorlevel 1 exit /b %ERRORLEVEL%
    popd
    call :logseparator

    call :msg qrceditor
    pushd vs2008\qtvstools\tools\qrceditor
    qmake                                                                  >> %LOGFILE% 2>&1
    jom /nologo clean release                                              >> %LOGFILE% 2>&1
    if errorlevel 1 exit /b %ERRORLEVEL%
    popd
    call :logseparator
    endlocal
goto :eof

:buildVSI
    setlocal
    :: set the right environment 'n stuff
    set LOGFILE=%DEV_DIR%\build%1.log
    set FAILEDFILE=VS%1_FAILED.txt
    set QMAKESPEC=win32-msvc%1
    set PATH=%QTDIR%\bin;%PATH%
    call :setenv%1
    
    echo.
    call :msg Building the Visual Studio Add-in for Visual Studio %1
    call :msg =============================================================
    echo QMAKESPEC=%QMAKESPEC%                                             >> %LOGFILE%
    echo QTDIR=%QTDIR%                                                     >> %LOGFILE%

::    call :logseparator
::    call :msg building the COMWrapper
::    pushd vs%1\qtvstools\Qt4VS2003\ComWrappers\FormEditor
::    qmake -tp vc                                                           >> %LOGFILE% 2>&1
::    call ibmake release %IBMAKEOPTIONS%                                    >> %LOGFILE% 2>&1
::    popd
::    
::    :: check if COMWrapper has been built
::    if not exist vs%1\qtvstools\Qt4VS2003\ComWrappers\FormEditor\release\formeditor1.dll (
::        touch %FAILEDFILE%
::        goto :stopthisthing
::    )

    call :logseparator
    call :msg building the VS Add-in main part
    pushd vs%1\qtvstools\Qt4VS2003
    call createCultureResources.cmd
    devenv Qt4VSAddin%1.sln /useenv /Clean Release                         >> %LOGFILE% 2>&1
    devenv Qt4VSAddin%1.sln /useenv /Build Release                         >> %LOGFILE% 2>&1
    if errorlevel 1 exit /b %ERRORLEVEL%
    popd

    :: check if main part has been built
    if not exist vs%1\qtvstools\Qt4VS2003\Qt4VSAddin\Release\Qt5VSAddin.dll (
        touch %FAILEDFILE%
        goto :stopthisthing
    )

    call :logseparator
    call :msg collecting installer files
    pushd vs%1\qtvstools\Qt4VS2003
    set COLLECTOPTIONS=
    if "%1" NEQ "2008" set COLLECTOPTIONS=--addin
    call collectInstallerFiles.bat %COLLECTOPTIONS%
    popd

    :stopthisthing
    endlocal
goto :eof

:setenv2005
    call "%VSVARS2005%"
goto :eof

:setenv2008
    call "%VSVARS2008%"
goto :eof

:setenv2010
    call "%VSVARS2010%"
goto :eof

:msg
    echo %*
    echo %* >> %LOGFILE%
goto :eof

:logseparator
    echo. >> %LOGFILE%
    echo ----------------------------------------------------------- >> %LOGFILE%
    echo. >> %LOGFILE%
goto :eof

:buildInstaller
    echo.
    echo Calling iwmake...
    setlocal
    pushd %DEV_DIR%\mkdist\installers\win-binary
    if exist log.txt del log.txt
    call iwmake addin7x
    popd
    endlocal
goto :eof

