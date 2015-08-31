@echo off
setlocal
set CmdCompilerDir=%~dp0..\ic
set RuntimeDir=%~dp0..\IrisRuntime
set BuiltConfig=Debug
if exist %CmdCompilerDir%\bin\Release\ic.exe set BuiltConfig=Release
set CmdCompilerPath=%CmdCompilerDir%\bin\%BuiltConfig%\ic.exe
set RuntimePath=%RuntimeDir%\bin\%BuiltConfig%\IrisRuntime.dll

if not exist %CmdCompilerPath% echo Iris.sln must be built before build.cmd can be run.& exit /b -1
if not exist %CmdCompilerPath% echo Cannot find IrisRuntime.dll. This should have been built by Iris.sln.& exit /b -1

copy /y %RuntimePath% %~dp0

REM Now build the .iris files
for %%f in (%~dp0*.iris) do %CmdCompilerPath% %%f