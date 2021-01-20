@echo off
set Program=TicTacToe.dll
if not exist %Program% echo The %Program% does not exist! Build it first by invoking buildCore.cmd.& exit /b -1
call dotnet %Program%