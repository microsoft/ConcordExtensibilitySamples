// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler.BackEnd;
using IrisCompiler.FrontEnd;
using IrisCompiler.Import;
using System;
using System.IO;
using System.Linq;

namespace IrisCompiler
{
    /// <summary>
    /// Abstract class containing the context information needed by the compiler.
    /// Users of the compiler define its behavior by overriding this class.
    /// </summary>
    public abstract class CompilerContext : IDisposable
    {
        public readonly string FilePath;
        public readonly CompilationFlags Flags;
        public readonly ErrorList CompileErrors;
        public readonly Importer Importer;
        public readonly SymbolTable SymbolTable;
        public readonly Lexer Lexer;
        public readonly IEmitter Emitter;

        private Translator _translator;
        private bool _ownsImporter;

        protected CompilerContext(string filePath, StreamReader inputReader, Importer importer, IEmitter emitter, CompilationFlags flags)
        {
            FilePath = filePath;
            Flags = flags;
            Emitter = emitter;
            CompileErrors = new ErrorList();
            Importer = importer;
            SymbolTable = new SymbolTable();
            Lexer = Lexer.Create(inputReader, CompileErrors);
        }

        protected CompilerContext(string filePath, StreamReader inputReader, IEmitter emitter, CompilationFlags flags)
            : this(filePath, inputReader, new Importer(), emitter, flags)
        {
            _ownsImporter = true;
        }

        /// <summary>
        /// Create the Translator instance.  Subclasses can customize parsing by deriving from
        /// Translator and returning their custom instance here.
        /// </summary>
        /// <returns>Translator instance</returns>
        protected virtual Translator CreateTranslator()
        {
            return new Translator(this);
        }

        ~CompilerContext()
        {
            Dispose(false);
        }

        public Translator Translator
        {
            get
            {
                if (_translator == null)
                    _translator = CreateTranslator();

                return _translator;
            }
        }

        public int ErrorCount
        {
            get
            {
                return CompileErrors.Count;
            }
        }

        public string FirstError
        {
            get
            {
                return CompileErrors.Count == 0 ?
                    string.Empty : CompileErrors.List.First().ToString();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Add intrinsic methods and globals to the symbol table
        /// </summary>
        public void AddIntrinsics()
        {
            ImportedModule mscorlib = ReferenceMscorlib();
            ImportedModule runtime = ReferenceExternal("IrisRuntime.dll");

            IrisType tVoid = IrisType.Void;
            IrisType tString = IrisType.String;
            IrisType tInt = IrisType.Integer;
            IrisType[] noParams = new IrisType[0];

            FilePosition fp = FilePosition.Begin;

            if (mscorlib != null)
            {
                ImportGlobalField(fp, "$.emptystr", mscorlib, "System.String", "Empty");
                ImportMethod(fp, "concat", mscorlib, "System.String", "Concat", false, tString, new IrisType[] { tString, tString });
                ImportMethod(fp, "readln", mscorlib, "System.Console", "ReadLine", false, tString, noParams);
                ImportMethod(fp, "str", mscorlib, "System.Int32", "ToString", true, tString, noParams);
                ImportMethod(fp, "strcmp", mscorlib, "System.String", "Compare", false, tInt, new IrisType[] { tString, tString });
                ImportMethod(fp, "writeln", mscorlib, "System.Console", "WriteLine", false, tVoid, new IrisType[] { tString });
            }

            if (runtime != null)
            {
                ImportMethod(fp, "$.initstrarray", runtime, "IrisRuntime.CompilerServices", "InitStrArray", false, tVoid, new IrisType[] { IrisType.String.MakeArrayType() });
                ImportMethod(fp, "rand", runtime, "IrisRuntime.CompilerServices", "Rand", false, tInt, noParams);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_ownsImporter)
                Importer.Dispose();

            Emitter.Dispose();
        }

        protected virtual ImportedModule ReferenceMscorlib()
        {
            // For now, just use the same mscorlib as used by the compiler.
            string path = typeof(object).Assembly.Location;
            return Importer.ImportModule(path);
        }

        protected virtual ImportedModule ReferenceExternal(string moduleName)
        {
            // Reference an external dll

            // First look in the current directory
            string path = Path.Combine(Environment.CurrentDirectory, moduleName);
            if (!File.Exists(path))
            {
                // If not there, try looking in the same directory as the compiler exe
                string thisExeDir = Path.GetDirectoryName(GetType().Assembly.Location);
                path = Path.Combine(thisExeDir, moduleName);
            }

            if (!File.Exists(path))
            {
                CompileErrors.Add(FilePosition.Begin, string.Format("Cannot find referenced module {0}.", moduleName));
                return null;
            }

            return Importer.ImportModule(path);
        }

        protected void ImportGlobalField(
            FilePosition fp,
            string symbolName,
            ImportedModule module,
            string declaringTypeName,
            string fieldName)
        {
            ImportedType declaringType = module.TryGetTypeByName(declaringTypeName);
            if (declaringType == null)
            {
                CompileErrors.Add(fp, String.Format("Cannot find imported type '{0}'.", declaringTypeName));
                return;
            }

            ImportedField field = declaringType.TryGetPublicStaticField(fieldName);
            if (field == null)
            {
                CompileErrors.Add(fp, String.Format("Cannot find imported field '{0}.{1}'.", declaringTypeName, fieldName));
                return;
            }

            IrisType fieldType = field.FieldType;
            if (fieldType == IrisType.Invalid)
            {
                CompileErrors.Add(fp, String.Format("Type of field '{0}.{1}' is not supported by the language.", declaringTypeName, fieldName));
                return;
            }

            SymbolTable.Add(symbolName, field.FieldType, StorageClass.Global, field);
        }

        protected void ImportMethod(
            FilePosition fp,
            string symbolName,
            ImportedModule module,
            string declaringTypeName,
            string methodName,
            bool instance,
            IrisType returnType,
            IrisType[] paramTypes)
        {
            ImportedType declaringType = module.TryGetTypeByName(declaringTypeName);
            if (declaringType == null)
            {
                CompileErrors.Add(fp, String.Format("Cannot find imported type '{0}'.", declaringTypeName));
                return;
            }

            ImportedMethod importedMethod = declaringType.TryFindMethod(methodName, instance, returnType, paramTypes);
            if (importedMethod == null)
            {
                CompileErrors.Add(fp, String.Format("Cannot find imported function or procedure '{0}.{1}'.", declaringTypeName, methodName));
                return;
            }

            Method method = importedMethod.ConvertToIrisMethod();
            bool containsInvalidType = method.ReturnType == IrisType.Invalid;
            foreach (Variable param in method.GetParameters())
            {
                if (containsInvalidType)
                    break;

                if (param.Type == IrisType.Invalid)
                    containsInvalidType = true;
            }

            if (containsInvalidType)
                CompileErrors.Add(fp, String.Format("The function or procedure '{0}.{1}' contains types that are not supported by the language.", declaringTypeName, methodName));
            else
                SymbolTable.Add(symbolName, method, StorageClass.Global, importedMethod);
        }
    }
}
