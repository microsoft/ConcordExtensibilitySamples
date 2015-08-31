// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;

namespace IrisCompiler.FrontEnd
{
    /// <summary>
    /// This class contains mappings between tokens and the operators they represent in different contexts.
    /// </summary>
    internal class OperatorMaps
    {
        public readonly Operator[] Logic;
        public readonly Operator[] Compare;
        public readonly Operator[] Arithmetic;
        public readonly Operator[] Term;
        public readonly Operator[] Factor;

        private static OperatorMaps s_instance;

        public static OperatorMaps Instance
        {
            get
            {
                if (s_instance == null)
                {
                    OperatorMaps maps = new OperatorMaps();
                    Interlocked.CompareExchange(ref s_instance, maps, null);
                }

                return s_instance;
            }
        }

        private OperatorMaps()
        {
            int tokenCount = Enum.GetValues(typeof(Token)).Cast<int>().Max() + 1;
            Logic = new Operator[tokenCount];
            Compare = new Operator[tokenCount];
            Arithmetic = new Operator[tokenCount];
            Term = new Operator[tokenCount];
            Factor = new Operator[tokenCount];

            Logic[(int)Token.KwAnd] = Operator.And;
            Logic[(int)Token.KwOr] = Operator.Or;
            Compare[(int)Token.ChrEqual] = Operator.Equal;
            Compare[(int)Token.ChrNotEqual] = Operator.NotEqual;
            Compare[(int)Token.ChrLessThan] = Operator.LessThan;
            Compare[(int)Token.ChrLessThanEqual] = Operator.LessThanEqual;
            Compare[(int)Token.ChrGreaterThan] = Operator.GreaterThan;
            Compare[(int)Token.ChrGreaterThanEqual] = Operator.GreaterThanEqual;
            Arithmetic[(int)Token.ChrPlus] = Operator.Add;
            Arithmetic[(int)Token.ChrMinus] = Operator.Subtract;
            Term[(int)Token.ChrStar] = Operator.Multiply;
            Term[(int)Token.ChrSlash] = Operator.Divide;
            Term[(int)Token.ChrPercent] = Operator.Modulo;
            Factor[(int)Token.ChrMinus] = Operator.Negate;
            Factor[(int)Token.KwNot] = Operator.Not;
        }
    }
}
