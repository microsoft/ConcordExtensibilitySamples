// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler;
using IrisCompiler.BackEnd;
using IrisCompiler.FrontEnd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FrontEndTest
{
    public sealed class GlobalSymbolList
    {
        private List<Tuple<string, IrisType>> _symbols = new List<Tuple<string, IrisType>>();

        public void Add(string name, IrisType type)
        {
            _symbols.Add(new Tuple<string, IrisType>(name, type));
        }

        public IEnumerable<Tuple<string, IrisType>> Items
        {
            get
            {
                return _symbols;
            }
        }
    }

    internal sealed class TestCompilerContext : CompilerContext
    {
        private MemoryStream _input;
        private StreamReader _reader;
        private MemoryStream _output;

        private TestCompilerContext(
            MemoryStream input,
            StreamReader reader,
            MemoryStream output,
            IEmitter emitter,
            CompilationFlags flags)
            : base("FakeFile.iris", reader, emitter, flags)
        {
            _input = input;
            _reader = reader;
            _output = output;
        }

        public static TestCompilerContext Create(string compiland, GlobalSymbolList globals, CompilationFlags flags)
        {
#if NETCOREAPP
            flags |= CompilationFlags.NetCore;
#endif

            byte[] buffer = Encoding.Default.GetBytes(compiland);
            MemoryStream input = new MemoryStream(buffer);
            StreamReader reader = new StreamReader(input);
            MemoryStream output = new MemoryStream();
            TextEmitter emitter = new TextEmitter(output);

            TestCompilerContext testContext = new TestCompilerContext(input, reader, output, emitter, flags);
            if (globals != null)
            {
                foreach (var symbol in globals.Items)
                    testContext.SymbolTable.Add(symbol.Item1, symbol.Item2, StorageClass.Global, null);
            }

            return testContext;
        }

        public void ParseProgram()
        {
            AddIntrinsics();

            // Now parse the program
            Translator.TranslateInput();
        }

        public void ParseExpression()
        {
            Translator.TranslateExpression();
        }

        public void ParseStatement()
        {
            Translator.TranslateStatement();
        }

        public string GetCompilerOutput()
        {
            Emitter.Flush();

            _output.Seek(0, SeekOrigin.Begin);
            byte[] resultBuffer = _output.GetBuffer();
            return Encoding.Default.GetString(resultBuffer, 0, (int)_output.Length);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _output.Dispose();
                _reader.Dispose();
                _input.Dispose();
            }
        }
    }
}
