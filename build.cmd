@echo off
setlocal

if not defined VisualStudioVersion (
    if defined VS140COMNTOOLS (
        call "%VS140COMNTOOLS%\VsDevCmd.bat"
        goto :EnvSet
    )

    echo Error: build.cmd requires Visual Studio 2015.
    exit /b 1
)

if not exist "%VSSDK140Install%" (
    echo Error: build.cmd requires the Visual Studio SDK to be installed.
    exit /b 1
)

set _buildproj=%~dp0BuildAndTest.proj
set _buildlog=%~dp0msbuild.log

msbuild "%_buildproj%" /nologo /maxcpucount /nodeReuse:false %*
set BUILDERRORLEVEL=%ERRORLEVEL%

echo Build Exit Code = %BUILDERRORLEVEL%
exit /b %BUILDERRORLEVEL%