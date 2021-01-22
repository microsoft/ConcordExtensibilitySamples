// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler;
using NUnit.Framework;
using static System.FormattableString;

namespace FrontEndTest
{
    public class SamplePrograms
    {
        [Test]
        public void Program01()
        {
            string input =
@"
program HelloWorld;
begin
   writeln('Hello World');
end.
";
            string output = TestHelpers.TestCompileProgram(input);
            string expected = FixupBaseline(
@"
.assembly SYSTEM-ASSEMBLIES-HERE { }
.assembly extern IrisRuntime { }
.assembly HelloWorld { }
.class public HelloWorld
{
   .method public hidebysig static void $.main() cil managed
   {
      .entrypoint
      ldstr ""Hello World""
      call void [System.Console]System.Console::WriteLine(string)
      ret
   }
}
");

            Assert.AreEqual(expected, output);
        }
        [Test]
        public void Program02()
        {
            string input =
@"
program HelloWorld;
begin
   writeln('Hello' + ' ' + 'World');
end.
";
            string output = TestHelpers.TestCompileProgram(input);
            string expected = FixupBaseline(
@"
.assembly SYSTEM-ASSEMBLIES-HERE { }
.assembly extern IrisRuntime { }
.assembly HelloWorld { }
.class public HelloWorld
{
   .method public hidebysig static void $.main() cil managed
   {
      .entrypoint
      ldstr ""Hello""
      ldstr "" ""
      ldstr ""World""
      call string [CoreLib]System.String::Concat(string,string)
      call string [CoreLib]System.String::Concat(string,string)
      call void [System.Console]System.Console::WriteLine(string)
      ret
   }
}
");

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void Program03()
        {
            string input =
@"
program Fibbonacci;

function Fib(i:integer) : integer;
var
   a, b : integer;
begin
   Fib := 1;
   if i > 2 then
   begin
      a := Fib(i - 1);
      b := Fib(i - 2);
      Fib := a + b;
   end;
end

procedure Test(i:integer);
var
   result : integer;
begin
   result := Fib(i);
   writeln(str(result));
end;

begin
   Test(1);
   Test(2);
   Test(3);
   Test(4);
   Test(5);
end.
";
            string output = TestHelpers.TestCompileProgram(input, true);
            string expected = FixupBaseline(
@"
.assembly SYSTEM-ASSEMBLIES-HERE { }
.assembly extern IrisRuntime { }
.assembly Fibbonacci { }
.class public Fibbonacci
{
   .method public hidebysig static int32 Fib(int32 i) cil managed
   {
      .locals init ([0] int32 Fib, [1] int32 a, [2] int32 b)
      .language '{3456107b-a1f4-4d47-8e18-7cf2c54559ae}', '{5e176682-93da-497a-a5f0-f1aee5e18cce}', '{5a869d0b-6611-11d3-bd2a-0000f80849bd}'
      .line 7,7 : 1,6 'FakeFile.iris'
      nop
      .line 8,8 : 4,12 ''
      ldc.i4.1
      stloc.s 0
      .line 9,9 : 4,17 ''
      ldarg.0
      ldc.i4.2
      ble L0
      .line 10,10 : 4,9 ''
      nop
      .line 11,11 : 7,22 ''
      ldarg.0
      ldc.i4.1
      sub
      call int32 Fibbonacci::Fib(int32)
      stloc.s 1
      .line 12,12 : 7,22 ''
      ldarg.0
      ldc.i4.2
      sub
      call int32 Fibbonacci::Fib(int32)
      stloc.s 2
      .line 13,13 : 7,19 ''
      ldloc.1
      ldloc.2
      add
      stloc.s 0
      .line 14,14 : 4,7 ''
      nop
L0:
      .line 15,15 : 1,4 ''
      nop
      ldloc.0
      ret
   }
   .method public hidebysig static void Test(int32 i) cil managed
   {
      .locals init ([0] int32 result)
      .language '{3456107b-a1f4-4d47-8e18-7cf2c54559ae}', '{5e176682-93da-497a-a5f0-f1aee5e18cce}', '{5a869d0b-6611-11d3-bd2a-0000f80849bd}'
      .line 20,20 : 1,6 'FakeFile.iris'
      nop
      .line 21,21 : 4,20 ''
      ldarg.0
      call int32 Fibbonacci::Fib(int32)
      stloc.s 0
      .line 22,22 : 4,24 ''
      ldloca.s 0
      call instance string [CoreLib]System.Int32::ToString()
      call void [System.Console]System.Console::WriteLine(string)
      .line 23,23 : 1,4 ''
      nop
      ret
   }
   .method public hidebysig static void $.main() cil managed
   {
      .entrypoint
      .language '{3456107b-a1f4-4d47-8e18-7cf2c54559ae}', '{5e176682-93da-497a-a5f0-f1aee5e18cce}', '{5a869d0b-6611-11d3-bd2a-0000f80849bd}'
      .line 25,25 : 1,6 'FakeFile.iris'
      nop
      .line 26,26 : 4,11 ''
      ldc.i4.1
      call void Fibbonacci::Test(int32)
      .line 27,27 : 4,11 ''
      ldc.i4.2
      call void Fibbonacci::Test(int32)
      .line 28,28 : 4,11 ''
      ldc.i4.3
      call void Fibbonacci::Test(int32)
      .line 29,29 : 4,11 ''
      ldc.i4.4
      call void Fibbonacci::Test(int32)
      .line 30,30 : 4,11 ''
      ldc.i4.5
      call void Fibbonacci::Test(int32)
      .line 31,31 : 1,4 ''
      nop
      ret
   }
}
");

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void Program04()
        {
            string input =
@"
program Shuffle;

var
    a : array[0..9] of integer;

procedure Swap(
    var a : integer;
    var b : integer);
var
    temp : integer;
begin
    temp := a;
    a := b;
    b := temp;
end

procedure Shuffle(
    a : array of integer;
    length : integer);
var
    random : integer;
    i : integer;
begin
    while i < length do
    begin
        random := rand % length;
        if i <> random then
            Swap(a[i], a[random]);
        i := i + 1;
    end;
end

procedure Fill(
    a : array of integer;
    length : integer);
var
    i : integer;
begin
    while i < length do
    begin
        a[i] := i;
        i := i + 1;
    end;
end

begin
    Fill(a, 10);
    Shuffle(a, 10);
end.
";
            string output = TestHelpers.TestCompileProgram(input, true);
            string expected = FixupBaseline(
@"
.assembly SYSTEM-ASSEMBLIES-HERE { }
.assembly extern IrisRuntime { }
.assembly Shuffle { }
.class public Shuffle
{
   .field public static int32[] a
   .method public hidebysig static void Swap(int32& a, int32& b) cil managed
   {
      .locals init ([0] int32 temp)
      .language '{3456107b-a1f4-4d47-8e18-7cf2c54559ae}', '{5e176682-93da-497a-a5f0-f1aee5e18cce}', '{5a869d0b-6611-11d3-bd2a-0000f80849bd}'
      .line 12,12 : 1,6 'FakeFile.iris'
      nop
      .line 13,13 : 5,14 ''
      ldarg.0
      ldind.i4
      stloc.s 0
      .line 14,14 : 5,11 ''
      ldarg.0
      ldarg.1
      ldind.i4
      stind.i4
      .line 15,15 : 5,14 ''
      ldarg.1
      ldloc.0
      stind.i4
      .line 16,16 : 1,4 ''
      nop
      ret
   }
   .method public hidebysig static void Shuffle(int32[] a, int32 length) cil managed
   {
      .locals init ([0] int32 random, [1] int32 i)
      .language '{3456107b-a1f4-4d47-8e18-7cf2c54559ae}', '{5e176682-93da-497a-a5f0-f1aee5e18cce}', '{5a869d0b-6611-11d3-bd2a-0000f80849bd}'
      .line 24,24 : 1,6 'FakeFile.iris'
      nop
      .line 25,25 : 5,24 ''
L0:
      ldloc.1
      ldarg.1
      bge L1
      .line 26,26 : 5,10 ''
      nop
      .line 27,27 : 9,32 ''
      call int32 [IrisRuntime]IrisRuntime.CompilerServices::Rand()
      ldarg.1
      rem
      stloc.s 0
      .line 28,28 : 9,28 ''
      ldloc.1
      ldloc.0
      beq L2
      .line 29,29 : 13,34 ''
      ldarg.0
      ldloc.1
      ldelema int32
      ldarg.0
      ldloc.0
      ldelema int32
      call void Shuffle::Swap(int32&,int32&)
      .line 30,30 : 9,19 ''
L2:
      ldloc.1
      ldc.i4.1
      add
      stloc.s 1
      .line 31,31 : 5,8 ''
      nop
      br L0
L1:
      .line 32,32 : 1,4 ''
      nop
      ret
   }
   .method public hidebysig static void Fill(int32[] a, int32 length) cil managed
   {
      .locals init ([0] int32 i)
      .language '{3456107b-a1f4-4d47-8e18-7cf2c54559ae}', '{5e176682-93da-497a-a5f0-f1aee5e18cce}', '{5a869d0b-6611-11d3-bd2a-0000f80849bd}'
      .line 39,39 : 1,6 'FakeFile.iris'
      nop
      .line 40,40 : 5,24 ''
L3:
      ldloc.0
      ldarg.1
      bge L4
      .line 41,41 : 5,10 ''
      nop
      .line 42,42 : 9,18 ''
      ldarg.0
      ldloc.0
      dup
      stelem.i4
      .line 43,43 : 9,19 ''
      ldloc.0
      ldc.i4.1
      add
      stloc.s 0
      .line 44,44 : 5,8 ''
      nop
      br L3
L4:
      .line 45,45 : 1,4 ''
      nop
      ret
   }
   .method public hidebysig static void $.main() cil managed
   {
      .entrypoint
      .language '{3456107b-a1f4-4d47-8e18-7cf2c54559ae}', '{5e176682-93da-497a-a5f0-f1aee5e18cce}', '{5a869d0b-6611-11d3-bd2a-0000f80849bd}'
      .line 47,47 : 1,6 'FakeFile.iris'
      nop
      ldc.i4.s 10
      newarr int32
      stsfld int32[] Shuffle::a
      .line 48,48 : 5,16 ''
      ldsfld int32[] Shuffle::a
      ldc.i4.s 10
      call void Shuffle::Fill(int32[],int32)
      .line 49,49 : 5,19 ''
      ldsfld int32[] Shuffle::a
      ldc.i4.s 10
      call void Shuffle::Shuffle(int32[],int32)
      .line 50,50 : 1,4 ''
      nop
      ret
   }
}
");

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void Program05()
        {
            string input =
@"
program LineInfoTest;

var
    i : integer;
    a : array[0..9] of integer;

begin
    for i := 0 to 10 do
        a[i] := 0;

    if i = 0 then
        a[0] := 0
    else if i = 1 then
        a[1] := 1
    else
        a[2] := 1;

    i := 9;
    repeat
        a[i] := i;
        i := i - 1;
    until i < 0;
end.
";
            string output = TestHelpers.TestCompileProgram(input, true);
            string expected = FixupBaseline(
@"
.assembly SYSTEM-ASSEMBLIES-HERE { }
.assembly extern IrisRuntime { }
.assembly LineInfoTest { }
.class public LineInfoTest
{
   .field public static int32 i
   .field public static int32[] a
   .method public hidebysig static void $.main() cil managed
   {
      .entrypoint
      .language '{3456107b-a1f4-4d47-8e18-7cf2c54559ae}', '{5e176682-93da-497a-a5f0-f1aee5e18cce}', '{5a869d0b-6611-11d3-bd2a-0000f80849bd}'
      .line 8,8 : 1,6 'FakeFile.iris'
      nop
      ldc.i4.s 10
      newarr int32
      stsfld int32[] LineInfoTest::a
      .line 9,9 : 5,24 ''
      ldc.i4.0
      stsfld int32 LineInfoTest::i
L0:
      ldsfld int32 LineInfoTest::i
      ldc.i4.s 10
      bgt L1
      .line 10,10 : 9,18 ''
      ldsfld int32[] LineInfoTest::a
      ldsfld int32 LineInfoTest::i
      ldc.i4.0
      stelem.i4
      .line 9,9 : 5,24 ''
      ldsfld int32 LineInfoTest::i
      ldc.i4.1
      add
      stsfld int32 LineInfoTest::i
      br L0
L1:
      .line 12,12 : 5,18 ''
      ldsfld int32 LineInfoTest::i
      ldc.i4.0
      bne.un L2
      .line 13,13 : 9,18 ''
      ldsfld int32[] LineInfoTest::a
      ldc.i4.0
      ldc.i4.0
      stelem.i4
      br L3
L2:
      .line 14,14 : 5,23 ''
      ldsfld int32 LineInfoTest::i
      ldc.i4.1
      bne.un L4
      .line 15,15 : 9,18 ''
      ldsfld int32[] LineInfoTest::a
      ldc.i4.1
      ldc.i4.1
      stelem.i4
      br L5
L4:
      .line 16,16 : 5,9 ''
      nop
      .line 17,17 : 9,18 ''
      ldsfld int32[] LineInfoTest::a
      ldc.i4.2
      ldc.i4.1
      stelem.i4
      .line 19,19 : 5,11 ''
L5:
L3:
      ldc.i4.s 9
      stsfld int32 LineInfoTest::i
L6:
      .line 20,20 : 5,11 ''
      nop
      .line 21,21 : 9,18 ''
      ldsfld int32[] LineInfoTest::a
      ldsfld int32 LineInfoTest::i
      dup
      stelem.i4
      .line 22,22 : 9,19 ''
      ldsfld int32 LineInfoTest::i
      ldc.i4.1
      sub
      stsfld int32 LineInfoTest::i
      .line 23,23 : 5,10 ''
      nop
      ldsfld int32 LineInfoTest::i
      ldc.i4.0
      bge L6
      .line 24,24 : 1,4 ''
      nop
      ret
   }
}
");

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void Program06()
        {
            string input =
@"
program ByRefTest;

var
    i : integer;
    s : string;

procedure ByRefProc(var i : integer; var s : string);
begin
    for i := 0 to 10 do
        s := str(i);
    i := 20;
end

begin
    ByRefProc(i, s);
end.
";
            string output = TestHelpers.TestCompileProgram(input);
            string expected = FixupBaseline(
@"
.assembly SYSTEM-ASSEMBLIES-HERE { }
.assembly extern IrisRuntime { }
.assembly ByRefTest { }
.class public ByRefTest
{
   .field public static int32 i
   .field public static string s
   .method public hidebysig static void ByRefProc(int32& i, string& s) cil managed
   {
      ldarg.0
      ldc.i4.0
      stind.i4
L0:
      ldarg.0
      ldind.i4
      ldc.i4.s 10
      bgt L1
      ldarg.1
      ldarg.0
      call instance string [CoreLib]System.Int32::ToString()
      stind.ref
      ldarg.0
      dup
      ldind.i4
      ldc.i4.1
      add
      stind.i4
      br L0
L1:
      ldarg.0
      ldc.i4.s 20
      stind.i4
      ret
   }
   .method public hidebysig static void $.main() cil managed
   {
      .entrypoint
      ldsfld string [CoreLib]System.String::Empty
      stsfld string ByRefTest::s
      ldsflda int32 ByRefTest::i
      ldsflda string ByRefTest::s
      call void ByRefTest::ByRefProc(int32&,string&)
      ret
   }
}
");

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void Program07()
        {
            // Test variable initialization and case-insensitivity.
            string input =
@"
Program VarInitTest;

Procedure Proc();
Var
    a : array[0..9] of string;
    s : string;
    i : integer;
Begin
    for i := 0 to 9 do
    begin
        A[i] := Str(i);
        S := S + A[i];
    end
End

Begin
    proc();
End.
";
            string output = TestHelpers.TestCompileProgram(input);
            string expected = FixupBaseline(
@"
.assembly SYSTEM-ASSEMBLIES-HERE { }
.assembly extern IrisRuntime { }
.assembly VarInitTest { }
.class public VarInitTest
{
   .method public hidebysig static void Proc() cil managed
   {
      .locals init ([0] string[] a, [1] string s, [2] int32 i)
      ldc.i4.s 10
      newarr string
      stloc.s 0
      ldloc.0
      call void [IrisRuntime]IrisRuntime.CompilerServices::InitStrArray(string[])
      ldsfld string [CoreLib]System.String::Empty
      stloc.s 1
      ldc.i4.0
      stloc.s 2
L0:
      ldloc.2
      ldc.i4.s 9
      bgt L1
      ldloc.0
      ldloc.2
      ldloca.s 2
      call instance string [CoreLib]System.Int32::ToString()
      stelem.ref
      ldloc.1
      ldloc.0
      ldloc.2
      ldelem string
      call string [CoreLib]System.String::Concat(string,string)
      stloc.s 1
      ldloc.2
      ldc.i4.1
      add
      stloc.s 2
      br L0
L1:
      ret
   }
   .method public hidebysig static void $.main() cil managed
   {
      .entrypoint
      call void VarInitTest::Proc()
      ret
   }
}
");

            Assert.AreEqual(expected, output);
        }

        static string FixupBaseline(string expected)
        {
#if NETCOREAPP
            expected = expected.Replace(".assembly SYSTEM-ASSEMBLIES-HERE { }", @".assembly extern System.Private.CoreLib { }
.assembly extern System.Console { }");
            expected = expected.Replace("[CoreLib]", "[System.Private.CoreLib]");
#else
            expected = expected.Replace(".assembly SYSTEM-ASSEMBLIES-HERE { }", @".assembly extern mscorlib { }");
            expected = expected.Replace("[System.Console]", "[mscorlib]");
            expected = expected.Replace("[CoreLib]", "[mscorlib]");
#endif
            return expected;
        }
    }
}
