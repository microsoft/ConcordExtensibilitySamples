// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace IrisCompiler
{
    [Flags]
    public enum CompilationFlags
    {
        None = 0,

        /// <summary>
        /// Write a .dll file instead of a .exe
        /// </summary>
        WriteDll = 1,

        /// <summary>
        /// Write a 32-bit preferred PE file
        /// </summary>
        Platform32 = 2,

        /// <summary>
        /// Write a 64-bit preferred PE file.
        /// </summary>
        Platform64 = 4,

        /// <summary>
        /// Don't emit debug information.
        /// </summary>
        NoDebug = 8,

        /// <summary>
        /// Instead of emitting a PE file, compile to CIL assembly.
        /// </summary>
        Assembly = 16,

        /// <summary>
        /// Target .NET Core instead of .NET Framework.
        /// This flag is set based on the target framework of the compiler runner.
        /// </summary>
        NetCore = 32,
    }
}
