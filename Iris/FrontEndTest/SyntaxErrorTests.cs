// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FrontEndTest
{
    [TestClass]
    public class SyntaxErrorTests
    {
        [TestMethod]
        public void SyntaxError01()
        {
            string input = @"";
            TestHelpers.TestStatementParserWithError(input, @"(1, 1) Expecting statement.");
        }

        [TestMethod]
        public void SyntaxError02()
        {
            string input = @"if";
            TestHelpers.TestStatementParserWithError(input, @"(1, 3) Expecting expression.");
        }

        [TestMethod]
        public void SyntaxError03()
        {
            string input = @"if true then";
            TestHelpers.TestStatementParserWithError(input, @"(1, 13) Expecting statement.");
        }

        [TestMethod]
        public void SyntaxError04()
        {
            string input = @"1 +";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 4) Expecting expression.");
        }

        [TestMethod]
        public void SyntaxError05()
        {
            string input = @"while true begin end;";
            TestHelpers.TestStatementParserWithError(input, @"(1, 12) Expecting 'do'.");
        }

        [TestMethod]
        public void SyntaxError06()
        {
            string input = @"if true do begin end;";
            TestHelpers.TestStatementParserWithError(input, @"(1, 9) Expecting 'then'.");
        }

        [TestMethod]
        public void SyntaxError07()
        {
            string input = @"if (true then begin end;";
            TestHelpers.TestStatementParserWithError(input, @"(1, 10) Expecting ')'.");
        }

        [TestMethod]
        public void SyntaxError08()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer);

            string input = @"else a := 1";
            TestHelpers.TestStatementParserWithError(input, globals, @"(1, 6) Cannot start statement with 'else' or unexpected ';' after if statement.");
        }

        [TestMethod]
        public void SyntaxError09()
        {
            string input = @"if true then begin ;";
            TestHelpers.TestStatementParserWithError(input, @"(1, 21) Unexpected end of file looking for 'end'.");
        }

        [TestMethod]
        public void SyntaxError10()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer.MakeArrayType());

            string input = @"a[0];";
            TestHelpers.TestStatementParserWithError(input, globals, @"(1, 5) Expecting ':='.");
        }

        [TestMethod]
        public void SyntaxError11()
        {
            string input = @"procedure a; begin ; end";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 25) Unexpected end of file looking for main block.");
        }

        [TestMethod]
        public void SyntaxError12()
        {
            string input = @"procedure a; begin ; end var i : integer; begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 30) Global variables must be declared before the first function or procedure.");
        }

        [TestMethod]
        public void SyntaxError13()
        {
            string input = @"procedure a; begin ; end a;";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 26) Expecting 'function', 'procedure', or 'begin'.");
        }

        [TestMethod]
        public void SyntaxError14()
        {
            string input = @"procedure a; begin ; end begin ; end. unexpected";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 39) Expecting end of file.");
        }

        [TestMethod]
        public void SyntaxError15()
        {
            TestHelpers.TestCompileProgramWithError(string.Empty, @"(1, 1) Unexpected end of file looking for main block.");
        }

        [TestMethod]
        public void SyntaxError16()
        {
            string input = @"procedure ; begin ; end begin ; end. unexpected";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 11) Expecting procedure or function name.");
        }

        [TestMethod]
        public void SyntaxError17()
        {
            string input = @"procedure a; var a, : integer; begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 21) Expecting variable name after ','.");
        }

        [TestMethod]
        public void SyntaxError18()
        {
            string input = @"procedure a; var : integer; begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 18) Expecting variable name.");
        }

        [TestMethod]
        public void SyntaxError19()
        {
            string input = @"procedure a; var a : array['test'] of integer; begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 28) Expecting numeric constant.");
        }

        [TestMethod]
        public void SyntaxError20()
        {
            string input = @"procedure a; var a : array of integer; begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 28) Expecting array subrange.");
        }

        [TestMethod]
        public void SyntaxError21()
        {
            string input = @"procedure a(a : array[0..9] of integer); begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 22) Not expecting array subrange here.");
        }

        [TestMethod]
        public void SyntaxError22()
        {
            string input = @"procedure a(a : array of array); begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 26) Expecting  'integer', 'string', or 'boolean'");
        }

        [TestMethod]
        public void SyntaxError23()
        {
            string input = @"procedure a(a : 'test'); begin ; end begin ; end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 17) Expecting  'integer', 'string', 'boolean', or 'array'");
        }

        [TestMethod]
        public void SyntaxError24()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer);

            string input = @"for 'test' := 0 to 'test' do a := 1";
            TestHelpers.TestStatementParserWithError(input, globals, @"(1, 5) Expecting integer identifier.");
        }

        [TestMethod]
        public void SyntaxError25()
        {
            string input = @"var a : integer; begin a := 1 a := 2 end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 31) Expecting ';'.");
        }

        [TestMethod]
        public void SyntaxError26()
        {
            string input = @"var a : integer; begin a := 1;;;; a := 1 a := 2 end.";
            TestHelpers.TestCompileProgramWithError(input, @"(1, 42) Expecting ';'.");
        }

        [TestMethod]
        public void LexError01()
        {
            string input = @"~";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 1) Unexpected character.");
        }

        [TestMethod]
        public void LexError02()
        {
            string input = @"#fffffffffffffffffffff";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 1) Invalid numeric constant.");
        }

        [TestMethod]
        public void LexError03()
        {
            string input = @"2222222222222222222222";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 1) Invalid numeric constant.");
        }

        [TestMethod]
        public void LexError04()
        {
            string input = @"'aaaa";
            TestHelpers.TestExpressionParserWithError(input, @"(1, 1) Unexpected end of line looking for end of string.");
        }

        [TestMethod]
        public void LexError05()
        {
            string input =
@"{aaaa
aaaa
aaaa";
            TestHelpers.TestExpressionParserWithError(input, @"(4, 1) Unexpected end of file looking for end of comment.");
        }
    }
}
