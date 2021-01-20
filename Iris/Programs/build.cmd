REM @echo off
setlocal
pushd %~dp0..
set IrisRoot=%CD%
popd
set CmdCompilerDir=%IrisRoot%\ic
set RuntimeDir=%IrisRoot%\IrisRuntime
set BuiltConfig=Debug
set TargetFramework=net5.0
if exist %CmdCompilerDir%\bin\%TargetFramework%\Release\ic.exe set BuiltConfig=Release
set CmdCompilerPath=%CmdCompilerDir%\bin\%BuiltConfig%\%TargetFramework%\ic.exe
set RuntimePath=%RuntimeDir%\bin\%BuiltConfig%\IrisRuntime.dll

if not exist "%CmdCompilerPath%" echo Iris.sln must be built before build.cmd can be run.& exit /b -1
if not exist "%RuntimePath%" echo Cannot find IrisRuntime.dll. This should have been built by Iris.sln.& exit /b -1

copy /y %RuntimePath%
REM Now build the .iris files

for %%f in (%~dp0*.iris) do %CmdCompilerPath% %%f