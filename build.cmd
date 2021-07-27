@echo off
setlocal

if "%~1"=="/?" goto :Help
if "%~1"=="-?" goto :Help
if "%~1"=="-h" goto :Help
set AdditionalBuildArgs=%*

if NOT "%VSINSTALLDIR%"=="" goto InDevPrompt

set x86ProgramFiles=%ProgramFiles(x86)%
if "%x86ProgramFiles%"=="" set x86ProgramFiles=%ProgramFiles%
set VSWherePath=%x86ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe
if NOT exist "%VSWherePath%" echo ERROR: Could not find vswhere.exe (%VSWherePath%). Ensure that Visual Studio 2022 is installed. & exit /b -1 

for /f "usebackq tokens=1 delims=" %%a in (`"%VSWherePath%" -version [17.0, -prerelease -requires Microsoft.VisualStudio.Workload.NativeDesktop;Microsoft.VisualStudio.Workload.VisualStudioExtension;Microsoft.VisualStudio.Workload.ManagedDesktop -property installationPath`) do call :ProcessIDE "%%a"
if NOT "%VSINSTALLDIR%"=="" goto InDevPrompt

echo ERROR: Unable to find a Visual Studio 2022 or newer install.
exit /b -1

:InDevPrompt
pushd %~dp0
del /s msbuild.log 2>NUL
set BuildError=
call :SetNugetPath nuget.exe
call :RestoreFromSLN Iris\Iris.sln
call :RestoreFromSLN HelloWorld\cs\HelloWorld.sln
call :RestoreFromPackagesConfig CppCustomVisualizer\dll\packages.config CppCustomVisualizer\packages
call :RestoreFromPackagesConfig HelloWorld\Cpp\dll\packages.config HelloWorld\Cpp\packages
call :Build Iris\Iris.sln Iris "Any CPU"
call :Build HelloWorld\cs\HelloWorld.sln CsHelloWorld "Any CPU"
call :Build HelloWorld\cpp\HelloWorld.sln CppHelloWorld x64
call :Build CppCustomVisualizer\CppCustomVisualizer.sln CppCustomVisualizer x64

if NOT "%BuildError%"=="" exit /b -1
echo build.cmd completed successfully.
exit /b 0

:RestoreFromPackagesConfig
if NOT "%BuildError%"=="" goto :EOF
set NugetCmd="%NUGET_EXE%" restore %1 -PackagesDirectory %2
echo %NugetCmd%
call %NugetCmd%
if NOT "%ERRORLEVEL%"=="0" echo ERROR: build.cmd: Restoring %1 failed.& set BuildError=1
goto :EOF

:RestoreFromSLN
if NOT "%BuildError%"=="" goto :EOF
set NugetCmd="%NUGET_EXE%" restore %1
echo %NugetCmd%
call %NugetCmd%
if NOT "%ERRORLEVEL%"=="0" echo ERROR: build.cmd: Restoring %1 failed.& set BuildError=1
goto :EOF

:Build
if NOT "%BuildError%"=="" goto :EOF
REM NOTE: To enable binary logs, add: `/bl:%2.binlog` after `%1`
set MSBuildCmd=msbuild.exe %1 /nologo /maxcpucount /nodeReuse:false /p:Platform=%3 %AdditionalBuildArgs%

echo %MSBuildCmd%
call %MSBuildCmd%
if NOT "%ERRORLEVEL%"=="0" echo ERROR: build.cmd: Building %1 failed.& set BuildError=1
goto :EOF

:SetNugetPath
set NUGET_EXE=%~$PATH:1
if NOT "%NUGET_EXE%"=="" goto :EOF
if exist obj\nuget.exe set NUGET_EXE=%~dp0obj\nuget.exe& goto :EOF

if not exist obj mkdir obj
call powershell.exe -NoProfile -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile obj\nuget.exe"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: build.cmd: Downloading nuget.exe failed.& set BuildError=1& goto :EOF
if NOT exist obj\nuget.exe echo ERROR: build.cmd: nuget.exe is unexpectedly missing.& set BuildError=1& goto :EOF

set NUGET_EXE=%~dp0obj\nuget.exe 
goto :EOF

:ProcessIDE
if NOT "%VSINSTALLDIR%"=="" goto :EOF
if NOT exist "%~1\Common7\Tools\VsDevCmd.bat" goto :EOF
echo Using Visual Studio from %1
call "%~1\Common7\Tools\VsDevCmd.bat"
goto :EOF

:Help
echo Build.cmd [Additional msbuild arguments]
echo.
echo This script restores and builds all projects in this repo.
echo.
echo Example usage: build.cmd /p:Configuration=Debug