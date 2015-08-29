// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace IrisCompiler
{
    public enum Operator
    {
        None = 0,

        // Compare
        Equal,
        NotEqual,
        LessThan,
        LessThanEqual,
        GreaterThan,
        GreaterThanEqual,

        // Arithmetic
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,

        // Logical
        And,
        Or,

        // Unary
        Negate,
        Not,
    }
}
