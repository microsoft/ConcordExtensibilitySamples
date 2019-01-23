@echo off
setlocal

if NOT "%VSINSTALLDIR%"=="" goto InDevPrompt

set x86ProgramFiles=%ProgramFiles(x86)%
if "%x86ProgramFiles%"=="" set x86ProgramFiles=%ProgramFiles%
set VSWherePath=%x86ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe
if NOT exist "%VSWherePath%" echo ERROR: Could not find vswhere.exe (%VSWherePath%). Ensure that Visual Studio 2017 version 15.5 or newer is installed. & exit /b -1 

for /f "usebackq tokens=1 delims=" %%a in (`"%VSWherePath%" -version [15.0, -prerelease -property installationPath`) do call :ProcessIDE "%%a"
if NOT "%VSINSTALLDIR%"=="" goto InDevPrompt

echo ERROR: Unable to find a Visual Studio install.
exit /b -1

:InDevPrompt
set _buildproj=%~dp0BuildAndTest.proj
set _buildlog=%~dp0msbuild.log
set MSBuildCmd=msbuild "%_buildproj%" /nologo /maxcpucount /nodeReuse:false %*

echo %MSBuildCmd%
call %MSBuildCmd%
set BUILDERRORLEVEL=%ERRORLEVEL%

echo Build Exit Code = %BUILDERRORLEVEL%
exit /b %BUILDERRORLEVEL%

:ProcessIDE
if NOT "%VSINSTALLDIR%"=="" goto :EOF
if NOT exist "%~1\Common7\Tools\VsDevCmd.bat" goto :EOF
echo Using Visual Studio from %1
call "%~1\Common7\Tools\VsDevCmd.bat"
goto :EOF
