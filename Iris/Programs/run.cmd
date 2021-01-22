@echo off
set Program=TicTacToe.dll
if not exist "%~dp0%Program%" echo The %Program% does not exist! Build it first by invoking build.cmd.& exit /b -1
call dotnet %~dp0%Program%