// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler;
using IrisCompiler.BackEnd;
using IrisCompiler.FrontEnd;
using System;
using System.IO;

namespace ic
{
    public class CmdLineCompilerContext : CompilerContext
    {
        private Stream _inputFile;
        private StreamReader _reader;
        private IEmitter _emitter;

        protected CmdLineCompilerContext(
            string sourcePath,
            Stream inputFile,
            StreamReader sourceReader,
            IEmitter emitter,
            CompilationFlags flags)
            : base(sourcePath, sourceReader, emitter, flags)
        {
            _inputFile = inputFile;
            _reader = sourceReader;
            _emitter = emitter;
        }

        public static CmdLineCompilerContext Create(string sourcePath, CompilationFlags flags)
        {
            string outputFile;
            IEmitter emitter;

            if (flags.HasFlag(CompilationFlags.Assembly))
            {
                outputFile = Path.ChangeExtension(sourcePath, "il");
                emitter = new TextEmitter(outputFile);
            }
            else if (flags.HasFlag(CompilationFlags.WriteDll))
            {
                outputFile = Path.ChangeExtension(sourcePath, "dll");
                emitter = new PeEmitter(outputFile, flags);
            }
            else
            {
                outputFile = Path.ChangeExtension(sourcePath, "exe");
                emitter = new PeEmitter(outputFile, flags);
            }

            Stream inputFile = File.Open(sourcePath, FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(inputFile);

            return new CmdLineCompilerContext(sourcePath, inputFile, reader, emitter, flags);
        }

        public void DoCompile()
        {
            AddIntrinsics();
            Translator.TranslateInput();

            // Check for errors
            if (ErrorCount > 0)
            {
                foreach (Error e in CompileErrors.List)
                    Console.WriteLine(e);

                Console.WriteLine();
                Console.WriteLine("{0} Compile Error(s).", ErrorCount);
            }
            else
            {
                // Translation successful.  Write out the PE file.
                Console.WriteLine("0 Compile Errors.");
                _emitter.Flush();

                Console.WriteLine("Output file generated.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Dispose();
                _inputFile.Dispose();
                _emitter.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
