using System;

namespace KoreForge.Jex.Lexer;

/// <summary>
/// Token types for the JEX language.
/// </summary>
public enum TokenType
{
    // End of file
    EOF,

    // Literals
    Integer,
    Decimal,
    String,
    True,
    False,
    Null,

    // Identifiers and references
    Identifier,
    VariableRef,       // &name

    // Keywords (case-insensitive)
    Let,               // %let
    Set,               // %set
    If,                // %if
    Then,              // %then
    Else,              // %else
    Do,                // %do
    End,               // %end
    Foreach,           // %foreach
    In,                // %in
    To,                // %to
    Break,             // %break
    Continue,          // %continue
    Return,            // %return
    Func,              // %func
    EndFunc,           // %endfunc

    // Operators
    Assign,            // =
    Equal,             // ==
    NotEqual,          // !=
    LessThan,          // <
    LessOrEqual,       // <=
    GreaterThan,       // >
    GreaterOrEqual,    // >=
    Plus,              // +
    Minus,             // -
    Multiply,          // *
    Divide,            // /
    Modulo,            // %
    And,               // &&
    Or,                // ||
    Not,               // !

    // Punctuation
    LeftParen,         // (
    RightParen,        // )
    LeftBrace,         // {
    RightBrace,        // }
    LeftBracket,       // [
    RightBracket,      // ]
    Comma,             // ,
    Semicolon,         // ;
    Colon,             // :
    Dot,               // .
    Dollar,            // $
}

/// <summary>
/// Represents a token in the JEX source code.
/// </summary>
public readonly record struct Token(TokenType Type, string Lexeme, object? Value, SourceSpan Span)
{
    public override string ToString() => $"{Type}({Lexeme}) at {Span}";
}

/// <summary>
/// Tokenizer for the JEX language.
/// </summary>
public sealed class JexLexer
{
    private readonly string _source;
    private int _start;
    private int _current;
    private int _line = 1;
    private int _column = 1;
    private int _startLine;
    private int _startColumn;

    public JexLexer(string source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public Token NextToken()
    {
        SkipWhitespaceAndComments();

        _start = _current;
        _startLine = _line;
        _startColumn = _column;

        if (IsAtEnd())
            return MakeToken(TokenType.EOF);

        char c = Advance();

        // Identifiers and keywords starting with %
        if (c == '%')
        {
            return ScanKeywordOrModulo();
        }

        // Variable references &name or && operator
        if (c == '&')
        {
            // Check if it's && operator first
            if (Match('&'))
            {
                return MakeToken(TokenType.And);
            }
            return ScanVariableRef();
        }

        // Identifiers
        if (IsAlpha(c))
        {
            return ScanIdentifier();
        }

        // Numbers
        if (IsDigit(c))
        {
            return ScanNumber();
        }

        // Strings
        if (c == '"')
        {
            return ScanString();
        }

        // Operators and punctuation
        return c switch
        {
            '(' => MakeToken(TokenType.LeftParen),
            ')' => MakeToken(TokenType.RightParen),
            '{' => MakeToken(TokenType.LeftBrace),
            '}' => MakeToken(TokenType.RightBrace),
            '[' => MakeToken(TokenType.LeftBracket),
            ']' => MakeToken(TokenType.RightBracket),
            ',' => MakeToken(TokenType.Comma),
            ';' => MakeToken(TokenType.Semicolon),
            ':' => MakeToken(TokenType.Colon),
            '.' => MakeToken(TokenType.Dot),
            '$' => MakeToken(TokenType.Dollar),
            '+' => MakeToken(TokenType.Plus),
            '-' => MakeToken(TokenType.Minus),
            '*' => MakeToken(TokenType.Multiply),
            '/' => MakeToken(TokenType.Divide),
            '=' => Match('=') ? MakeToken(TokenType.Equal) : MakeToken(TokenType.Assign),
            '!' => Match('=') ? MakeToken(TokenType.NotEqual) : MakeToken(TokenType.Not),
            '<' => Match('=') ? MakeToken(TokenType.LessOrEqual) : MakeToken(TokenType.LessThan),
            '>' => Match('=') ? MakeToken(TokenType.GreaterOrEqual) : MakeToken(TokenType.GreaterThan),
            '|' => Match('|') ? MakeToken(TokenType.Or) : throw MakeError($"Unexpected character '|'. Did you mean '||'?"),
            _ => throw MakeError($"Unexpected character '{c}'")
        };
    }

    private Token ScanKeywordOrModulo()
    {
        // Check if this is a keyword or modulo operator
        if (!IsAlpha(Peek()))
        {
            // Just % by itself is modulo
            return MakeToken(TokenType.Modulo);
        }

        // Scan the keyword
        while (IsAlphaNumeric(Peek()))
            Advance();

        string text = _source[_start.._current];
        string keyword = text.ToLowerInvariant();

        return keyword switch
        {
            "%let" => MakeToken(TokenType.Let),
            "%set" => MakeToken(TokenType.Set),
            "%if" => MakeToken(TokenType.If),
            "%then" => MakeToken(TokenType.Then),
            "%else" => MakeToken(TokenType.Else),
            "%do" => MakeToken(TokenType.Do),
            "%end" => MakeToken(TokenType.End),
            "%foreach" => MakeToken(TokenType.Foreach),
            "%in" => MakeToken(TokenType.In),
            "%to" => MakeToken(TokenType.To),
            "%break" => MakeToken(TokenType.Break),
            "%continue" => MakeToken(TokenType.Continue),
            "%return" => MakeToken(TokenType.Return),
            "%func" => MakeToken(TokenType.Func),
            "%endfunc" => MakeToken(TokenType.EndFunc),
            _ => throw MakeError($"Unknown keyword '{text}'")
        };
    }

    private Token ScanVariableRef()
    {
        if (!IsAlpha(Peek()) && Peek() != '_')
        {
            throw MakeError("Expected variable name after '&'");
        }

        while (IsAlphaNumeric(Peek()) || Peek() == '_')
            Advance();

        string name = _source[(_start + 1).._current]; // Skip the &
        return MakeToken(TokenType.VariableRef, name);
    }

    private Token ScanIdentifier()
    {
        while (IsAlphaNumeric(Peek()) || Peek() == '_')
            Advance();

        string text = _source[_start.._current];
        string lower = text.ToLowerInvariant();

        return lower switch
        {
            "true" => MakeToken(TokenType.True, true),
            "false" => MakeToken(TokenType.False, false),
            "null" => MakeToken(TokenType.Null, null),
            _ => MakeToken(TokenType.Identifier, text)
        };
    }

    private Token ScanNumber()
    {
        while (IsDigit(Peek()))
            Advance();

        bool isDecimal = false;
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            isDecimal = true;
            Advance(); // Consume the '.'
            while (IsDigit(Peek()))
                Advance();
        }

        string text = _source[_start.._current];
        if (isDecimal)
        {
            if (decimal.TryParse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal d))
                return MakeToken(TokenType.Decimal, d);
            throw MakeError($"Invalid decimal number '{text}'");
        }
        else
        {
            if (long.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long l))
                return MakeToken(TokenType.Integer, l);
            throw MakeError($"Invalid integer '{text}'");
        }
    }

    private Token ScanString()
    {
        var sb = new System.Text.StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\n')
            {
                _line++;
                _column = 1;
            }

            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd())
                    throw MakeError("Unterminated string");

                char escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    _ => throw MakeError($"Invalid escape sequence '\\{escaped}'")
                });
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (IsAtEnd())
            throw MakeError("Unterminated string");

        Advance(); // Closing "
        return MakeToken(TokenType.String, sb.ToString());
    }

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            char c = Peek();
            switch (c)
            {
                case ' ':
                case '\r':
                case '\t':
                    Advance();
                    break;
                case '\n':
                    _line++;
                    _column = 1;
                    _current++;
                    break;
                case '/':
                    if (PeekNext() == '/')
                    {
                        // Single-line comment
                        while (!IsAtEnd() && Peek() != '\n')
                            Advance();
                    }
                    else if (PeekNext() == '*')
                    {
                        // Block comment
                        Advance(); // /
                        Advance(); // *
                        while (!IsAtEnd())
                        {
                            if (Peek() == '\n')
                            {
                                _line++;
                                _column = 1;
                                _current++;
                            }
                            else if (Peek() == '*' && PeekNext() == '/')
                            {
                                Advance();
                                Advance();
                                break;
                            }
                            else
                            {
                                Advance();
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                    break;
                default:
                    return;
            }
        }
    }

    private bool IsAtEnd() => _current >= _source.Length;
    private char Peek() => IsAtEnd() ? '\0' : _source[_current];
    private char PeekNext() => _current + 1 >= _source.Length ? '\0' : _source[_current + 1];

    private char Advance()
    {
        char c = _source[_current++];
        _column++;
        return c;
    }

    private bool Match(char expected)
    {
        if (IsAtEnd() || _source[_current] != expected)
            return false;
        Advance();
        return true;
    }

    private static bool IsDigit(char c) => c >= '0' && c <= '9';
    private static bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    private static bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);

    private Token MakeToken(TokenType type, object? value = null)
    {
        string lexeme = _source[_start.._current];
        var startPos = new SourcePosition(_startLine, _startColumn, _start);
        var endPos = new SourcePosition(_line, _column, _current);
        return new Token(type, lexeme, value, new SourceSpan(startPos, endPos));
    }

    private JexCompileException MakeError(string message)
    {
        var startPos = new SourcePosition(_startLine, _startColumn, _start);
        var endPos = new SourcePosition(_line, _column, _current);
        return new JexCompileException(message, new SourceSpan(startPos, endPos));
    }
}
