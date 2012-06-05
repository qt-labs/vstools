:: This awesome script creates resource DLLs for the Visual Studio Add-in.
:: This is needed for Visual Studio 2005 where we need a resource DLL for every
:: possible language. This is a bug in VS 2005. :-/

@echo off
setlocal
set PROJECTDIR=Qt4VSAddin
set TARGETDIR=%PROJECTDIR%\Release
pushd "%PROJECTDIR%"
resgen StringResources.resX StringResources.resources
resgen StringResources.de.resX StringResources.de.resources
popd

call :createResource zh-cn
call :createResource zh-Hans 
call :createResource zh-Hant 
call :createResource en
::call :createResource de 
call :createResource es
call :createResource fr
call :createResource it 
call :createResource ja
call :createResource ko
call :createResource ru
endlocal
goto :eof

:createResource
set CULTURE=%1
if not exist %TARGETDIR%\%CULTURE% mkdir %TARGETDIR%\%CULTURE%
set INRESOURCEFILE=%PROJECTDIR%\StringResources.resources
if exist %PROJECTDIR%\StringResources.%CULTURE%.resources set INRESOURCEFILE=%PROJECTDIR%\StringResources.%CULTURE%.resources
echo %CULTURE% %INRESOURCEFILE%
al /nologo /embed:%INRESOURCEFILE% /v:1.0.0.0 /culture:%CULTURE% /out:%TARGETDIR%\%CULTURE%\Qt5VSAddin.resources.dll
goto :eof

