## Summary

This project packages up IrisExtension and its dependencies for use in Visual Studio Code, Visual Studio Linux scenarios, and Visual Studio for Mac.

More information about these scenarios is in the [wiki](https://github.com/microsoft/ConcordExtensibilitySamples/wiki/Support-for-cross-platform-.NET-scenarios).

## Note about ilasm

This project currently uses ilasm to create PE (.dll/.exe) files. This is inconvenient for cross-platform scenarios because ilasm is a native executable.
Because of this, this sample currently just runs on windows-x64 and linux-x64. It is recommended that a production EE consider using
[System.Reflection.Emit](https://www.nuget.org/packages/System.Reflection.Emit/) to avoid this native dependency. Thus the same extension could run on all platforms.

## Using this project to target Linux

Steps for using this project to debug on Linux

#### Computer setup
Here are the one time steps to prep your computer for this scenario.

1. Install Windows 10 May 2019 (1903) or later
2. Install [WSL2 (Windows Subsystem for Linux v2)](https://aka.ms/wsl2) and the [distribution](https://aka.ms/wslstore) of your choice.
3. Install Visual Studio
    * 2019 vesion 16.9 or newer is required. If doing this before 16.9 is released, use the [Preview version](https://visualstudio.microsoft.com/vs/preview/). 
    * Enable support for WSL 2 -- this is an optional feature. In the Visual Studio installer, switch to the 'Indivual components' tab, and search for '.NET Core Debugging with WSL 2' and enable it.
4. Install Visual Studio Code on Windows
5. In Visual Studio Code, install the following extensions:
    * C#
    * Remote - WSL
6. Open a bash prompt and install the .NET Core SDK into your Linux distribution (see [instructions](https://docs.microsoft.com/dotnet/core/install/linux)).
7. Verify C# debugging is working on Linux in VS Code:
    * From a bash prompt, make a C# project -- `dotnet new console -o HelloWorld`
    * Open VS Code on Windows, and click the 'Remote Explorer' icon on the right side, this should show the WSL targets
    * Click to connect to WSL
    * Select the path to your C# project
    * Wait for the C# extension to initialize, you should eventually see a prompt asking if you want to add "Required assets to bulid and debug". Click "yes".
    * Set a breakpoint and hit F5, make sure debugging works

#### Prepare Visual Studio for Linux debugging

1. In Visual Studio, set this project (xplat-package) as the startup project
2. Build the solution
3. Open a bash prompt and run `ls -d ~/.vscode-server/extensions/ms-dotnettools.csharp-*` to see what version of the C# extension you have installed.
4. Open Properties\launchSettings.json and replace '1.2.3' with the real version of the C# extension
5. In the debug location toolbar, click the down arrow to bring up the drop down menu, and select the WSL 2 configuration. ![Debug location toolbar dropdown](https://raw.github.com/wiki/Microsoft/ConcordExtensibilitySamples/images/wsl2.png)
6. Switch back to your bash prompt
7. Run CreateLinkFile.sh. Example: `/mnt/c/MyProjects/ConcordExtensibilitySamples/Iris/xplat-package/bin/Debug/linux-x64/CreateLinkFile.sh` (where 'c/MyProjects' should be replaced with the actual root)

#### Inner loop
1. Setup the 'debuggee' IDE
   * Start VS Code on Windows
   * Open the WSL view of the 'Programs' folder (example: '/mnt/c/MyProjects/ConcordExtensibilitySamples/Iris/Programs' where 'c/MyProjects' should be replaced with the actual root)
2. Hit F5 in Visual Studio, this should launch vsdbg-ui inside of WSL.
3. Set any breakpoint you want in IrisExtension or IrisCompiler
4. Hit F5 in Visual Studio code. Visual Studio Code should connect to the running vsdbg-ui process, and it should stop at entry point to the TicTacToe program.

## Using this project to target Windows

If you want to target Visual Studio Code .NET Debugging on Windows, the steps are fairly similar to the about steps on Linux. But make the following changes:
1. WSL Support is not required. It certainly will not cause any problems to have it, but it isn't needed.
2. Set the debug profile for the xplat-package project (this project) to Windows
3. Run CreateLinkFile.cmd instead of .sh. Example: c:\MyProjects\ConcordExtensibilitySamples\Iris\xplat-package\bin\Debug\win-x64\CreateLinkFile.cmd (where 'c:\MyProjects' should be replaced with the actual root)
4. In Visual Studio Code, open the 'Iris\Programs' folder from the Windows file system instead of the Linux.