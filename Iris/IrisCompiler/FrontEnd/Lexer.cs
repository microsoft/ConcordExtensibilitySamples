// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace IrisCompiler.FrontEnd
{
    /// <summary>
    /// This class is the Lexical Analyzer for the Iris compiler.
    /// </summary>
    public class Lexer
    {
        private static Dictionary<string, Token> s_keywords;

        private ErrorList _errors;
        private StreamReader _reader;
        private string _lineText;
        private int _lineLen;
        private int _tokenLen;
        private int _line;
        private int _col;

        private Lexer(StreamReader reader, ErrorList errors)
        {
            _errors = errors;
            _reader = reader;

            CurrentToken = Token.Eol;
        }

        #region Properties

        public Token CurrentToken
        {
            get;
            private set;
        }

        public FilePosition TokenStartPosition
        {
            get
            {
                return new FilePosition(_line, _col + 1);
            }
        }

        public FilePosition TokenEndPosition
        {
            get
            {
                return new FilePosition(_line, _col + _tokenLen + 1);
            }
        }

        #endregion

        public static Lexer Create(StreamReader reader, ErrorList errors)
        {
            Lexer lex = new Lexer(reader, errors);
            lex.MoveNext();

            return lex;
        }

        /// <summary>
        /// Gets the name of the token.  This is used in error messages so literal names such as
        /// operators (ex. &quot;:=&quot;) are wrapped with single quotes.
        /// </summary>
        /// <param name="token">Token value</param>
        /// <returns>Token name</returns>
        public static string TokenName(Token token)
        {
            string constName = Enum.GetName(typeof(Token), token).ToLower();
            if (constName.StartsWith("kw"))
                return "'" + constName.Substring(2) + "'";

            if (constName.StartsWith("chr"))
            {
                switch (token)
                {
                    case Token.ChrOpenParen:
                        return "'('";
                    case Token.ChrCloseParen:
                        return "')'";
                    case Token.ChrOpenBracket:
                        return "'['";
                    case Token.ChrCloseBracket:
                        return "']'";
                    case Token.ChrColon:
                        return "':'";
                    case Token.ChrSemicolon:
                        return "';'";
                    case Token.ChrAssign:
                        return "':='";
                    case Token.ChrEqual:
                        return "'='";
                    case Token.ChrNotEqual:
                        return "'<>'";
                    case Token.ChrPlus:
                        return "'+'";
                    case Token.ChrMinus:
                        return "'-'";
                    case Token.ChrStar:
                        return "'*'";
                    case Token.ChrSlash:
                        return "'/'";
                    case Token.ChrPercent:
                        return "'%'";
                    case Token.ChrComma:
                        return "','";
                    case Token.ChrPeriod:
                        return "'.'";
                    case Token.ChrGreaterThan:
                        return "'>'";
                    case Token.ChrLessThan:
                        return "'<'";
                    case Token.ChrGreaterThanEqual:
                        return "'>='";
                    case Token.ChrLessThanEqual:
                        return "'<='";
                    case Token.ChrDotDot:
                        return "'..'";
                }
            }

            switch (token)
            {
                case Token.Eof:
                    return "end of file";
                case Token.Number:
                    return "numeric constant";
                case Token.String:
                    return "string constant";
                case Token.Identifier:
                    return "identifier";
                default:
                    throw new NotSupportedException("Can't get name of token.  Missing case?");
            }
        }

        public void Reset()
        {
            CurrentToken = Token.Eol;
            _reader.BaseStream.Seek(0, SeekOrigin.Begin);
            MoveNext();
        }

        public void MoveNext()
        {
            do
            {
                if (CurrentToken != Token.Eol && CurrentToken != Token.Eof)
                    MoveNextOnLine();

                while (CurrentToken == Token.Eol)
                    BeginNewLine();
            }
            while (CurrentToken == Token.Skip);
        }

        public string GetLexeme()
        {
            string lexeme = GetRawLexeme();
            if (CurrentToken == Token.String)
            {
                // Trim the quotes off the string ends and unescape internal quotes.
                int len = lexeme.Length - 1;

                if (lexeme.EndsWith("\'"))
                    len--;

                lexeme = lexeme.Substring(1, len);
                lexeme = lexeme.Replace("''", "'");
            }

            return lexeme;
        }

        public int ParseInteger()
        {
            string lexeme = GetRawLexeme();
            NumberStyles style = NumberStyles.None;
            if (lexeme.StartsWith("#"))
            {
                lexeme = lexeme.Substring(1, lexeme.Length - 1);
                style = NumberStyles.AllowHexSpecifier;
            }

            int result;
            if (int.TryParse(lexeme, style, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            AddError("Invalid numeric constant.");
            return 0;
        }

        private string GetRawLexeme()
        {
            if (_tokenLen == 0)
                return string.Empty;
            else
                return _lineText.Substring(_col, _tokenLen);
        }

        private void BeginNewLine()
        {
            _line++;
            _col = 0;
            _tokenLen = 0;
            _lineText = _reader.ReadLine();
            if (_lineText == null)
            {
                _lineLen = 0;
                CurrentToken = Token.Eof;
            }
            else
            {
                _lineLen = _lineText.Length;
                MoveNextOnLine();
            }
        }

        private void MoveNextOnLine()
        {
            AcceptMany(IsWhitespace);

            _col += _tokenLen;

            if (_col >= _lineLen)
            {
                _tokenLen = 0;
                CurrentToken = Token.Eol;
                return;
            }

            _tokenLen = 1; // Assume token is one character

            char c = _lineText[_col];
            if (IsAlpha(c))
            {
                ProcessWord();
                return;
            }

            if (IsDigit(c))
            {
                CurrentToken = Token.Number;
                AcceptMany(IsDigit);
                return;
            }

            switch (c)
            {
                case '(':
                    CurrentToken = Token.ChrOpenParen;
                    break;
                case ')':
                    CurrentToken = Token.ChrCloseParen;
                    break;
                case '[':
                    CurrentToken = Token.ChrOpenBracket;
                    break;
                case ']':
                    CurrentToken = Token.ChrCloseBracket;
                    break;
                case ':':
                    if (Accept('='))
                        CurrentToken = Token.ChrAssign;
                    else
                        CurrentToken = Token.ChrColon;
                    break;
                case ';':
                    CurrentToken = Token.ChrSemicolon;
                    break;
                case '=':
                    CurrentToken = Token.ChrEqual;
                    break;
                case '+':
                    CurrentToken = Token.ChrPlus;
                    break;
                case '-':
                    CurrentToken = Token.ChrMinus;
                    break;
                case '*':
                    CurrentToken = Token.ChrStar;
                    break;
                case '/':
                    if (Accept('/'))
                    {
                        // Comment - treat as EOL
                        _tokenLen = 0;
                        CurrentToken = Token.Eol;
                    }
                    else
                    {
                        CurrentToken = Token.ChrSlash;
                    }
                    break;
                case '%':
                    CurrentToken = Token.ChrPercent;
                    break;
                case ',':
                    CurrentToken = Token.ChrComma;
                    break;
                case '>':
                    if (Accept('='))
                        CurrentToken = Token.ChrGreaterThanEqual;
                    else
                        CurrentToken = Token.ChrGreaterThan;
                    break;
                case '<':
                    if (Accept('='))
                        CurrentToken = Token.ChrLessThanEqual;
                    else if (Accept('>'))
                        CurrentToken = Token.ChrNotEqual;
                    else
                        CurrentToken = Token.ChrLessThan;
                    break;
                case '_':
                case '$': // Used for special identifiers in the debugger
                    ProcessWord();
                    break;
                case '#':
                    CurrentToken = Token.Number;
                    AcceptMany(IsHexDigit);
                    break;
                case '\'':
                    CurrentToken = Token.String;
                    ProcessString();
                    break;
                case '.':
                    if (Accept('.'))
                        CurrentToken = Token.ChrDotDot;
                    else
                        CurrentToken = Token.ChrPeriod;
                    break;
                case '{':
                    ProcessMultilineComment();
                    break;
                default:
                    AddError("Unexpected character.");
                    CurrentToken = Token.Skip;
                    break;
            }
        }

        private void AddError(string error)
        {
            _errors.Add(_line, _col + 1, error);
        }

        private void ProcessWord()
        {
            AcceptMany(c => (IsAlpha(c) || IsDigit(c) || c == '_'));
            Token kw;
            if (GetKeywordMap().TryGetValue(GetRawLexeme(), out kw))
            {
                CurrentToken = kw;
            }
            else
            {
                CurrentToken = Token.Identifier;
            }
        }

        private void ProcessString()
        {
            do
            {
                AcceptMany(c => c != '\'');
                if (!Accept('\''))
                {
                    AddError("Unexpected end of line looking for end of string.");
                    return;
                }
            }
            while (Accept('\''));
        }

        private void ProcessMultilineComment()
        {
            _col = _lineText.IndexOf('}', _col);
            while (_col == -1)
            {
                _line++;
                _lineText = _reader.ReadLine();
                if (_lineText == null)
                {
                    _col = 0;
                    _lineLen = 0;
                    CurrentToken = Token.Eof;
                    AddError("Unexpected end of file looking for end of comment.");

                    return;
                }

                _lineLen = _lineText.Length;
                _col = _lineText.IndexOf('}');
            }

            _tokenLen = 0;
            _col++;

            CurrentToken = _col >= _lineLen ? Token.Eol : Token.Skip;
        }

        private bool Accept(char c)
        {
            if (PeekNext() == c)
            {
                _tokenLen++;
                return true;
            }

            return false;
        }

        private void AcceptMany(Func<char, bool> testFunction)
        {
            for (; ;)
            {
                char c = PeekNext();
                if (c == 0)
                    break;
                if (!testFunction(c))
                    break;

                _tokenLen++;
            }
        }

        private char PeekNext()
        {
            int next = _col + _tokenLen;
            if (next < _lineLen)
            {
                return _lineText[next];
            }

            return (char)0;
        }

        private static Dictionary<string, Token> GetKeywordMap()
        {
            if (s_keywords == null)
            {
                Dictionary<string, Token> keywords = new Dictionary<string, Token>(StringComparer.InvariantCultureIgnoreCase);
                keywords.Add("and", Token.KwAnd);
                keywords.Add("array", Token.KwArray);
                keywords.Add("begin", Token.KwBegin);
                keywords.Add("boolean", Token.KwBoolean);
                keywords.Add("do", Token.KwDo);
                keywords.Add("else", Token.KwElse);
                keywords.Add("end", Token.KwEnd);
                keywords.Add("false", Token.KwFalse);
                keywords.Add("for", Token.KwFor);
                keywords.Add("function", Token.KwFunction);
                keywords.Add("if", Token.KwIf);
                keywords.Add("integer", Token.KwInteger);
                keywords.Add("not", Token.KwNot);
                keywords.Add("of", Token.KwOf);
                keywords.Add("or", Token.KwOr);
                keywords.Add("procedure", Token.KwProcedure);
                keywords.Add("program", Token.KwProgram);
                keywords.Add("repeat", Token.KwRepeat);
                keywords.Add("string", Token.KwString);
                keywords.Add("then", Token.KwThen);
                keywords.Add("to", Token.KwTo);
                keywords.Add("true", Token.KwTrue);
                keywords.Add("until", Token.KwUntil);
                keywords.Add("var", Token.KwVar);
                keywords.Add("while", Token.KwWhile);

                Interlocked.CompareExchange(ref s_keywords, keywords, null);
            }

            return s_keywords;
        }

        private static bool IsWhitespace(char c)
        {
            return c == ' ' || c == '\t';
        }

        private static bool IsAlpha(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        private static bool IsHexDigit(char c)
        {
            return IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c >= 'f');
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }
    }
}
