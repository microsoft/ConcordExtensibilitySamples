// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler.FrontEnd;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace IrisCompiler.BackEnd
{
    /// <summary>
    /// Emitter implemetation for generating a PE file.
    /// For now, we'll output textual CIL and use ILASM to generate the actual PE file.  In the
    /// future, we may generate the PE file directly.
    /// </summary>
    public class PeEmitter : IDisposable, IEmitter
    {
        private static int s_nextTempFile;

        private TextEmitter _textEmitter;
        private string _ilFile;
        private string _outputFile;
        private CompilationFlags _flags;
        private bool _deleteOutputOnClose;

        /// <summary>
        /// Initializes a new instance of the PeEmitter class.
        /// Use this constructor to when saving the PE file to disk
        /// </summary>
        /// <param name="outputFile">Path to output file</param>
        /// <param name="flags">Compiler flags</param>
        public PeEmitter(string outputFile, CompilationFlags flags)
        {
            _ilFile = GetPathToTempFile("il");
            _textEmitter = new TextEmitter(_ilFile);

            _outputFile = outputFile;
            _flags = flags;
        }

        /// <summary>
        /// Initializes a new instance of the PeEmitter class.
        /// Use this constructor to when generating an in-memory PE file
        /// </summary>
        /// <param name="flags">Compiler flags</param>
        public PeEmitter(CompilationFlags flags)
            : this(GetPathToTempFile(flags.HasFlag(CompilationFlags.WriteDll) ? "dll" : "exe"), flags)
        {
            _deleteOutputOnClose = true;
        }

        ~PeEmitter()
        {
            Dispose(false);
        }

        public void Flush()
        {
            if (_textEmitter != null)
            {
                _textEmitter.Flush();
                _textEmitter.Dispose();
                _textEmitter = null;

                string platform = _flags.HasFlag(CompilationFlags.Platform32) ? " /32BITPREFERRED" : " /X64";
                string debug = _flags.HasFlag(CompilationFlags.NoDebug) ? string.Empty : " /DEBUG";
                string dll = _flags.HasFlag(CompilationFlags.WriteDll) ? " /DLL" : string.Empty;

                string mscorlibPath;
                string frameworkDir;
                string ilasmPath;

                // Find the path to ilasm.exe
                if (_flags.HasFlag(CompilationFlags.NetCore))
                {
                    string thisDir = Path.GetDirectoryName(GetType().Assembly.Location);
                    ilasmPath = Path.Combine(thisDir, "ilasm.exe");

                    if (!File.Exists(ilasmPath))
                        throw new FileNotFoundException("ilasm.exe cannot be found, make sure netcore ilasm.exe is present in the same directory as IrisCompiler.dll");

                    if (!string.IsNullOrEmpty(debug)) // netcore ilasm requires explicitly specifying pdb format
                        debug += " /PDBFMT=PORTABLE";
                }
                else
                {
                    mscorlibPath = typeof(object).Assembly.Location;
                    frameworkDir = Path.GetDirectoryName(mscorlibPath);
                    ilasmPath = Path.Combine(frameworkDir, "ilasm.exe");
                }

                // Invoke ilasm to convert the textual CIL into a PE file.
                Process process = new Process();
                process.StartInfo.FileName = ilasmPath;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(_outputFile);
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.Arguments = string.Format(
                    @"{0}{1}{2}{3} /OUTPUT={4}",
                    _ilFile,
                    platform,
                    debug,
                    dll,
                    _outputFile);

                process.Start();
                process.WaitForExit();
            }
        }

        public byte[] GetPeBytes()
        {
            return File.ReadAllBytes(_outputFile);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static string GetPathToTempFile(string extension)
        {
            for (; ;)
            {
                string tempFileName = string.Format("ICF{0}.{1}", s_nextTempFile++, extension);
                string path = Path.Combine(Path.GetTempPath(), tempFileName);
                if (!File.Exists(path))
                    return path;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _textEmitter != null)
                _textEmitter.Dispose();

            DeleteFile(_ilFile);
            if (_deleteOutputOnClose)
                DeleteFile(_outputFile);
        }

        private static void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (FileNotFoundException)
            {
                // Don't care if the file wasn't writtn
            }
            catch (UnauthorizedAccessException)
            {
                // Eat UnauthorizedAccessException
            }
        }

        public void Store(IrisType type)
        {
            _textEmitter.Store(type);
        }

        public void BeginMethod(string name, IrisType returnType, Variable[] parameters, Variable[] locals, bool entryPoint)
        {
            _textEmitter.BeginMethod(name, returnType, parameters, locals, entryPoint);
        }

        public void BeginProgram(string name, IEnumerable<string> references)
        {
            _textEmitter.BeginProgram(name, references);
        }

        public void BranchCondition(Operator condition, int i)
        {
            _textEmitter.BranchCondition(condition, i);
        }

        public void BranchFalse(int i)
        {
            _textEmitter.BranchFalse(i);
        }

        public void BranchTrue(int i)
        {
            _textEmitter.BranchTrue(i);
        }

        public void Call(Symbol methodSymbol)
        {
            _textEmitter.Call(methodSymbol);
        }

        public void DeclareGlobal(Symbol symbol)
        {
            _textEmitter.DeclareGlobal(symbol);
        }

        public void Dup()
        {
            _textEmitter.Dup();
        }

        public void EmitLineInfo(SourceRange range, string filePath)
        {
            _textEmitter.EmitLineInfo(range, filePath);
        }

        public void EmitMethodLanguageInfo()
        {
            _textEmitter.EmitMethodLanguageInfo();
        }

        public void EndMethod()
        {
            _textEmitter.EndMethod();
        }

        public void EndProgram()
        {
            _textEmitter.EndProgram();
        }

        public void Goto(int i)
        {
            _textEmitter.Goto(i);
        }

        public void InitArray(Symbol arraySymbol, SubRange subRange)
        {
            _textEmitter.InitArray(arraySymbol, subRange);
        }

        public void Label(int i)
        {
            _textEmitter.Label(i);
        }

        public void Load(IrisType type)
        {
            _textEmitter.Load(type);
        }

        public void LoadElement(IrisType elementType)
        {
            _textEmitter.LoadElement(elementType);
        }

        public void LoadElementAddress(IrisType elementType)
        {
            _textEmitter.LoadElementAddress(elementType);
        }

        public void NoOp()
        {
            _textEmitter.NoOp();
        }

        public void Operator(Operator opr)
        {
            _textEmitter.Operator(opr);
        }

        public void Pop()
        {
            _textEmitter.Pop();
        }

        public void PushArgument(int i)
        {
            _textEmitter.PushArgument(i);
        }

        public void PushArgumentAddress(int i)
        {
            _textEmitter.PushArgumentAddress(i);
        }

        public void PushGlobal(Symbol symbol)
        {
            _textEmitter.PushGlobal(symbol);
        }

        public void PushGlobalAddress(Symbol symbol)
        {
            _textEmitter.PushGlobalAddress(symbol);
        }

        public void PushIntConst(int i)
        {
            _textEmitter.PushIntConst(i);
        }

        public void PushLocal(int i)
        {
            _textEmitter.PushLocal(i);
        }

        public void PushLocalAddress(int i)
        {
            _textEmitter.PushLocalAddress(i);
        }

        public void PushString(string s)
        {
            _textEmitter.PushString(s);
        }

        public void StoreArgument(int i)
        {
            _textEmitter.StoreArgument(i);
        }

        public void StoreElement(IrisType elementType)
        {
            _textEmitter.StoreElement(elementType);
        }

        public void StoreGlobal(Symbol symbol)
        {
            _textEmitter.StoreGlobal(symbol);
        }

        public void StoreLocal(int i)
        {
            _textEmitter.StoreLocal(i);
        }
    }
}
