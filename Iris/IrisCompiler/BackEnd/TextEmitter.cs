// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler.FrontEnd;
using IrisCompiler.Import;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace IrisCompiler.BackEnd
{
    /// <summary>
    /// The TextEmitter allows the IrisCompiler to output the target code as textual
    /// CIL (Common Intermediate Language) that can be compiled into .exe and .pdb files using
    /// ILASM.EXE.
    /// </summary>
    public sealed class TextEmitter : IEmitter
    {
        /// <summary>
        /// String builder used by WriteInstruction and WriteIndentedText.  It is cached here for performance and
        /// should NOT be used by any other methods.
        /// </summary>
        private StringBuilder _stringBuilder;

        private Dictionary<int, string> _methodNameCache = new Dictionary<int, string>();
        private Dictionary<int, string> _globalVariableCache = new Dictionary<int, string>();
        private Stream _stream;
        private TextWriter _writer;
        private string _programName;
        private int _indentLevel;

        public TextEmitter(string outputFile)
            : this()
        {
            _stream = File.Open(outputFile, FileMode.Create, FileAccess.Write);
            _writer = new StreamWriter(_stream);
        }

        public TextEmitter(Stream outputStream)
            : this()
        {
            _writer = new StreamWriter(outputStream);
        }

        private TextEmitter()
        {
            _stringBuilder = new StringBuilder();
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }

            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }
        }

        public void Flush()
        {
            if (_writer != null)
                _writer.Flush();
        }

        public void BeginProgram(string name, IEnumerable<string> references)
        {
            _programName = name;

            // Output standard text that applies to all Iris programs
            _writer.WriteLine();

            foreach (string reference in references)
                _writer.WriteLine(".assembly extern {0} {{ }}", reference);

            _writer.WriteLine(string.Format(".assembly {0} {{ }}", _programName));
            _writer.WriteLine(string.Format(".class public {0}", _programName));
            _writer.WriteLine("{");
            _indentLevel++;
        }

        public void DeclareGlobal(Symbol symbol)
        {
            WriteIndentedText(string.Format(
                ".field public static {0} {1}",
                IrisTypeToCilTypeName(symbol.Type),
                symbol.Name));
        }

        public void EndProgram()
        {
            _indentLevel--;
            _writer.WriteLine("}");
        }

        public void BeginMethod(string name, IrisType returnType, Variable[] parameters, Variable[] locals, bool entryPoint)
        {
            string cilName = string.Format(
                "{0} {1}({2})",
                IrisTypeToCilTypeName(returnType),
                name,
                string.Join(", ", parameters.Select(v => IrisVarToCilVar(v))));

            WriteIndentedText(string.Format(".method public hidebysig static {0} cil managed", cilName));
            WriteIndentedText("{");

            _indentLevel++;

            if (entryPoint)
            {
                WriteIndentedText(".entrypoint");
            }

            if (locals.Length > 0)
            {
                StringBuilder localsBuilder = new StringBuilder();
                localsBuilder.Append(".locals init (");

                int localIndex = 0;
                foreach (Variable var in locals)
                {
                    if (localIndex != 0)
                        localsBuilder.Append(", ");

                    localsBuilder.AppendFormat("[{0}] {1}", localIndex, IrisVarToCilVar(var));
                    localIndex++;
                }

                localsBuilder.Append(")");
                WriteIndentedText(localsBuilder.ToString());
            }
        }

        public void EmitMethodLanguageInfo()
        {
            WriteIndentedText(string.Format(
                ".language '{{{0}}}', '{{{1}}}', '{{{2}}}'",
                Guids.IrisLanguage,
                Guids.IrisVendor,
                Guids.PdbDocumentType));
        }

        public void EmitLineInfo(SourceRange range, string filePath)
        {
            filePath = filePath.Replace(@"\", @"\\");
            WriteIndentedText(string.Format(
                ".line {0},{1} : {2},{3} '{4}'",
                range.Start.Line,
                range.End.Line,
                range.Start.Column,
                range.End.Column,
                filePath));
        }

        public void InitArray(Symbol arraySymbol, SubRange subRange)
        {
            int lowerBound = 0;
            int dimension = 0;

            if (subRange != null)
            {
                lowerBound = subRange.From;
                dimension = subRange.To - subRange.From + 1;
                if (dimension < 0)
                    dimension = 0;
            }

            InitArrayHelper(arraySymbol, lowerBound, dimension);
        }

        private void InitArrayHelper(Symbol arraySymbol, int lowerBound, int dimension)
        {
            IrisType elementType = arraySymbol.Type.GetElementType();
            string cilType = IrisTypeToCilTypeName(elementType);

            PushIntConst(dimension);
            WriteInstruction("newarr", cilType);

            if (arraySymbol.StorageClass == StorageClass.Local)
                StoreLocal(arraySymbol.Location);
            else
                StoreGlobal(arraySymbol);
        }

        public void EndMethod()
        {
            WriteInstruction("ret", null);

            _indentLevel--;
            WriteIndentedText("}");
        }

        public void PushString(string s)
        {
            WriteInstruction("ldstr", s, quoteArgument: true);
        }

        public void PushIntConst(int i)
        {
            switch (i)
            {
                case 0:
                    WriteInstruction("ldc.i4.0", null);
                    break;
                case 1:
                    WriteInstruction("ldc.i4.1", null);
                    break;
                case 2:
                    WriteInstruction("ldc.i4.2", null);
                    break;
                case 3:
                    WriteInstruction("ldc.i4.3", null);
                    break;
                case 4:
                    WriteInstruction("ldc.i4.4", null);
                    break;
                case 5:
                    WriteInstruction("ldc.i4.5", null);
                    break;
                case 6:
                    WriteInstruction("ldc.i4.6", null);
                    break;
                case 7:
                    WriteInstruction("ldc.i4.7", null);
                    break;
                case 8:
                    WriteInstruction("ldc.i4.8", null);
                    break;
                default:
                    if (i <= 255)
                        WriteInstruction("ldc.i4.s", i.ToString());
                    else
                        WriteInstruction("ldc.i4", i.ToString());
                    break;
            }
        }

        public void PushArgument(int i)
        {
            switch (i)
            {
                case 0:
                    WriteInstruction("ldarg.0", null);
                    break;
                case 1:
                    WriteInstruction("ldarg.1", null);
                    break;
                case 2:
                    WriteInstruction("ldarg.2", null);
                    break;
                case 3:
                    WriteInstruction("ldarg.3", null);
                    break;
                default:
                    if (i <= 255)
                        WriteInstruction("ldarg.s", i.ToString());
                    else
                        WriteInstruction("ldarg", i.ToString());
                    break;
            }
        }

        public void PushArgumentAddress(int i)
        {
            if (i <= 255)
                WriteInstruction("ldarga.s", i.ToString());
            else
                WriteInstruction("ldarga", i.ToString());
        }

        public void StoreArgument(int i)
        {
            if (i <= 255)
                WriteInstruction("starg.s", i.ToString());
            else
                WriteInstruction("starg", i.ToString());
        }

        public void PushLocal(int i)
        {
            switch (i)
            {
                case 0:
                    WriteInstruction("ldloc.0", null);
                    break;
                case 1:
                    WriteInstruction("ldloc.1", null);
                    break;
                case 2:
                    WriteInstruction("ldloc.2", null);
                    break;
                case 3:
                    WriteInstruction("ldloc.3", null);
                    break;
                default:
                    if (i <= 255)
                        WriteInstruction("ldloc.s", i.ToString());
                    else
                        WriteInstruction("ldloc", i.ToString());
                    break;
            }
        }

        public void PushLocalAddress(int i)
        {
            if (i <= 255)
                WriteInstruction("ldloca.s", i.ToString());
            else
                WriteInstruction("ldloca", i.ToString());
        }

        public void StoreLocal(int i)
        {
            if (i <= 255)
                WriteInstruction("stloc.s", i.ToString());
            else
                WriteInstruction("stloc", i.ToString());
        }

        public void PushGlobal(Symbol symbol)
        {
            WriteInstruction("ldsfld", GetGlobalVariableFieldName(symbol));
        }

        public void PushGlobalAddress(Symbol symbol)
        {
            WriteInstruction("ldsflda", GetGlobalVariableFieldName(symbol));
        }

        public void StoreGlobal(Symbol symbol)
        {
            WriteInstruction("stsfld", GetGlobalVariableFieldName(symbol));
        }

        public void Dup()
        {
            WriteInstruction("dup", null);
        }

        public void Pop()
        {
            WriteInstruction("pop", null);
        }

        public void NoOp()
        {
            WriteInstruction("nop", null);
        }

        public void LoadElement(IrisType elementType)
        {
            if (elementType == IrisType.Integer)
                WriteInstruction("ldelem.i4", null);
            else if (elementType == IrisType.Boolean)
                WriteInstruction("ldelem.i1", null);
            else
                WriteInstruction("ldelem", IrisTypeToCilTypeName(elementType));
        }

        public void LoadElementAddress(IrisType elementType)
        {
            WriteInstruction("ldelema", IrisTypeToCilTypeName(elementType));
        }

        public void StoreElement(IrisType elementType)
        {
            if (elementType == IrisType.Integer)
                WriteInstruction("stelem.i4", null);
            else if (elementType == IrisType.Boolean)
                WriteInstruction("stelem.i1", null);
            else
                WriteInstruction("stelem.ref", null);
        }

        public void Label(int i)
        {
            _writer.WriteLine(LabelText(i) + ":");
        }

        public void Goto(int i)
        {
            WriteInstruction("br", LabelText(i));
        }

        public void BranchCondition(Operator condition, int i)
        {
            switch (condition)
            {
                case IrisCompiler.Operator.Equal:
                    WriteInstruction("beq", LabelText(i));
                    break;
                case IrisCompiler.Operator.NotEqual:
                    WriteInstruction("bne.un", LabelText(i));
                    break;
                case IrisCompiler.Operator.LessThan:
                    WriteInstruction("blt", LabelText(i));
                    break;
                case IrisCompiler.Operator.LessThanEqual:
                    WriteInstruction("ble", LabelText(i));
                    break;
                case IrisCompiler.Operator.GreaterThan:
                    WriteInstruction("bgt", LabelText(i));
                    break;
                case IrisCompiler.Operator.GreaterThanEqual:
                    WriteInstruction("bge", LabelText(i));
                    break;
                default:
                    throw new InvalidOperationException("Invalid branch condition operator");
            }
        }

        public void BranchTrue(int i)
        {
            WriteInstruction("brtrue", LabelText(i));
        }

        public void BranchFalse(int i)
        {
            WriteInstruction("brfalse", LabelText(i));
        }

        public void Call(Symbol methodSymbol)
        {
            string destination = GetEmittedMethodName(methodSymbol);
            WriteInstruction("call", destination);
        }

        public void Operator(Operator opr)
        {
            switch (opr)
            {
                case IrisCompiler.Operator.Equal:
                    WriteInstruction("ceq", null);
                    break;
                case IrisCompiler.Operator.NotEqual:
                    WriteInstruction("ceq", null);
                    WriteInstruction("ldc.i4.1", null);
                    WriteInstruction("xor", null);
                    break;
                case IrisCompiler.Operator.LessThan:
                    WriteInstruction("clt", null);
                    break;
                case IrisCompiler.Operator.LessThanEqual:
                    WriteInstruction("cgt", null);
                    WriteInstruction("ldc.i4.1", null);
                    WriteInstruction("xor", null);
                    break;
                case IrisCompiler.Operator.GreaterThan:
                    WriteInstruction("cgt", null);
                    break;
                case IrisCompiler.Operator.GreaterThanEqual:
                    WriteInstruction("clt", null);
                    WriteInstruction("ldc.i4.1", null);
                    WriteInstruction("xor", null);
                    break;
                case IrisCompiler.Operator.Add:
                    WriteInstruction("add", null);
                    break;
                case IrisCompiler.Operator.Subtract:
                    WriteInstruction("sub", null);
                    break;
                case IrisCompiler.Operator.Multiply:
                    WriteInstruction("mul", null);
                    break;
                case IrisCompiler.Operator.Divide:
                    WriteInstruction("div", null);
                    break;
                case IrisCompiler.Operator.Modulo:
                    WriteInstruction("rem", null);
                    break;
                case IrisCompiler.Operator.And:
                    WriteInstruction("and", null);
                    break;
                case IrisCompiler.Operator.Or:
                    WriteInstruction("or", null);
                    break;
                case IrisCompiler.Operator.Negate:
                    WriteInstruction("neg", null);
                    break;
                case IrisCompiler.Operator.Not:
                    WriteInstruction("ldc.i4.1", null);
                    WriteInstruction("xor", null);
                    break;
            }
        }

        public void Load(IrisType type)
        {
            if (type == IrisType.Boolean)
                WriteInstruction("ldind.i1", null);
            else if (type == IrisType.Integer)
                WriteInstruction("ldind.i4", null);
            else
                WriteInstruction("ldind.ref", null);
        }

        public void Store(IrisType type)
        {
            if (type == IrisType.Boolean)
                WriteInstruction("stind.i1", null);
            else if (type == IrisType.Integer)
                WriteInstruction("stind.i4", null);
            else
                WriteInstruction("stind.ref", null);
        }

        private static string IrisVarToCilVar(Variable var)
        {
            return string.Format("{0} {1}", IrisTypeToCilTypeName(var.Type), var.Name);
        }

        private static string IrisTypeToCilTypeName(IrisType type)
        {
            if (type.IsPrimitive)
            {
                if (type == IrisType.Integer)
                    return "int32";
                else if (type == IrisType.String)
                    return "string";
                else if (type == IrisType.Boolean)
                    return "bool";
            }
            else if (type.IsArray)
            {
                return IrisTypeToCilTypeName(type.GetElementType()) + "[]";
            }
            else if (type.IsByRef)
            {
                return IrisTypeToCilTypeName(type.GetElementType()) + "&";
            }
            else if (type == IrisType.Void)
            {
                return "void";
            }

            return "_Unknown";
        }

        private static string LabelText(int label)
        {
            return string.Format(CultureInfo.InvariantCulture, "L{0}", label);
        }

        private string GetEmittedMethodName(Symbol methodSymbol)
        {
            string name;
            if (!_methodNameCache.TryGetValue(methodSymbol.Location, out name))
            {
                Method method = methodSymbol.Type as Method;
                if (method == null || _programName == null)
                {
                    // "method" will be null if the symbol is undefined.
                    // There should already be a compile error emitted for the undefined symbol.
                    // "m_programName" is null in some unit testing cases.
                    name = "_Unknown";
                }
                else if (methodSymbol.ImportInfo != null)
                {
                    ImportedMethod importedMethod = (ImportedMethod)methodSymbol.ImportInfo;
                    StringBuilder nameBuilder = new StringBuilder();
                    if (!importedMethod.IsStatic)
                        nameBuilder.Append("instance ");

                    nameBuilder.Append(IrisTypeToCilTypeName(importedMethod.ReturnType));
                    nameBuilder.Append(' ');
                    AppendImportedMemberName(nameBuilder, importedMethod);
                    AppendParameterList(nameBuilder, importedMethod.GetParameters());

                    name = nameBuilder.ToString();
                }
                else
                {
                    StringBuilder nameBuilder = new StringBuilder();
                    nameBuilder.Append(IrisTypeToCilTypeName(method.ReturnType));
                    nameBuilder.Append(' ');
                    nameBuilder.Append(_programName);
                    nameBuilder.Append("::");
                    nameBuilder.Append(methodSymbol.Name);
                    AppendParameterList(nameBuilder, method.GetParameters());

                    name = nameBuilder.ToString();
                }
                _methodNameCache.Add(methodSymbol.Location, name);
            }

            return name;
        }

        private string GetGlobalVariableFieldName(Symbol symbol)
        {
            string name;
            if (!_globalVariableCache.TryGetValue(symbol.Location, out name))
            {
                if (symbol.ImportInfo != null)
                {
                    ImportedField globalField = (ImportedField)symbol.ImportInfo;
                    StringBuilder fieldNameBuilder = new StringBuilder();
                    fieldNameBuilder.Append(IrisTypeToCilTypeName(globalField.FieldType));
                    fieldNameBuilder.Append(' ');
                    AppendImportedMemberName(fieldNameBuilder, globalField);
                    name = fieldNameBuilder.ToString();
                }
                else if (_programName == null)
                {
                    // This can be null when running unit tests
                    name = symbol.Location.ToString();
                }
                else
                {
                    name = string.Format(
                        "{0} {1}::{2}",
                        IrisTypeToCilTypeName(symbol.Type),
                        _programName,
                        symbol.Name);
                }

                _globalVariableCache.Add(symbol.Location, name);
            }

            return name;
        }

        private static void AppendImportedMemberName(StringBuilder builder, ImportedMember member)
        {
            ImportedType declaringType = member.DeclaringType;

            builder.Append('[');
            builder.Append(declaringType.Module.AssemblyName);
            builder.Append(']');
            builder.Append(declaringType.FullName);
            builder.Append("::");
            builder.Append(member.Name);
        }

        private static void AppendParameterList(StringBuilder builder, Variable[] parameters)
        {
            builder.Append('(');

            bool first = true;
            foreach (Variable param in parameters)
            {
                if (first)
                    first = false;
                else
                    builder.Append(',');

                builder.Append(IrisTypeToCilTypeName(param.Type));
            }

            builder.Append(')');
        }

        private void WriteInstruction(string opcode, string argument, bool quoteArgument = false)
        {
            _stringBuilder.Length = 0;

            for (int i = 0; i < _indentLevel; i++)
                _stringBuilder.Append("   ");

            _stringBuilder.Append(opcode);
            if (argument != null)
            {
                _stringBuilder.Append(' ');

                if (quoteArgument)
                    _stringBuilder.Append('\"');
                _stringBuilder.Append(argument);
                if (quoteArgument)
                    _stringBuilder.Append('\"');
            }

            _writer.WriteLine(_stringBuilder);
        }

        private void WriteIndentedText(string text)
        {
            _stringBuilder.Length = 0;

            for (int i = 0; i < _indentLevel; i++)
                _stringBuilder.Append("   ");

            _stringBuilder.Append(text);
            _writer.WriteLine(_stringBuilder);
        }
    }
}
