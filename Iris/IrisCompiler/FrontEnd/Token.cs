// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace IrisCompiler.FrontEnd
{
    public enum Token
    {
        // Misc
        Eof,
        Identifier,
        Number,
        String,

        Eol, // 'Eol' is only used internally by the Lexer
        Skip, // 'Skip' is only used internally by the Lexer

        // Character / Character combinations
        ChrOpenParen,
        ChrCloseParen,
        ChrOpenBracket,
        ChrCloseBracket,
        ChrColon,
        ChrSemicolon,
        ChrAssign,
        ChrEqual,
        ChrNotEqual,
        ChrPlus,
        ChrMinus,
        ChrStar,
        ChrSlash,
        ChrPercent,
        ChrComma,
        ChrPeriod,
        ChrGreaterThan,
        ChrLessThan,
        ChrGreaterThanEqual,
        ChrLessThanEqual,
        ChrDotDot,

        // Keywords,
        KwAnd,
        KwArray,
        KwBegin,
        KwBoolean,
        KwDo,
        KwElse,
        KwEnd,
        KwFalse,
        KwFor,
        KwIf,
        KwInteger,
        KwFunction,
        KwOf,
        KwOr,
        KwNot,
        KwProcedure,
        KwProgram,
        KwRepeat,
        KwString,
        KwThen,
        KwTo,
        KwTrue,
        KwUntil,
        KwVar,
        KwWhile,
    }
}
