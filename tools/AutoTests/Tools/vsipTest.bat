start /wait GetSnapshot.bat

"C:\Program Files\Nokia\Qt VS Integration\uninst.exe" /S

PING 1.1.1.1 -n 1 -w 60000 >NUL

start /wait InstallIntegration.bat

PING 1.1.1.1 -n 1 -w 60000 >NUL

set PATH=C:\Program Files\Microsoft Visual Studio 9.0\Common7\IDE\;%PATH%

devenv

PING localhost -n 1 -w 10000 >NUL

UDPClient.exe