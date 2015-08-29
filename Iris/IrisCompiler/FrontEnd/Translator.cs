// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler.BackEnd;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IrisCompiler.FrontEnd
{
    /// <summary>
    /// The Translator is the front-end of the Iris compiler.
    /// 
    /// Translation takes place in a single pass over the source file.  The Translator reads input
    /// via the Lexical Analyzer (Lexer.cs) and parses using recursive descent.  While parsing, the
    /// Translater does semantic analysis and outputs via an emitter.  The single pass of
    /// compilation means we don't need to create any intermediate representations of the code
    /// (syntax trees, etc).
    /// 
    /// Iris supports multiple back-ends to generate different types of code.  Each back-end is an
    /// implementation of IEmitter.
    /// </summary>
    public class Translator
    {
        protected readonly MethodGenerator MethodGenerator;

        private readonly CompilerContext _context;
        private readonly SymbolTable _symbolTable;
        private readonly Lexer _lexer;

        private FilePosition _lastParsedPosition;
        private string _lexeme;
        private int _lastIntegerLexeme;
        private int _nextLabel;
        private int _nextUniqueName;

        /// <summary>
        /// The mode we are in when emitting the code to load a symbol
        /// </summary>
        protected enum SymbolLoadMode
        {
            Raw, // Emit code to load the value of a symbol directly.
            Dereference, // Emit code to load the value of a symbol and dereference it if the symbol represents an address.
            Address, // Emit code to load the value of a symbol representing an address, or load the address of a non-reference symbol.
            Element, // Emit code to load an array element.
            ElementAddress, // Emit code to load the address of an array element.
        }

        public Translator(CompilerContext context)
        {
            MethodGenerator = new MethodGenerator(context);

            _context = context;
            _lexer = context.Lexer;
            _symbolTable = context.SymbolTable;
            _lastParsedPosition = _lexer.TokenStartPosition;
            _lexeme = string.Empty;
        }

        /// <summary>
        /// Gets a value indicating whether we've parsed a call instruction.  This is used by the
        /// debugger to detect potential side effects of expression evaluation.
        /// Some calls that are implicitly emitted such as strcmp do not set this property.
        /// </summary>
        protected bool ParsedCallSyntax
        {
            get;
            set;
        }

        public virtual void TranslateInput()
        {
            ParseProgram();
        }

        public void TranslateStatement()
        {
            ParseStatement();
            MethodGenerator.EmitDeferredInstructions();
        }

        public void TranslateExpression()
        {
            ParseExpression();
            MethodGenerator.EmitDeferredInstructions();
        }

        protected void ParseProgram()
        {
            string programName = "program";

            if (Accept(Token.KwProgram))
            {
                if (Accept(Token.Identifier))
                    programName = _lexeme;
                else
                    AddErrorAtTokenStart("Expecting program name.");

                Expect(Token.ChrSemicolon);
            }

            _context.Emitter.BeginProgram(programName, _context.Importer.ImportedAssemblies);

            List<Tuple<Variable, FilePosition>> globals = new List<Tuple<Variable, FilePosition>>();
            if (Accept(Token.KwVar))
            {
                ParseVariableList(globals, isArgumentList: false);
                foreach (Tuple<Variable, FilePosition> globalDecl in globals)
                {
                    Variable global = globalDecl.Item1;
                    if (ValidateName(globalDecl.Item2, global.Name, global: true))
                    {
                        Symbol globalSymbol = _symbolTable.Add(global.Name, global.Type, StorageClass.Global);
                        _context.Emitter.DeclareGlobal(globalSymbol);
                    }
                }
            }

            FilePosition blockBegin = _lexer.TokenStartPosition;
            while (!Accept(Token.KwBegin))
            {
                if (Accept(Token.Eof))
                {
                    AddErrorAtLastParsedPosition("Unexpected end of file looking for main block.");
                    return;
                }
                else if (Accept(Token.KwFunction))
                {
                    ParseMethod(isFunction: true);
                }
                else if (Accept(Token.KwProcedure))
                {
                    ParseMethod(isFunction: false);
                }
                else if (Accept(Token.KwVar))
                {
                    AddErrorAtTokenStart("Global variables must be declared before the first function or procedure.");
                    List<Tuple<Variable, FilePosition>> unused = new List<Tuple<Variable, FilePosition>>();
                    ParseVariableList(unused, isArgumentList: false);
                }
                else if (!Accept(Token.ChrSemicolon))
                {
                    AddErrorAtTokenStart("Expecting 'function', 'procedure', or 'begin'.");
                    SkipToNextEnd();
                }

                blockBegin = _lexer.TokenStartPosition;
            }

            // We are now at the main block
            IrisType mainMethod = Procedure.Create(new Variable[0]);
            Symbol mainSymbol = _symbolTable.OpenMethod("$.main", mainMethod);

            MethodGenerator.BeginMethod(mainSymbol.Name, IrisType.Void, new Variable[0], new Variable[0], true, _context.FilePath);
            MethodGenerator.EmitNonCodeLineInfo(blockBegin.Expand(5 /* Length of "begin" */));

            // Initialize global variables if needed
            foreach (Tuple<Variable, FilePosition> globalDecl in globals)
                InitializeVariableIfNeeded(blockBegin, globalDecl.Item1);

            ParseStatements(Token.KwEnd);

            MethodGenerator.EndMethod();

            Accept(Token.ChrPeriod);
            Expect(Token.Eof);

            _context.Emitter.EndProgram();
        }

        protected void ParseMethod(bool isFunction)
        {
            FilePosition namePosition = _lexer.TokenStartPosition;
            if (!Accept(Token.Identifier))
            {
                AddError(namePosition, "Expecting procedure or function name.");
                SkipToNextEnd();
                return;
            }

            string methodName = _lexeme;
            if (!ValidateName(namePosition, methodName, global: true))
                methodName = (_nextUniqueName++).ToString();

            List<Tuple<Variable, FilePosition>> parameterList = new List<Tuple<Variable, FilePosition>>();
            if (Accept(Token.ChrOpenParen) && !Accept(Token.ChrCloseParen))
            {
                ParseVariableList(parameterList, isArgumentList: true);
                Expect(Token.ChrCloseParen);
            }

            IrisType returnType = IrisType.Void;
            if (Accept(Token.ChrColon))
            {
                returnType = ParseType();
                if (!isFunction)
                    AddErrorAtTokenStart("Procedure cannot have return value.");
            }
            else if (isFunction)
            {
                AddErrorAtTokenStart("Expecting return type for function.");
                returnType = IrisType.Invalid;
            }

            Expect(Token.ChrSemicolon);

            // We've now parsed the method header.  We can now create the method symbol and parse
            // the body of the method.
            IrisType method;
            Variable[] parameters = parameterList.Select(p => p.Item1).ToArray();
            method = isFunction ?
                (IrisType)Function.Create(returnType, parameters) :
                Procedure.Create(parameterList.Select(p => p.Item1).ToArray());

            Symbol methodSymbol = _symbolTable.OpenMethod(methodName, method);

            // Add argument symbols for all of the parameters
            foreach (Tuple<Variable, FilePosition> param in parameterList)
            {
                if (ValidateName(param.Item2, param.Item1.Name, global: false))
                    _symbolTable.Add(param.Item1.Name, param.Item1.Type, StorageClass.Argument);
            }

            List<Variable> locals = new List<Variable>();
            if (isFunction)
            {
                // Create a local variable for the return value
                _symbolTable.Add(methodName, returnType, StorageClass.Local);
                locals.Add(new Variable(returnType, methodName));
            }

            if (Accept(Token.KwVar))
            {
                List<Tuple<Variable, FilePosition>> localsAndPositions = new List<Tuple<Variable, FilePosition>>();
                ParseVariableList(localsAndPositions, isArgumentList: false);
                Accept(Token.ChrSemicolon);

                foreach (Tuple<Variable, FilePosition> localDecl in localsAndPositions)
                {
                    Variable local = localDecl.Item1;
                    string localName = local.Name;
                    if (ValidateName(localDecl.Item2, localName, global: false))
                    {
                        _symbolTable.Add(localName, local.Type, StorageClass.Local);
                        locals.Add(local);
                    }
                }
            }

            FilePosition begin = _lexer.TokenStartPosition;
            MethodGenerator.BeginMethod(methodSymbol.Name, returnType, parameters, locals.ToArray(), false, _context.FilePath);
            MethodGenerator.EmitNonCodeLineInfo(begin.Expand(5 /* Length of "begin" */));

            // Initialize locals if needed
            foreach (Variable local in locals)
                InitializeVariableIfNeeded(begin, local);

            // Parse the body of the method
            Expect(Token.KwBegin);
            ParseStatements(Token.KwEnd);

            if (isFunction)
                MethodGenerator.PushLocal(0);

            MethodGenerator.EndMethod();

            _symbolTable.CloseMethod();
        }

        private void InitializeVariableIfNeeded(FilePosition fp, Variable variable)
        {
            IrisType varType = variable.Type;
            if (varType.IsArray || varType == IrisType.String)
            {
                // Variable needs to be initialized.
                Symbol varSymbol = _symbolTable.Lookup(variable.Name);
                if (varType.IsArray)
                {
                    MethodGenerator.InitArray(varSymbol, variable.SubRange);
                    if (varType.GetElementType() == IrisType.String)
                    {
                        // String arary - initialize all elements
                        EmitLoadSymbol(varSymbol, SymbolLoadMode.Raw);
                        Symbol initProc = LookupSymbol(fp, "$.initstrarray");
                        MethodGenerator.Call(initProc);
                    }
                }
                else
                {
                    // String
                    Symbol emptyStr = LookupSymbol(fp, "$.emptystr");
                    MethodGenerator.PushGlobal(emptyStr);
                    EmitStoreSymbol(varSymbol);
                }

                MethodGenerator.EmitDeferredInstructions();
            }
        }

        protected void ParseVariableList(List<Tuple<Variable, FilePosition>> variables, bool isArgumentList)
        {
            bool first = true;
            do
            {
                bool isByRef = isArgumentList && Accept(Token.KwVar);

                FilePosition nameStart = _lexer.TokenStartPosition;
                if (!Accept(Token.Identifier))
                {
                    if (!isArgumentList && !first)
                        return; // Allow semicolon on the final var declaration

                    AddErrorAtTokenStart("Expecting variable name.");
                    SkipStatement();
                    return;
                }

                first = false;

                // Parse name list
                List<Tuple<string, FilePosition>> names = new List<Tuple<string, FilePosition>>();
                names.Add(new Tuple<string, FilePosition>(_lexeme, nameStart));
                while (Accept(Token.ChrComma))
                {
                    nameStart = _lexer.TokenStartPosition;
                    if (!Accept(Token.Identifier))
                        AddErrorAtTokenStart("Expecting variable name after ','.");
                    else
                        names.Add(new Tuple<string, FilePosition>(_lexeme, nameStart));
                }

                Expect(Token.ChrColon);

                // Now parse the type and create the variable(s)
                SubRange subRange = null;
                IrisType type = ParseType(ref subRange, expectSubRange: !isArgumentList);
                if (isByRef)
                    type = type.MakeByRefType();

                foreach (Tuple<string, FilePosition> name in names)
                    variables.Add(new Tuple<Variable, FilePosition>(new Variable(type, name.Item1, subRange), name.Item2));
            }
            while (Accept(Token.ChrSemicolon));
        }

        private void SkipToNextEnd()
        {
            // Skip to the next end of block or EOF
            while (_lexer.CurrentToken != Token.KwEnd && _lexer.CurrentToken != Token.Eof)
                _lexer.MoveNext();

            Accept(Token.KwEnd); // Parser expects that we've already accepted 'end'
        }

        protected void ParseStatements(Token endToken)
        {
            FilePosition fp;
            bool allowEmpty = false;
            do
            {
                ParseStatement(allowEmpty);
                allowEmpty = true;
                fp = _lexer.TokenStartPosition;
                if (Accept(Token.Eof))
                {
                    // Don't loop forever
                    AddErrorAtLastParsedPosition(string.Format(
                        "Unexpected end of file looking for {0}.",
                        Lexer.TokenName(endToken)));
                    return;
                }
            }
            while (Accept(Token.ChrSemicolon));

            if (!Accept(endToken))
            {
                AddErrorAtTokenStart("Expecting ';'."); // Missing semicolon caused us to exit the loop above.
                SkipToNextEnd();
            }

            MethodGenerator.EmitNonCodeLineInfo(new SourceRange(fp, _lastParsedPosition));
        }

        protected void ParseStatement(bool allowEmpty = false)
        {
            FilePosition statementStart = _lexer.TokenStartPosition;
            MethodGenerator.BeginSourceLine(statementStart);
            if (Accept(Token.KwFor))
            {
                Symbol iterator;
                FilePosition fp = _lexer.TokenStartPosition;
                if (!Accept(Token.Identifier))
                {
                    AddError(fp, "Expecting integer identifier.");
                    SkipStatement();
                    return;
                }

                // Initial assignment
                iterator = LookupSymbol(fp, _lexeme);
                VerifyExpressionType(fp, DerefType(iterator.Type), IrisType.Integer);
                bool byRef = iterator.Type.IsByRef;
                if (byRef)
                    EmitLoadSymbol(iterator, SymbolLoadMode.Raw);

                Expect(Token.ChrAssign);
                fp = _lexer.TokenStartPosition;
                IrisType rhs = ParseExpression();
                VerifyExpressionType(fp, rhs, IrisType.Integer);

                if (byRef)
                    MethodGenerator.Store(rhs);
                else
                    EmitStoreSymbol(iterator);

                Expect(Token.KwTo);

                // Loop start and condition
                int loopLabel = GetNextLabel();
                MethodGenerator.Label(loopLabel);
                EmitLoadSymbol(iterator, SymbolLoadMode.Dereference);
                fp = _lexer.TokenStartPosition;
                rhs = ParseExpression();
                VerifyExpressionType(fp, rhs, IrisType.Integer);
                int exitLabel = GetNextLabel();
                MethodGenerator.BranchCondition(Operator.GreaterThan, exitLabel);

                // Loop body
                Expect(Token.KwDo);
                FilePosition forEndPosition = _lastParsedPosition;
                MethodGenerator.EndSourceLine(forEndPosition);
                ParseStatement();

                // Loop end
                MethodGenerator.BeginSourceLine(statementStart); // Source position is the same as the loop start.
                Increment(iterator);
                MethodGenerator.Goto(loopLabel);
                MethodGenerator.Label(exitLabel);
                MethodGenerator.EndSourceLine(forEndPosition);
            }
            else if (Accept(Token.KwWhile))
            {
                int loopLabel = GetNextLabel();
                MethodGenerator.Label(loopLabel);

                FilePosition fp = _lexer.TokenStartPosition;
                IrisType type = ParseExpression();
                VerifyExpressionType(fp, type, IrisType.Boolean);
                int exitLabel = GetNextLabel();
                MethodGenerator.BranchFalse(exitLabel);

                Expect(Token.KwDo);

                MethodGenerator.EndSourceLine(_lastParsedPosition);

                ParseStatement();
                MethodGenerator.Goto(loopLabel);
                MethodGenerator.Label(exitLabel);
            }
            else if (Accept(Token.KwRepeat))
            {
                int loopLabel = GetNextLabel();
                MethodGenerator.Label(loopLabel);
                MethodGenerator.EmitNonCodeLineInfo(new SourceRange(statementStart, _lastParsedPosition));

                ParseStatements(Token.KwUntil);

                FilePosition fp = _lexer.TokenStartPosition;
                IrisType type = ParseExpression();
                VerifyExpressionType(fp, type, IrisType.Boolean);
                MethodGenerator.BranchFalse(loopLabel);

                MethodGenerator.EndSourceLine(_lastParsedPosition);
            }
            else if (Accept(Token.KwIf))
            {
                ParseIf();
            }
            else if (Accept(Token.KwBegin))
            {
                MethodGenerator.EmitNonCodeLineInfo(new SourceRange(statementStart, _lastParsedPosition));
                ParseStatements(Token.KwEnd);
            }
            else if (Accept(Token.Identifier))
            {
                FilePosition fp = _lexer.TokenStartPosition;
                string symbolName = _lexeme;
                Symbol symbol = LookupSymbol(fp, symbolName);
                IrisType lhs = symbol.Type;
                bool assign = false;
                bool isArray = false;
                if (Accept(Token.ChrOpenBracket))
                {
                    // Assignment to an array element.
                    isArray = true;
                    lhs = ProcessArrayAccess(fp, symbol, SymbolLoadMode.Raw);
                }
                if (Accept(Token.ChrAssign))
                {
                    assign = true;
                    bool indirectAssign = false;

                    if (lhs.IsByRef)
                    {
                        lhs = lhs.GetElementType();
                        EmitLoadSymbol(symbol, SymbolLoadMode.Raw);
                        indirectAssign = true;
                    }

                    FilePosition exprPosition = _lexer.TokenStartPosition;
                    IrisType rhs = ParseExpression();

                    if (lhs.IsMethod)
                    {
                        AddError(fp, "Cannot assign to result of function or procedure call.");
                    }
                    else if (lhs != IrisType.Invalid)
                    {
                        if (rhs == IrisType.Void)
                            AddError(fp, "Cannot use procedure in assignment statement.");
                        else if (rhs != IrisType.Invalid && rhs != lhs)
                            AddError(exprPosition, string.Format("Cannot assign to '{0}' (type mismatch error).", symbolName));

                        if (isArray)
                            MethodGenerator.StoreElement(lhs);
                        else if (indirectAssign)
                            MethodGenerator.Store(lhs);
                        else
                            EmitStoreSymbol(symbol);
                    }
                }
                else if (isArray)
                {
                    // This is an array subscript.  Assignment is the only kind of statement that
                    // starts with an array subscript.
                    AddErrorAtTokenStart("Expecting ':='.");
                    SkipStatement();
                }

                if (!assign && !isArray)
                {
                    bool skipArgList = !Accept(Token.ChrOpenParen);
                    ProcessCall(fp, symbol, skipArgList);

                    if (symbol.Type.IsFunction)
                        MethodGenerator.Pop();
                }

                MethodGenerator.EndSourceLine(_lastParsedPosition);
            }
            else if (Accept(Token.KwElse))
            {
                AddErrorAtTokenStart("Cannot start statement with 'else' or unexpected ';' after if statement.");
                SkipStatement();
            }
            else if (!allowEmpty && !Accept(Token.ChrSemicolon))
            {
                AddErrorAtLastParsedPosition("Expecting statement.");
                SkipStatement();
            }
        }

        private void SkipStatement()
        {
            // Skip to semicolon, end of block, or EOF
            while (_lexer.CurrentToken != Token.ChrSemicolon && _lexer.CurrentToken != Token.KwEnd && _lexer.CurrentToken != Token.Eof)
                _lexer.MoveNext();
        }

        protected IrisType ParseType()
        {
            SubRange sr = null;
            return ParseType(ref sr, expectSubRange: false);
        }

        protected IrisType ParseType(ref SubRange subRange, bool expectSubRange)
        {
            IrisType type;
            if (Accept(Token.KwArray))
            {
                subRange = null;
                FilePosition dimensionPosition = _lexer.TokenStartPosition;
                if (Accept(Token.ChrOpenBracket))
                {
                    Expect(Token.Number);
                    int from = _lastIntegerLexeme;
                    Expect(Token.ChrDotDot);
                    Expect(Token.Number);
                    int to = _lastIntegerLexeme;
                    Expect(Token.ChrCloseBracket);

                    subRange = new SubRange(from, to);
                }

                if (expectSubRange)
                {
                    // Expecting array subrange (local/global var declarations)
                    if (subRange == null)
                        AddError(dimensionPosition, "Expecting array subrange.");
                    else if (subRange.From != 0)
                        AddError(dimensionPosition, "Iris only support arrays that start at index zero.");
                }
                else if (subRange != null)
                {
                    // Not expecting array dimension (parameter list)
                    AddError(dimensionPosition, "Not expecting array subrange here.");
                }

                Expect(Token.KwOf);
                type = ParsePrimitiveType();
                if (type == IrisType.Invalid)
                    AddErrorAtTokenStart("Expecting  'integer', 'string', or 'boolean'");
                else
                    type = type.MakeArrayType();

                return type;
            }

            type = ParsePrimitiveType();
            if (type == IrisType.Invalid)
                AddErrorAtTokenStart("Expecting  'integer', 'string', 'boolean', or 'array'");

            return type;
        }

        private IrisType ParsePrimitiveType()
        {
            if (Accept(Token.KwInteger))
                return IrisType.Integer;
            else if (Accept(Token.KwString))
                return IrisType.String;
            else if (Accept(Token.KwBoolean))
                return IrisType.Boolean;
            else
                return IrisType.Invalid;
        }

        protected void ParseIf()
        {
            FilePosition fp = _lexer.TokenStartPosition;
            IrisType type = ParseExpression();
            VerifyExpressionType(fp, type, IrisType.Boolean);
            Expect(Token.KwThen);

            int label = GetNextLabel();
            MethodGenerator.BranchFalse(label);
            MethodGenerator.EndSourceLine(_lastParsedPosition);

            ParseStatement();

            FilePosition endOfIfStatement = _lastParsedPosition;
            FilePosition elseStart = _lexer.TokenStartPosition;
            if (Accept(Token.KwElse))
            {
                int label2 = GetNextLabel();
                MethodGenerator.Goto(label2);
                MethodGenerator.Label(label);
                MethodGenerator.EndSourceLine(endOfIfStatement);
                MethodGenerator.BeginSourceLine(elseStart);
                if (Accept(Token.KwIf))
                {
                    ParseIf();
                }
                else
                {
                    MethodGenerator.EmitNonCodeLineInfo(elseStart.Expand(4 /* Length of "else" */));
                    ParseStatement();
                }

                MethodGenerator.Label(label2);
            }
            else
            {
                MethodGenerator.Label(label);
            }
        }

        protected IrisType ParseExpression(SymbolLoadMode mode = SymbolLoadMode.Dereference)
        {
            IrisType lhs = ParseCompareExpression(mode);
            FilePosition fp = _lexer.TokenStartPosition;
            Operator opr = AcceptOperator(OperatorMaps.Instance.Logic);
            if (opr != Operator.None)
            {
                VerifyExpressionType(fp, lhs, IrisType.Boolean);
                IrisType rhs = ParseExpression(mode);
                VerifyExpressionType(fp, rhs, IrisType.Boolean);
                MethodGenerator.Operator(opr);
            }

            return lhs;
        }

        protected IrisType ParseCompareExpression(SymbolLoadMode mode)
        {
            IrisType lhs = ParseArithmeticExpression(mode);
            FilePosition fp = _lexer.TokenStartPosition;
            Operator opr = AcceptOperator(OperatorMaps.Instance.Compare);
            if (opr != Operator.None)
            {
                IrisType rhs = ParseCompareExpression(mode);
                if (lhs == IrisType.String && rhs == IrisType.String)
                {
                    Symbol strcmp = LookupSymbol(fp, "strcmp");
                    MethodGenerator.Call(strcmp);
                    MethodGenerator.PushIntConst(0);
                }

                MethodGenerator.Operator(opr);
                return ApplyTypeRules(fp, lhs, rhs, boolResult: true);
            }

            return lhs;
        }

        protected IrisType ParseArithmeticExpression(SymbolLoadMode mode)
        {
            IrisType lhs = ParseTerm(mode);
            FilePosition fp = _lexer.TokenStartPosition;
            Operator opr = AcceptOperator(OperatorMaps.Instance.Arithmetic);
            if (opr != Operator.None)
            {
                IrisType rhs = ParseArithmeticExpression(mode);
                return ProcessArithemticOperator(fp, lhs, rhs, opr);
            }

            return lhs;
        }

        protected IrisType ParseTerm(SymbolLoadMode mode)
        {
            IrisType lhs = ParseFactor(mode);
            FilePosition fp = _lexer.TokenStartPosition;
            Operator opr = AcceptOperator(OperatorMaps.Instance.Term);
            if (opr != Operator.None)
            {
                IrisType rhs = ParseTerm(mode);
                return ProcessArithemticOperator(fp, lhs, rhs, opr);
            }

            return lhs;
        }

        private IrisType ProcessArithemticOperator(FilePosition fp, IrisType lhs, IrisType rhs, Operator opr)
        {
            IrisType resultType = ApplyTypeRules(fp, lhs, rhs);
            if (resultType == IrisType.String)
            {
                if (opr != Operator.Add)
                    AddError(fp, "Only the '+' or comparison operators can be used on string values.");

                Symbol concat = LookupSymbol(fp, "concat");
                MethodGenerator.Call(concat);
            }
            else if (resultType == IrisType.Boolean)
            {
                AddError(fp, "Arithmetic operators cannot be applied to boolean values.");
                return IrisType.Invalid;
            }
            else
            {
                MethodGenerator.Operator(opr);
            }

            return resultType;
        }

        protected IrisType ParseFactor(SymbolLoadMode mode)
        {
            Operator opr = AcceptOperator(OperatorMaps.Instance.Factor);
            FilePosition fp = _lexer.TokenStartPosition;
            IrisType type = ParseBaseExpression(mode);
            if (opr != Operator.None)
            {
                type = DerefType(type);
                if (type == IrisType.String)
                    AddError(fp, "Unary operators cannot be applied to string values.");
                else if (opr == Operator.Not)
                    VerifyExpressionType(fp, type, IrisType.Boolean);
                else if (opr == Operator.Negate && type == IrisType.Boolean)
                    AddError(fp, "Unary negate operator cannot be applied to boolean values.");

                MethodGenerator.Operator(opr);
            }

            return type;
        }

        protected IrisType ParseBaseExpression(SymbolLoadMode mode)
        {
            FilePosition fp = _lastParsedPosition;
            if (Accept(Token.Identifier))
            {
                Symbol symbol = LookupSymbol(fp, _lexeme);

                if (Accept(Token.ChrOpenBracket))
                    return ProcessArrayAccess(fp, symbol, mode == SymbolLoadMode.Address ? SymbolLoadMode.ElementAddress : SymbolLoadMode.Element);
                if (Accept(Token.ChrOpenParen))
                    return ProcessCall(fp, symbol, skipArgList: false);
                
                IrisType type = symbol.Type;
                if (type.IsMethod)
                    return ProcessCall(fp, symbol, skipArgList: true);
                if (type != IrisType.Invalid)
                    EmitLoadSymbol(symbol, mode);

                if (mode == SymbolLoadMode.Address && !type.IsByRef)
                    return type.MakeByRefType();
                else if (mode == SymbolLoadMode.Dereference)
                    return DerefType(type);
                else
                    return type;
            }
            else if (Accept(Token.KwTrue))
            {
                MethodGenerator.PushIntConst(1);
                return IrisType.Boolean;
            }
            else if (Accept(Token.KwFalse))
            {
                MethodGenerator.PushIntConst(0);
                return IrisType.Boolean;
            }
            else if (Accept(Token.Number))
            {
                MethodGenerator.PushIntConst(_lastIntegerLexeme);
                return IrisType.Integer;
            }
            else if (Accept(Token.String))
            {
                MethodGenerator.PushString(_lexeme);
                return IrisType.String;
            }
            else if (Accept(Token.ChrOpenParen))
            {
                IrisType type = ParseExpression(mode);
                Expect(Token.ChrCloseParen);
                return type;
            }

            AddError(fp, "Expecting expression.");
            return IrisType.Invalid;
        }

        private IrisType ProcessArrayAccess(FilePosition fp, Symbol symbol, SymbolLoadMode mode)
        {
            IrisType symbolType = symbol.Type;
            IrisType resultType = IrisType.Invalid;
            if (symbolType != IrisType.Invalid)
            {
                if (!symbolType.IsArray)
                {
                    AddError(fp, string.Format("Symbol '{0}' is not an array, but is being used as an array.", _lexeme));
                }
                else
                {
                    EmitLoadSymbol(symbol, SymbolLoadMode.Dereference);
                    resultType = symbol.Type.GetElementType();
                }
            }

            FilePosition indexerPosition = _lexer.TokenStartPosition;
            IrisType indexerType = ParseExpression();
            if (indexerType != IrisType.Integer)
                AddError(indexerPosition, "Expecting integer value as array index.");

            Expect(Token.ChrCloseBracket);

            if (resultType != IrisType.Invalid)
            {
                if (mode == SymbolLoadMode.ElementAddress)
                {
                    MethodGenerator.LoadElementAddress(resultType);
                    resultType = resultType.MakeByRefType();
                }
                else if (mode == SymbolLoadMode.Element)
                {
                    MethodGenerator.LoadElement(resultType);
                }
            }

            return resultType;
        }

        private IrisType ProcessCall(FilePosition fp, Symbol symbol, bool skipArgList)
        {
            ParsedCallSyntax = true;

            IrisType symbolType = symbol.Type;
            if (!symbolType.IsMethod)
            {
                // Variables can have the same name as functions.  If the symbol is not a method,
                // try looking up the same name in the global scope.  If the global symbol is a
                // method, use it instead.
                Symbol globalSym = _symbolTable.LookupGlobal(symbol.Name);
                if (globalSym != null && globalSym.Type.IsMethod)
                {
                    symbol = globalSym;
                    symbolType = symbol.Type;
                }
            }

            IrisType resultType = IrisType.Invalid;
            bool semanticError = symbolType == IrisType.Invalid;
            if (!symbolType.IsMethod && !semanticError)
            {
                semanticError = true;
                AddError(fp, string.Format("Symbol '{0}' is not a procedure or function.", _lexeme));
            }

            string symbolTypeName = symbolType.IsFunction ? "function" : "procedure";
            Method method = symbolType as Method;
            Variable[] methodParams = method?.GetParameters();
            int count = 0;
            if (!skipArgList && !Accept(Token.ChrCloseParen))
            {
                do
                {
                    FilePosition argPosition = _lexer.TokenStartPosition;

                    if (methodParams != null && count < methodParams.Length)
                    {
                        Variable param = methodParams[count];
                        IrisType paramType = param.Type;
                        IrisType argType = ParseExpression(paramType.IsByRef ? SymbolLoadMode.Address : SymbolLoadMode.Dereference);

                        if (paramType != IrisType.Invalid && argType != IrisType.Invalid && paramType != argType)
                        {
                            if (paramType.IsByRef && !argType.IsByRef)
                            {
                                AddError(argPosition, "Cannot take address of constant, call, or expression.");
                            }
                            else
                            {
                                AddError(argPosition, string.Format(
                                    "Argument type doesn't match parameter '{0}' of {1} '{2}'",
                                    param.Name,
                                    symbolTypeName,
                                    symbol.Name));
                            }
                        }
                    }
                    else
                    {
                        // Undefined method or too many arguments.  Parse the argument without validation.
                        ParseExpression();
                    }

                    count++;
                }
                while (Accept(Token.ChrComma));

                Expect(Token.ChrCloseParen);
            }

            // Verify argument count
            if (methodParams != null && methodParams.Length != count)
            {
                AddError(fp, string.Format(
                    "Wrong number of arguments for {0} '{1}'.  {2} expected.  {3} provided.",
                    symbolTypeName,
                    symbol.Name,
                    methodParams.Length,
                    count));
            }

            if (!semanticError)
            {
                MethodGenerator.Call(symbol);
                resultType = symbolType.IsFunction ? ((Function)symbolType).ReturnType : IrisType.Void;
            }

            return resultType;
        }

        protected bool Accept(Token token)
        {
            if (_lexer.CurrentToken == token)
            {
                if (token != Token.Eof)
                    _lastParsedPosition = _lexer.TokenEndPosition;

                if (token == Token.String || token == Token.Identifier)
                    _lexeme = _lexer.GetLexeme();
                else if (token == Token.Number)
                    _lastIntegerLexeme = _lexer.ParseInteger();

                _lexer.MoveNext();
                return true;
            }

            return false;
        }

        protected void Expect(Token token)
        {
            if (!Accept(token))
                AddErrorAtTokenStart(string.Format("Expecting {0}.", Lexer.TokenName(token)));
        }

        protected Operator AcceptOperator(Operator[] map)
        {
            Operator result = map[(int)_lexer.CurrentToken];
            if (result != Operator.None)
            {
                _lastParsedPosition = _lexer.TokenEndPosition;
                _lexer.MoveNext();
            }

            return result;
        }

        private int GetNextLabel()
        {
            return _nextLabel++;
        }

        private Symbol LookupSymbol(FilePosition symbolPosition, string name)
        {
            Symbol sym = _symbolTable.Lookup(name);
            if (sym == null)
            {
                // Undefined symbol.  Emit an error and "fake" the symbol so we can continue.
                AddError(symbolPosition, string.Format("Symbol '{0}' is undefined.", name));
                sym = _symbolTable.CreateUndefinedSymbol(name);
            }

            return sym;
        }

        private IrisType ApplyTypeRules(FilePosition fp, IrisType lhs, IrisType rhs, bool boolResult = false)
        {
            // If there was a previous semantic error, don't apply any further rules.
            if (lhs == IrisType.Invalid || rhs == IrisType.Invalid)
                return IrisType.Invalid;

            string semanticError = null;
            if (lhs == IrisType.Void || rhs == IrisType.Void)
                semanticError = "Cannot apply operator to procedure call.";
            else if (lhs.IsByRef || rhs.IsByRef)
                semanticError = "Cannot take address of expression.";
            else if (lhs != rhs)
                semanticError = "Type mismatch error.";
            else if (!lhs.IsPrimitive)
                semanticError = "Operator requires a primitive type (boolean, integer, or string).";

            if (semanticError != null)
            {
                AddError(fp, semanticError);
                return IrisType.Invalid;
            }
            else
            {
                return boolResult ? IrisType.Boolean : lhs;
            }
        }

        private bool ValidateName(FilePosition fp, string name, bool global)
        {
            if (name[0] == '$')
                AddError(fp, "Identifiers starting with '$' are reserved.");

            Symbol existing = global ? _symbolTable.LookupGlobal(name) : _symbolTable.LookupLocal(name);
            if (existing != null)
            {
                AddError(fp, string.Format("Cannot redefine symbol '{0}'.", name));
                return false;
            }

            return true;
        }

        private void VerifyExpressionType(FilePosition fp, IrisType actual, IrisType expected)
        {
            if (actual != expected)
                AddError(fp, string.Format("Expecting {0} expression.", expected));
        }

        protected IrisType DerefType(IrisType type)
        {
            return type.IsByRef ? type.GetElementType() : type;
        }

        protected void AddErrorAtTokenStart(string error)
        {
            AddError(_lexer.TokenStartPosition, error);
        }

        protected void AddErrorAtLastParsedPosition(string error)
        {
            AddError(_lastParsedPosition, error);
        }

        protected void AddError(FilePosition fp, string error)
        {
            MethodGenerator.SetOutputEnabled(false);
            _context.CompileErrors.Add(fp, error);
        }

        private void Increment(Symbol symbol)
        {
            EmitLoadSymbol(symbol, SymbolLoadMode.Raw);

            bool byRef = symbol.Type.IsByRef;
            if (byRef)
            {
                MethodGenerator.Dup();
                MethodGenerator.Load(IrisType.Integer);
            }

            MethodGenerator.PushIntConst(1);
            MethodGenerator.Operator(Operator.Add);

            if (byRef)
                MethodGenerator.Store(IrisType.Integer);
            else
                EmitStoreSymbol(symbol);
        }

        protected void EmitLoadSymbol(Symbol symbol, SymbolLoadMode mode)
        {
            bool isByRef = symbol.Type.IsByRef;
            if (symbol.StorageClass == StorageClass.Argument)
            {
                if (mode == SymbolLoadMode.Address && !isByRef)
                    MethodGenerator.PushArgumentAddress(symbol.Location);
                else
                    MethodGenerator.PushArgument(symbol.Location);
            }
            else if (symbol.StorageClass == StorageClass.Local)
            {
                if (mode == SymbolLoadMode.Address && !isByRef)
                    MethodGenerator.PushLocalAddress(symbol.Location);
                else
                    MethodGenerator.PushLocal(symbol.Location);
            }
            else
            {
                if (mode == SymbolLoadMode.Address && !isByRef)
                    MethodGenerator.PushGlobalAddress(symbol);
                else
                    MethodGenerator.PushGlobal(symbol);
            }

            if (mode == SymbolLoadMode.Dereference && isByRef)
                MethodGenerator.Load(symbol.Type.GetElementType());
        }

        protected void EmitStoreSymbol(Symbol symbol)
        {
            if (symbol.StorageClass == StorageClass.Argument)
                MethodGenerator.StoreArgument(symbol.Location);
            else if (symbol.StorageClass == StorageClass.Local)
                MethodGenerator.StoreLocal(symbol.Location);
            else if (symbol.StorageClass == StorageClass.Global)
                MethodGenerator.StoreGlobal(symbol);
        }
    }
}
