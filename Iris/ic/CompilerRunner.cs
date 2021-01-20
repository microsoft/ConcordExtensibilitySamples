// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using IrisCompiler;
using System.IO;

namespace ic
{
    public class CompilerRunner
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Iris managed compiler");
            string sourceFile;
            CompilationFlags flags;
            if (!TryParseArgs(args, out sourceFile, out flags))
            {
                Console.WriteLine("Usage: ic <source file> [Options]");
                Console.WriteLine("Options:");
                Console.WriteLine("   /32       Make 32-bit exe");
                Console.WriteLine("   /64       Make 64-bit exe");
                Console.WriteLine("   /NODEBUG  Don't include debug information or generate .PDB file");
                Console.WriteLine("   /ASM      Write out assembly instead of binary");
                return;
            }

            using (CmdLineCompilerContext context = CmdLineCompilerContext.Create(sourceFile, flags))
            {
                Console.WriteLine("Compiling file {0}...", sourceFile);

                try
                {
                    context.DoCompile();
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("Access to file denied");
                }
            }
        }

        private static bool TryParseArgs(string[] args, out string sourceFile, out CompilationFlags flags)
        {
            sourceFile = null;
            flags = 0;
            if (args.Length < 1)
                return false;

            foreach (string arg in args)
            {
                string normalizedArg = arg.ToUpper();
                switch (normalizedArg)
                {
                    case "/32":
                        flags |= CompilationFlags.Platform32;
                        break;
                    case "/64":
                        flags |= CompilationFlags.Platform64;
                        break;
                    case "/NODEBUG":
                        flags |= CompilationFlags.NoDebug;
                        break;
                    case "/ASM":
                        flags |= CompilationFlags.Assembly;
                        break;
                    default:
                        if (normalizedArg.StartsWith("/"))
                        {
                            Console.WriteLine("Unrecognized option {0}", normalizedArg);
                            return false;
                        }
                        if (!File.Exists(arg))
                        {
                            Console.WriteLine("Source file '{0}' not found", arg);
                            return false;
                        }
                        if (sourceFile != null)
                        {
                            Console.WriteLine("Multiple source files specified.  Only one file is supported");
                            return false;
                        }
                        sourceFile = arg;
                        break;
                }
            }

            if (flags.HasFlag(CompilationFlags.Platform32) && flags.HasFlag(CompilationFlags.Platform64))
            {
                Console.WriteLine("Both 32-bit and 64-bit platforms specified.  Only one platform is supported");
                return false;
            }

            if (!flags.HasFlag(CompilationFlags.Platform32) && !flags.HasFlag(CompilationFlags.Platform64))
                flags |= CompilationFlags.Platform32;

            if (sourceFile == null)
                return false;


#if NETCOREAPP
            flags |= CompilationFlags.NetCore;
            flags |= CompilationFlags.WriteDll; // force running application through dotnet
#endif

            return true;
        }
    }
}
