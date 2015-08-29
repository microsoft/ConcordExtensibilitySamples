// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace IrisRuntime
{
    /// <summary>
    /// This class contains methods Iris programs can use if needed.
    /// </summary>
    public static class CompilerServices
    {
        private static Random s_rand;

        public static void InitStrArray(string[] a)
        {
            int len = a.Length;
            for (int i = 0; i < len; i++)
                a.SetValue(string.Empty, i);
        }

        public static int Rand()
        {
            if (s_rand == null)
                s_rand = new Random();

            return s_rand.Next();
        }
    }
}
