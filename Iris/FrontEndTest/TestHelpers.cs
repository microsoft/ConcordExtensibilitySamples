// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler;
using NUnit.Framework;

namespace FrontEndTest
{
    internal static class TestHelpers
    {
        public static void Setup()
        {
            // We need to have a code reference to a method in IrisRuntime.dll to ensure that the
            // CI system deploys it.
            IrisRuntime.CompilerServices.Rand();
        }

        public static string TestExpressionParser(string compiland, GlobalSymbolList symbols = null)
        {
            using (TestCompilerContext context = TestCompilerContext.Create(compiland, symbols, CompilationFlags.NoDebug))
            {
                context.ParseExpression();
                Assert.AreEqual(0, context.ErrorCount, context.FirstError.ToString());
                return context.GetCompilerOutput();
            }
        }

        public static void TestExpressionParserWithError(string compiland, string expectedError)
        {
            TestExpressionParserWithError(compiland, null, expectedError);
        }

        public static void TestExpressionParserWithError(string compiland, GlobalSymbolList symbols, string expectedError)
        {
            using (TestCompilerContext context = TestCompilerContext.Create(compiland, symbols, CompilationFlags.NoDebug))
            {
                context.ParseExpression();
                Assert.IsTrue(context.ErrorCount > 0, "Expecting compiler error");
                Assert.AreEqual(expectedError, context.FirstError);
            }
        }

        public static string TestStatementParser(string compiland, GlobalSymbolList symbols = null)
        {
            using (TestCompilerContext context = TestCompilerContext.Create(compiland, symbols, CompilationFlags.NoDebug))
            {
                context.ParseStatement();
                Assert.AreEqual(0, context.ErrorCount, context.FirstError.ToString());
                return context.GetCompilerOutput();
            }
        }

        public static void TestStatementParserWithError(string compiland, string expectedError)
        {
            TestStatementParserWithError(compiland, null, expectedError);
        }

        public static void TestStatementParserWithError(string compiland, GlobalSymbolList symbols, string expectedError)
        {
            using (TestCompilerContext context = TestCompilerContext.Create(compiland, symbols, CompilationFlags.NoDebug))
            {
                context.ParseStatement();
                Assert.IsTrue(context.ErrorCount > 0, "Expecting compiler error");
                Assert.AreEqual(expectedError, context.FirstError);
            }
        }

        public static string TestCompileProgram(string compiland, bool writeDebugInfo = false)
        {
            CompilationFlags flags = writeDebugInfo ? CompilationFlags.None : CompilationFlags.NoDebug;
            using (TestCompilerContext context = TestCompilerContext.Create(compiland, null, flags))
            {
                context.ParseProgram();
                Assert.AreEqual(0, context.ErrorCount, context.FirstError.ToString());
                return context.GetCompilerOutput();
            }
        }

        public static void TestCompileProgramWithError(string compiland, string expectedError)
        {
            using (TestCompilerContext context = TestCompilerContext.Create(compiland, null, CompilationFlags.NoDebug))
            {
                context.ParseProgram();
                Assert.IsTrue(context.ErrorCount > 0, "Expecting compiler error");
                Assert.AreEqual(expectedError, context.FirstError);
            }
        }

        public static Function MakeTestFunction(IrisType returnType, IrisType[] paramTypes)
        {
            Variable[] parameters = new Variable[paramTypes.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                string name = string.Format("p{0}", i);
                parameters[i] = new Variable(paramTypes[i], name);
            }

            return Function.Create(returnType, parameters);
        }
    }
}
