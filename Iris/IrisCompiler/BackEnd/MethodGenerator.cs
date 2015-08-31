// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler.FrontEnd;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IrisCompiler.BackEnd
{
    /// <summary>
    /// MethodGenerator is used by the Translator to generate the output code.  MethodGenerator
    /// defers output of instructions to support emitting source/line information and does some
    /// peephole optimizations to cleanup the code output and emit proper branch instructions.
    /// </summary>
    public sealed class MethodGenerator
    {
        private bool _emitDebugInfo;
        private bool _outputEnabled;
        private string _methodFileName;
        private IEmitter _emitter;
        private List<MethodInstruction> _deferredInstructions = new List<MethodInstruction>(1024);
        private FilePosition _lineStart;
        private bool _isSourceLine;

        #region Nested Types

        private enum MethodOpCode
        {
            Assign,
            BranchCond,
            BranchFalse,
            BranchTrue,
            Call,
            Goto,
            Label,
            Load,
            LoadElem,
            LoadElemA,
            Operator,
            Dup,
            Pop,
            PushArg,
            PushArgA,
            PushInt,
            PushGbl,
            PushGblA,
            PushLoc,
            PushLocA,
            PushString,
            StoreArg,
            StoreElem,
            StoreGbl,
            StoreLoc,
        }

        private class MethodInstruction
        {
            public readonly MethodOpCode OpCode;
            public readonly object Operand;
            public readonly Operator Condition; // Only used for BranchCond

            public MethodInstruction(MethodOpCode opCode, object operand)
            {
                OpCode = opCode;
                Operand = operand;
                Condition = IrisCompiler.Operator.None;
            }

            public MethodInstruction(MethodOpCode opCode, object operand, Operator condition)
            {
                OpCode = opCode;
                Operand = operand;
                Condition = condition;
            }

            public override bool Equals(object obj)
            {
                MethodInstruction other = obj as MethodInstruction;
                if (other == null)
                    return false;
                if (OpCode != other.OpCode)
                    return false;
                if (Condition != other.Condition)
                    return false;

                if (Operand == null && other.Operand == null)
                    return true;
                if (Operand == null || other.Operand == null)
                    return false;
                if (!Operand.Equals(other.Operand))
                    return false;

                return true;
            }

            public override int GetHashCode()
            {
                return (int)OpCode ^ (Operand?.GetHashCode() ?? 0);
            }
        }

        #endregion

        public MethodGenerator(CompilerContext context)
        {
            bool emitDebugInfo = !context.Flags.HasFlag(CompilationFlags.NoDebug);

            _outputEnabled = true;
            _emitter = context.Emitter;
            _emitDebugInfo = emitDebugInfo;
        }

        public void SetOutputEnabled(bool enable)
        {
            if (enable != _outputEnabled)
            {
                _deferredInstructions.Clear();
                _outputEnabled = enable;
            }
        }

        public void BeginMethod(
            string name,
            IrisType returnType,
            Variable[] parameters,
            Variable[] locals,
            bool entryPoint,
            string methodFileName)
        {
            if (!_outputEnabled)
                return;

            _emitter.BeginMethod(name, returnType, parameters, locals.ToArray(), entryPoint);
            if (_emitDebugInfo)
                _emitter.EmitMethodLanguageInfo();

            _methodFileName = methodFileName;
        }

        public void EndMethod()
        {
            if (!_outputEnabled)
                return;

            EmitDeferredInstructions();
            _emitter.EndMethod();
        }

        public void BeginSourceLine(FilePosition fp)
        {
            if (_outputEnabled && _emitDebugInfo)
            {
                _lineStart = fp;
                _isSourceLine = true;
            }
        }

        public void EndSourceLine(FilePosition fp)
        {
            if (_outputEnabled && _isSourceLine)
            {
                _emitter.EmitLineInfo(new SourceRange(_lineStart, fp), _methodFileName ?? string.Empty);
                _isSourceLine = false;

                _methodFileName = null; // We only need to emit this for the first line
            }

            EmitDeferredInstructions();
        }

        public void EmitNonCodeLineInfo(SourceRange range)
        {
            if (_outputEnabled && _emitDebugInfo)
            {
                EmitDeferredInstructions();
                _emitter.EmitLineInfo(range, _methodFileName ?? string.Empty);
                _emitter.NoOp();

                _isSourceLine = false;
                _methodFileName = null; // We only need to emit this for the first line
            }
        }

        private static Operator InvertComparisonOperator(Operator opr)
        {
            switch (opr)
            {
                case IrisCompiler.Operator.Equal:
                    return IrisCompiler.Operator.NotEqual;
                case IrisCompiler.Operator.NotEqual:
                    return IrisCompiler.Operator.Equal;
                case IrisCompiler.Operator.LessThan:
                    return IrisCompiler.Operator.GreaterThanEqual;
                case IrisCompiler.Operator.LessThanEqual:
                    return IrisCompiler.Operator.GreaterThan;
                case IrisCompiler.Operator.GreaterThan:
                    return IrisCompiler.Operator.LessThanEqual;
                case IrisCompiler.Operator.GreaterThanEqual:
                    return IrisCompiler.Operator.LessThan;
                default:
                    throw new InvalidOperationException("Invalid comparison operator");
            }
        }

        private static bool IsComparisonOperator(Operator opr)
        {
            switch (opr)
            {
                case IrisCompiler.Operator.Equal:
                case IrisCompiler.Operator.NotEqual:
                case IrisCompiler.Operator.LessThan:
                case IrisCompiler.Operator.LessThanEqual:
                case IrisCompiler.Operator.GreaterThan:
                case IrisCompiler.Operator.GreaterThanEqual:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsUnaryNotOperator(Operator opr)
        {
            return opr == IrisCompiler.Operator.Not;
        }

        public void EmitDeferredInstructions()
        {
            if (!_outputEnabled)
                return;

            foreach (MethodInstruction instruction in _deferredInstructions)
            {
                switch (instruction.OpCode)
                {
                    case MethodOpCode.Assign:
                        _emitter.Store((IrisType)instruction.Operand);
                        break;
                    case MethodOpCode.BranchCond:
                        _emitter.BranchCondition(instruction.Condition, (int)instruction.Operand);
                        break;
                    case MethodOpCode.BranchFalse:
                        _emitter.BranchFalse((int)instruction.Operand);
                        break;
                    case MethodOpCode.BranchTrue:
                        _emitter.BranchTrue((int)instruction.Operand);
                        break;
                    case MethodOpCode.Call:
                        _emitter.Call((Symbol)instruction.Operand);
                        break;
                    case MethodOpCode.Goto:
                        _emitter.Goto((int)instruction.Operand);
                        break;
                    case MethodOpCode.Label:
                        _emitter.Label((int)instruction.Operand);
                        break;
                    case MethodOpCode.Load:
                        _emitter.Load((IrisType)instruction.Operand);
                        break;
                    case MethodOpCode.LoadElem:
                        _emitter.LoadElement((IrisType)instruction.Operand);
                        break;
                    case MethodOpCode.LoadElemA:
                        _emitter.LoadElementAddress((IrisType)instruction.Operand);
                        break;
                    case MethodOpCode.Operator:
                        _emitter.Operator((Operator)instruction.Operand);
                        break;
                    case MethodOpCode.Dup:
                        _emitter.Dup();
                        break;
                    case MethodOpCode.Pop:
                        _emitter.Pop();
                        break;
                    case MethodOpCode.PushArg:
                        _emitter.PushArgument((int)instruction.Operand);
                        break;
                    case MethodOpCode.PushArgA:
                        _emitter.PushArgumentAddress((int)instruction.Operand);
                        break;
                    case MethodOpCode.PushInt:
                        _emitter.PushIntConst((int)instruction.Operand);
                        break;
                    case MethodOpCode.PushGbl:
                        _emitter.PushGlobal((Symbol)instruction.Operand);
                        break;
                    case MethodOpCode.PushGblA:
                        _emitter.PushGlobalAddress((Symbol)instruction.Operand);
                        break;
                    case MethodOpCode.PushLoc:
                        _emitter.PushLocal((int)instruction.Operand);
                        break;
                    case MethodOpCode.PushLocA:
                        _emitter.PushLocalAddress((int)instruction.Operand);
                        break;
                    case MethodOpCode.PushString:
                        _emitter.PushString((string)instruction.Operand);
                        break;
                    case MethodOpCode.StoreArg:
                        _emitter.StoreArgument((int)instruction.Operand);
                        break;
                    case MethodOpCode.StoreElem:
                        _emitter.StoreElement((IrisType)instruction.Operand);
                        break;
                    case MethodOpCode.StoreGbl:
                        _emitter.StoreGlobal((Symbol)instruction.Operand);
                        break;
                    case MethodOpCode.StoreLoc:
                        _emitter.StoreLocal((int)instruction.Operand);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown opcode.  Missing case?");
                }
            }

            _deferredInstructions.Clear();
        }

        private MethodInstruction LastInstruction()
        {
            if (!_outputEnabled)
                return null;

            int count = _deferredInstructions.Count;
            return count == 0 ? null : _deferredInstructions[count - 1];
        }

        private void RemoveLastInstruction()
        {
            int count = _deferredInstructions.Count;
            if (count > 0)
                _deferredInstructions.RemoveAt(count - 1);
        }

        public void InitArray(Symbol arraySymbol, SubRange subRange)
        {
            if (_outputEnabled)
            {
                EmitDeferredInstructions();
                _emitter.InitArray(arraySymbol, subRange);
            }
        }

        public void PushString(string s)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.PushString, s));
        }

        public void PushIntConst(int i)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.PushInt, i));
        }

        public void PushArgument(int i)
        {
            MethodInstruction push = new MethodInstruction(MethodOpCode.PushArg, i);
            if (push.Equals(LastInstruction()))
                Dup();
            else
                _deferredInstructions.Add(push);
        }

        public void PushArgumentAddress(int i)
        {
            MethodInstruction push = new MethodInstruction(MethodOpCode.PushArgA, i);
            if (push.Equals(LastInstruction()))
                Dup();
            else
                _deferredInstructions.Add(push);
        }

        public void StoreArgument(int i)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.StoreArg, i));
        }

        public void PushLocal(int i)
        {
            MethodInstruction push = new MethodInstruction(MethodOpCode.PushLoc, i);
            if (push.Equals(LastInstruction()))
                Dup();
            else
                _deferredInstructions.Add(push);
        }

        public void PushLocalAddress(int i)
        {
            MethodInstruction push = new MethodInstruction(MethodOpCode.PushLocA, i);
            if (push.Equals(LastInstruction()))
                Dup();
            else
                _deferredInstructions.Add(push);
        }

        public void StoreLocal(int i)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.StoreLoc, i));
        }

        public void PushGlobal(Symbol symbol)
        {
            MethodInstruction push = new MethodInstruction(MethodOpCode.PushGbl, symbol);
            if (push.Equals(LastInstruction()))
                Dup();
            else
                _deferredInstructions.Add(push);
        }

        public void PushGlobalAddress(Symbol symbol)
        {
            MethodInstruction push = new MethodInstruction(MethodOpCode.PushGblA, symbol);
            if (push.Equals(LastInstruction()))
                Dup();
            else
                _deferredInstructions.Add(push);
        }

        public void StoreGlobal(Symbol symbol)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.StoreGbl, symbol));
        }

        public void Dup()
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.Dup, null));
        }

        public void Pop()
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.Pop, null));
        }

        public void Load(IrisType type)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.Load, type));
        }

        public void LoadElement(IrisType elementType)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.LoadElem, elementType));
        }

        public void LoadElementAddress(IrisType elementType)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.LoadElemA, elementType));
        }

        public void StoreElement(IrisType elementType)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.StoreElem, elementType));
        }

        public void Label(int i)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.Label, i));
        }

        public void Goto(int i)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.Goto, i));
        }

        public void BranchCondition(Operator condition, int i)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.BranchCond, i, condition));
        }

        public void BranchFalse(int i)
        {
            MethodInstruction last = LastInstruction();
            if (last != null && last.OpCode == MethodOpCode.Operator)
            {
                Operator opr = (Operator)last.Operand;
                if (IsUnaryNotOperator(opr))
                {
                    RemoveLastInstruction();
                    BranchTrue(i);
                    return;
                }
                else if (IsComparisonOperator(opr))
                {
                    RemoveLastInstruction();
                    BranchCondition(InvertComparisonOperator(opr), i);
                    return;
                }
            }

            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.BranchFalse, i));
        }

        public void BranchTrue(int i)
        {
            MethodInstruction last = LastInstruction();
            if (last != null && last.OpCode == MethodOpCode.Operator)
            {
                Operator opr = (Operator)last.Operand;
                if (IsUnaryNotOperator(opr))
                {
                    RemoveLastInstruction();
                    BranchFalse(i);
                    return;
                }
                else if (IsComparisonOperator(opr))
                {
                    RemoveLastInstruction();
                    BranchCondition(opr, i);
                    return;
                }
            }

            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.BranchTrue, i));
        }

        public void Call(Symbol methodSymbol)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.Call, methodSymbol));
        }

        public void Operator(Operator opr)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.Operator, opr));
        }

        public void Store(IrisType type)
        {
            _deferredInstructions.Add(new MethodInstruction(MethodOpCode.Assign, type));
        }
    }
}
