@echo off
setlocal
pushd %~dp0..
set IrisRoot=%CD%
popd
set CmdCompilerDir=%IrisRoot%\ic
set RuntimeDir=%IrisRoot%\IrisRuntime
set BuiltConfig=Debug
set TargetFramework=net6.0
if exist %CmdCompilerDir%\bin\%TargetFramework%\Release\ic.exe set BuiltConfig=Release
set CmdCompilerPath=%CmdCompilerDir%\bin\%BuiltConfig%\%TargetFramework%\ic.exe
set RuntimePath=%RuntimeDir%\bin\%BuiltConfig%\IrisRuntime.dll

if not exist "%CmdCompilerPath%" echo Iris.sln must be built before build.cmd can be run.& exit /b -1
if not exist "%RuntimePath%" echo Cannot find IrisRuntime.dll. This should have been built by Iris.sln.& exit /b -1

copy /y %RuntimePath%
REM Now build the .iris files
set BuildExitCode=0
for %%f in (%~dp0*.iris) do call :CompileFile "%%f"
exit /b %BuildExitCode%

:CompileFile
if NOT "%BuildExitCode%"=="0" goto :EOF
call %CmdCompilerPath% %1
set BuildExitCode=%ERRORLEVEL%