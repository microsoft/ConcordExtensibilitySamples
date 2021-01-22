// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler;
using NUnit.Framework;

namespace FrontEndTest
{
    public class SemanticTests
    {
        [Test]
        public void SemanticError01()
        {
            string input = @"not 'Hello World'";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 5) Unary operators cannot be applied to string values.");
        }

        [Test]
        public void SemanticError02()
        {
            string input = @"-false";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 2) Unary negate operator cannot be applied to boolean values.");
        }

        [Test]
        public void SemanticError03()
        {
            string input = @"'a' - 'b'";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 5) Only the '+' or comparison operators can be used on string values.");
        }

        [Test]
        public void SemanticError04()
        {
            string input = @"false + true";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 7) Arithmetic operators cannot be applied to boolean values.");
        }

        [Test]
        public void SemanticError05()
        {
            string input = @"a";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 1) Symbol 'a' is undefined.");
        }

        [Test]
        public void SemanticError06()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", Procedure.Create(new Variable[0]));
            string input = @"not a";
            TestHelpers.TestExpressionParserWithError(input, globals, @"(1, 5) Expecting boolean expression.");
        }

        [Test]
        public void SemanticError07()
        {
            string input = @"f(true)";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 1) Symbol 'f' is undefined.");
        }

        [Test]
        public void SemanticError08()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", Procedure.Create(new Variable[] { new Variable(IrisType.Boolean, "b") }));
            string input = @"a(true) + 1";
            TestHelpers.TestExpressionParserWithError(input, globals, @"(1, 9) Cannot apply operator to procedure call.");
        }

        [Test]
        public void SemanticError09()
        {
            string input = @"a[0]";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 1) Symbol 'a' is undefined.");
        }

        [Test]
        public void SemanticError10()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer);
            string input = @"a[0]";
            TestHelpers.TestExpressionParserWithError(input, globals, @"(1, 1) Symbol 'a' is not an array, but is being used as an array.");
        }

        [Test]
        public void SemanticError11()
        {
            string input = @"1 + true";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 3) Type mismatch error.");
        }

        [Test]
        public void SemanticError12()
        {
            string input = @"1 > true";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 3) Type mismatch error.");
        }

        [Test]
        public void SemanticError13()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer.MakeArrayType());
            string input = @"a + a";
            TestHelpers.TestExpressionParserWithError(input, globals, @"(1, 3) Operator requires a primitive type (boolean, integer, or string).");
        }

        [Test]
        public void SemanticError14()
        {
            string input = @"not 1";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 5) Expecting boolean expression.");
        }

        [Test]
        public void SemanticError15()
        {
            string input = @"while 1 do begin end;";
            TestHelpers.TestStatementParserWithError(input, @"(1, 7) Expecting boolean expression.");
        }

        [Test]
        public void SemanticError16()
        {
            string input = @"if 'test' do begin end;";
            TestHelpers.TestStatementParserWithError(input, @"(1, 4) Expecting boolean expression.");
        }

        [Test]
        public void SemanticError17()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("method", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[0]));

            string input = @"method(1)";
            TestHelpers.TestExpressionParserWithError(input, globals, @"(1, 1) Wrong number of arguments for function 'method'.  0 expected.  1 provided.");
        }

        [Test]
        public void SemanticError18()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("method", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[] { IrisType.Integer }));

            string input = @"method(false)";
            TestHelpers.TestExpressionParserWithError(input, globals, @"(1, 8) Argument type doesn't match parameter 'p0' of function 'method'");
        }

        [Test]
        public void SemanticError19()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer.MakeArrayType());

            string input = @"a[true]";
            TestHelpers.TestExpressionParserWithError(input, globals, @"(1, 3) Expecting integer value as array index.");
        }

        [Test]
        public void SemanticError20()
        {
            string input = @"a := 1";
            TestHelpers.TestStatementParserWithError(input, @"(1, 3) Symbol 'a' is undefined.");
        }

        [Test]
        public void SemanticError21()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer.MakeArrayType());

            string input = @"a := 1";
            TestHelpers.TestStatementParserWithError(input, globals, @"(1, 6) Cannot assign to 'a' (type mismatch error).");
        }

        [Test]
        public void SemanticError22()
        {
            string input = @"procedure a : integer; begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 22) Procedure cannot have return value.");
        }

        [Test]
        public void SemanticError23()
        {
            string input = @"function a; begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 11) Expecting return type for function.");
        }

        [Test]
        public void SemanticError24()
        {
            string input = @"var a, a : integer; procedure b; begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 8) Cannot redefine symbol 'a'.");
        }

        [Test]
        public void SemanticError25()
        {
            string input = @"var a : integer; procedure a; begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 28) Cannot redefine symbol 'a'.");
        }

        [Test]
        public void SemanticError26()
        {
            string input = @"procedure a; begin ; end function a : integer; begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 35) Cannot redefine symbol 'a'.");
        }

        [Test]
        public void SemanticError27()
        {
            string input = @"procedure a; var a, a : integer; begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 21) Cannot redefine symbol 'a'.");
        }

        [Test]
        public void SemanticError28()
        {
            string input = @"procedure a(a,a:integer); begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 15) Cannot redefine symbol 'a'.");
        }

        [Test]
        public void SemanticError29()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("method", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[] { IrisType.Integer.MakeByRefType() }));

            string input = @"method(false)";
            TestHelpers.TestExpressionParserWithError(input, globals, @"(1, 8) Cannot take address of constant, call, or expression.");
        }

        [Test]
        public void SemanticError30()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("method", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[] { IrisType.Integer.MakeByRefType() }));
            globals.Add("method2", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[0]));

            string input = @"method(method2)";
            TestHelpers.TestExpressionParserWithError(input, globals, @"(1, 8) Cannot take address of constant, call, or expression.");
        }

        [Test]
        public void SemanticError31()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer);

            string input = @"a(false)";
            TestHelpers.TestStatementParserWithError(input, globals, @"(1, 2) Symbol 'a' is not a procedure or function.");
        }

        [Test]
        public void SemanticError32()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer.MakeArrayType());
            globals.Add("b", IrisType.Integer);

            string input = @"for a := 0 to 10 do b := 1";
            TestHelpers.TestStatementParserWithError(input, globals, @"(1, 5) Expecting integer expression.");
        }

        [Test]
        public void SemanticError33()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer);
            globals.Add("b", IrisType.Integer);

            string input = @"for a := 'test' to 0 do b := 1";
            TestHelpers.TestStatementParserWithError(input, globals, @"(1, 10) Expecting integer expression.");
        }

        [Test]
        public void SemanticError34()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer);
            globals.Add("b", IrisType.Integer);

            string input = @"for a := 0 to 'test' do b := 1";
            TestHelpers.TestStatementParserWithError(input, globals, @"(1, 15) Expecting integer expression.");
        }

        [Test]
        public void SemanticError35()
        {
            string input = @"procedure a; var $a : integer; begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 18) Identifiers starting with '$' are reserved.");
        }

        [Test]
        public void SemanticError36()
        {
            string input = @"procedure a; var a : array[1..10] of integer; begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 27) Iris only support arrays that start at index zero.");
        }

        [Test]
        public void SemanticError37()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", Procedure.Create(new Variable[0]));
            globals.Add("b", IrisType.Integer);

            string input = @"a := b";
            TestHelpers.TestStatementParserWithError(input, globals, @"(1, 3) Cannot assign to result of function or procedure call.");
        }

        [Test]
        public void SemanticError38()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", Procedure.Create(new Variable[0]));
            globals.Add("b", IrisType.Integer);

            string input = @"b := a";
            TestHelpers.TestStatementParserWithError(input, globals, @"(1, 3) Cannot use procedure in assignment statement.");
        }

        [Test]
        public void SemanticError39()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("method", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[] { IrisType.Integer }));

            string input = @"method";
            TestHelpers.TestExpressionParserWithError(input, globals, @"(1, 1) Wrong number of arguments for function 'method'.  1 expected.  0 provided.");
        }

        [Test]
        public void SemanticError40()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("method", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[] { IrisType.Integer.MakeByRefType() }));
            globals.Add("a", IrisType.Integer);

            string input = @"method(-a)";
            TestHelpers.TestExpressionParserWithError(input, globals, @"(1, 8) Cannot take address of constant, call, or expression.");
        }

        [Test]
        public void SemanticError41()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("method", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[] { IrisType.Integer.MakeByRefType() }));
            globals.Add("a", IrisType.Integer);

            string input = @"method(a + 1)";
            TestHelpers.TestExpressionParserWithError(input, globals, @"(1, 10) Cannot take address of expression.");
        }
    }
}
