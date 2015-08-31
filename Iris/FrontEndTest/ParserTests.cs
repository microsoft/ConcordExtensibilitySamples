// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FrontEndTest
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void SimpleString()
        {
            string input =
@"'Hello World'";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldstr ""Hello World""
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void SimpleNumber()
        {
            string input =
@"4325";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4 4325
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void SimpleHexNumber()
        {
            string input =
@"#7F";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.s 127
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void SimpleFalse()
        {
            string input =
@"false";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.0
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void SimpleTrue()
        {
            string input =
@"true";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void SimpleCall()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("method", Function.Create(IrisType.Integer, new Variable[0]));

            string input =
@"method()";
            string output = TestHelpers.TestExpressionParser(input, globals);
            string expected =
@"call _Unknown
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void SimpleWithParams()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("global", IrisType.Integer);
            globals.Add("method", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[] { IrisType.Integer, IrisType.Integer, IrisType.Boolean, IrisType.Integer }));

            string input =
@"method(1, -2, false, global)";
            string output = TestHelpers.TestExpressionParser(input, globals);
            string expected =
@"ldc.i4.1
ldc.i4.2
neg
ldc.i4.0
ldsfld 0
call _Unknown
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression01()
        {
            string input =
@"1+2";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.2
add
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression02()
        {
            string input =
@"1-2";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.2
sub
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression03()
        {
            string input =
@"1*2";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.2
mul
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression04()
        {
            string input =
@"1/2";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.2
div
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression05()
        {
            string input =
@"1+2*3";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.2
ldc.i4.3
mul
add
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression06()
        {
            string input =
@"2--3";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.2
ldc.i4.3
neg
sub
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression07()
        {
            string input =
@"not false";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.0
ldc.i4.1
xor
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression08()
        {
            string input =
@"(2+3)/(3+4)";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.2
ldc.i4.3
add
ldc.i4.3
ldc.i4.4
add
div
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression09()
        {
            string input =
@"1=1";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
ceq
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression10()
        {
            string input =
@"1<>1";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
ceq
ldc.i4.1
xor
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression11()
        {
            string input =
@"1<1";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
clt
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression12()
        {
            string input =
@"1>1";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
cgt
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression13()
        {
            string input =
@"1<=1";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
cgt
ldc.i4.1
xor
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression14()
        {
            string input =
@"1>=1";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
clt
ldc.i4.1
xor
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression15()
        {
            string input =
@"true and true";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
and
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression16()
        {
            string input =
@"true or true";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
or
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression17()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer);

            string input =
@"a + 1 < 7 and a - 1 > 2";
            string output = TestHelpers.TestExpressionParser(input, globals);
            string expected =
@"ldsfld 0
ldc.i4.1
add
ldc.i4.7
clt
ldsfld 0
ldc.i4.1
sub
ldc.i4.2
cgt
and
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression18()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer.MakeArrayType());

            string input =
@"a[7]";
            string output = TestHelpers.TestExpressionParser(input, globals);
            string expected =
@"ldsfld 0
ldc.i4.7
ldelem.i4
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression19()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("strcmp", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[] { IrisType.Integer, IrisType.Integer }));
            globals.Add("a", IrisType.String);
            globals.Add("b", IrisType.String);

            string input =
@"a = b";
            string output = TestHelpers.TestExpressionParser(input, globals);
            string expected =
@"ldsfld 0
ldsfld 1
call _Unknown
ldc.i4.0
ceq
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression20()
        {
            string input =
@"'''you''re'''";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldstr ""'you're'""
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression21()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("method", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[] { IrisType.Integer.MakeByRefType() }));
            globals.Add("a", IrisType.Integer);

            string input =
@"method(a)";
            string output = TestHelpers.TestExpressionParser(input, globals);
            string expected =
@"ldsflda 0
call _Unknown
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression22()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("method", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[] { IrisType.Integer.MakeByRefType() }));
            globals.Add("a", IrisType.Integer.MakeByRefType());

            string input =
@"method(a)";
            string output = TestHelpers.TestExpressionParser(input, globals);
            string expected =
@"ldsfld 0
call _Unknown
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression23()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer.MakeByRefType());

            string input =
@"a + 1";
            string output = TestHelpers.TestExpressionParser(input, globals);
            string expected =
@"ldsfld 0
ldind.i4
ldc.i4.1
add
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression24()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("method", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[] { IrisType.Integer.MakeByRefType() }));
            globals.Add("a", IrisType.Integer.MakeArrayType());

            string input =
@"method(a[0])";
            string output = TestHelpers.TestExpressionParser(input, globals);
            string expected =
@"ldsfld 0
ldc.i4.0
ldelema int32
call _Unknown
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Expression25()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("method", TestHelpers.MakeTestFunction(IrisType.Integer, new IrisType[] { IrisType.Integer.MakeByRefType() }));
            globals.Add("a", IrisType.Integer);

            string input =
@"method((a))";
            string output = TestHelpers.TestExpressionParser(input, globals);
            string expected =
@"ldsflda 0
call _Unknown
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Comment01()
        {
            string input =
@"1 +  // Single Line Comment
1";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
add
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Comment02()
        {
            string input =
@"1 +  { Comment }
1";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
add
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Comment03()
        {
            string input =
@"1 {Comment} + 1";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
add
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Comment04()
        {
            string input =
@"{ 
   Comment
   Comment
   Comment
}
1 // Comment
+ 1";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
add
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Comment05()
        {
            string input =
@"'Not a { comment }' // Comment";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldstr ""Not a { comment }""
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Comment06()
        {
            string input =
@"
1 // Comment
{ Comment } +
1";
            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
add
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Comment07()
        {
            string input =
@"1 + 1 { Comment }";

            string output = TestHelpers.TestExpressionParser(input);
            string expected =
@"ldc.i4.1
ldc.i4.1
add
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void If01()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer);

            string input =
@"if 1 > 0 then
a := 0;";
            string output = TestHelpers.TestStatementParser(input, globals);
            string expected =
@"ldc.i4.1
ldc.i4.0
ble L0
ldc.i4.0
stsfld 0
L0:
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void If02()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer);
            string input =
@"if 1 > 0 or 1 < 0 then
   a := 0
else
   a := 1";
            string output = TestHelpers.TestStatementParser(input, globals);
            string expected =
@"ldc.i4.1
ldc.i4.0
cgt
ldc.i4.1
ldc.i4.0
clt
or
brfalse L0
ldc.i4.0
stsfld 0
br L1
L0:
ldc.i4.1
stsfld 0
L1:
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void If03()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer);
            string input =
@"if 0 > 1 then
   a := 0
else if 1 < 1 then
   a := 1
else if 2 < 1 then
   a := 2
else 
   a := 3";
            string output = TestHelpers.TestStatementParser(input, globals);
            string expected =
@"ldc.i4.0
ldc.i4.1
ble L0
ldc.i4.0
stsfld 0
br L1
L0:
ldc.i4.1
ldc.i4.1
bge L2
ldc.i4.1
stsfld 0
br L3
L2:
ldc.i4.2
ldc.i4.1
bge L4
ldc.i4.2
stsfld 0
br L5
L4:
ldc.i4.3
stsfld 0
L5:
L3:
L1:
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void While01()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer);
            string input =
@"while 1 > 0 do
   a := 0;";
            string output = TestHelpers.TestStatementParser(input, globals);
            string expected =
@"L0:
ldc.i4.1
ldc.i4.0
ble L1
ldc.i4.0
stsfld 0
br L0
L1:
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void For01()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("i", IrisType.Integer);
            globals.Add("a", IrisType.Integer);
            string input =
@"for i := 0 to 10 do
   a := 0;";
            string output = TestHelpers.TestStatementParser(input, globals);
            string expected =
@"ldc.i4.0
stsfld 0
L0:
ldsfld 0
ldc.i4.s 10
bgt L1
ldc.i4.0
stsfld 1
ldsfld 0
ldc.i4.1
add
stsfld 0
br L0
L1:
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Repeat01()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("i", IrisType.Integer);
            globals.Add("a", IrisType.Integer);
            string input =
@"
repeat
   a := 0;
   i := i + 1;
until i = 10;";
            string output = TestHelpers.TestStatementParser(input, globals);
            string expected =
@"L0:
ldc.i4.0
stsfld 1
ldsfld 0
ldc.i4.1
add
stsfld 0
ldsfld 0
ldc.i4.s 10
bne.un L0
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Assign01()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer.MakeArrayType());

            string input =
@"a[0] := a[1];";
            string output = TestHelpers.TestStatementParser(input, globals);
            string expected =
@"ldsfld 0
ldc.i4.0
ldsfld 0
ldc.i4.1
ldelem.i4
stelem.i4
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void Assign02()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("a", IrisType.Integer.MakeByRefType());

            string input =
@"a := 1;";
            string output = TestHelpers.TestStatementParser(input, globals);
            string expected =
@"ldsfld 0
ldc.i4.1
stind.i4
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void CallFunction01()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("f", Function.Create(IrisType.Integer, new Variable[0]));

            string input =
@"f;";
            string output = TestHelpers.TestStatementParser(input, globals);
            string expected =
@"call _Unknown
pop
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void CallFunction02()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("f", Function.Create(IrisType.Integer, new Variable[0]));

            string input =
@"f();";
            string output = TestHelpers.TestStatementParser(input, globals);
            string expected =
@"call _Unknown
pop
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void CallProcedure01()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("p", Procedure.Create(new Variable[0]));

            string input =
@"p;";
            string output = TestHelpers.TestStatementParser(input, globals);
            string expected =
@"call _Unknown
";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void CallProcedure02()
        {
            GlobalSymbolList globals = new GlobalSymbolList();
            globals.Add("p", Procedure.Create(new Variable[0]));

            string input =
@"p();";
            string output = TestHelpers.TestStatementParser(input, globals);
            string expected =
@"call _Unknown
";

            Assert.AreEqual(expected, output);
        }
    }
}
