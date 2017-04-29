// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FrontEndTest
{
    [TestClass]
    public class AssemblyInitialize
    {
        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            // We need to have a code reference to a method in IrisRuntime.dll.  If we don't do
            // this, mstest will not deploy IrisRuntime.dll.  We use deployment when running from
            // the command line and for doing the CI build.
            IrisRuntime.CompilerServices.Rand();
        }
    }
}
