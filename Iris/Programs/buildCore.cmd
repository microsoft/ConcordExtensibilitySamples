@echo off
setlocal
set CmdCompilerDir=%~dp0..\ic
set RuntimeDir=%~dp0..\IrisRuntime
set BuiltConfig=Debug
set TargetFramework=netcoreapp3.1
if exist %CmdCompilerDir%\bin\%TargetFramework%\Release\icCore.exe set BuiltConfig=Release
set CmdCompilerPath=%CmdCompilerDir%\bin\%BuiltConfig%\%TargetFramework%\icCore.exe
set RuntimePath=%RuntimeDir%\bin\%BuiltConfig%\IrisRuntime.dll

if not exist %CmdCompilerPath% echo IrisCore.sln must be built before buildCore.cmd can be run.& exit /b -1
if not exist %CmdCompilerPath% echo Cannot find IrisRuntime.dll. This should have been built by IrisCore.sln.& exit /b -1

copy /y %RuntimePath% %~dp0

REM Now build the .iris files
for %%f in (%~dp0*.iris) do %CmdCompilerPath% %%f