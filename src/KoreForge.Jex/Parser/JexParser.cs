using System;
using System.Collections.Generic;
using KoreForge.Jex.Lexer;

namespace KoreForge.Jex.Parser;

/// <summary>
/// Parser for the JEX language. Transforms tokens into an AST.
/// </summary>
public sealed class JexParser
{
    private readonly JexLexer _lexer;
    private Token _current;
    private Token _previous;

    public JexParser(string source)
    {
        _lexer = new JexLexer(source);
        _current = _lexer.NextToken();
    }

    public Program Parse()
    {
        var statements = new List<Statement>();
        var start = _current.Span.Start;

        while (!IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt is not null)
                statements.Add(stmt);
        }

        var span = new SourceSpan(start, _previous.Span.End);
        return new Program(statements, span);
    }

    private Statement? ParseStatement()
    {
        if (Check(TokenType.Let)) return ParseLetStatement();
        if (Check(TokenType.Set)) return ParseSetStatement();
        if (Check(TokenType.If)) return ParseIfStatement();
        if (Check(TokenType.Foreach)) return ParseForeachStatement();
        if (Check(TokenType.Do)) return ParseDoLoopStatement();
        if (Check(TokenType.Break)) return ParseBreakStatement();
        if (Check(TokenType.Continue)) return ParseContinueStatement();
        if (Check(TokenType.Return)) return ParseReturnStatement();
        if (Check(TokenType.Func)) return ParseFunctionDeclaration();
        if (Check(TokenType.Semicolon))
        {
            Advance(); // Skip empty statements
            return null;
        }

        // Expression statement
        var start = _current.Span.Start;
        var expr = ParseExpression();
        ConsumeSemicolon();
        return new ExpressionStatement(expr, new SourceSpan(start, _previous.Span.End));
    }

    private Statement ParseLetStatement()
    {
        var start = _current.Span.Start;
        Advance(); // %let

        var name = Consume(TokenType.Identifier, "Expected variable name after %let");
        Consume(TokenType.Assign, "Expected '=' after variable name");
        var value = ParseExpression();
        ConsumeSemicolon();

        return new LetStatement((string)name.Value!, value, new SourceSpan(start, _previous.Span.End));
    }

    private Statement ParseSetStatement()
    {
        var start = _current.Span.Start;
        Advance(); // %set

        // First parse what looks like a target path
        var firstExpr = ParseExpression();

        if (Match(TokenType.Comma))
        {
            // Form B: %set target, "path", value;
            var path = ParseExpression();
            Consume(TokenType.Comma, "Expected ',' after path");
            var value = ParseExpression();
            ConsumeSemicolon();
            return new SetStatement(firstExpr, path, value, new SourceSpan(start, _previous.Span.End));
        }
        else if (Match(TokenType.Assign))
        {
            // Form A: %set $.path = value;
            var value = ParseExpression();
            ConsumeSemicolon();
            return new SetStatement(null, firstExpr, value, new SourceSpan(start, _previous.Span.End));
        }
        else
        {
            throw Error("Expected '=' or ',' in %set statement");
        }
    }

    private Statement ParseIfStatement()
    {
        var start = _current.Span.Start;
        Advance(); // %if

        Consume(TokenType.LeftParen, "Expected '(' after %if");
        var condition = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after condition");
        Consume(TokenType.Then, "Expected %then after condition");
        Consume(TokenType.Do, "Expected %do after %then");
        ConsumeSemicolon();

        var thenBlock = ParseStatementBlock();

        List<Statement>? elseBlock = null;
        if (Match(TokenType.Else))
        {
            Consume(TokenType.Do, "Expected %do after %else");
            ConsumeSemicolon();
            elseBlock = ParseStatementBlock();
        }

        return new IfStatement(condition, thenBlock, elseBlock, new SourceSpan(start, _previous.Span.End));
    }

    private Statement ParseForeachStatement()
    {
        var start = _current.Span.Start;
        Advance(); // %foreach

        var iterName = Consume(TokenType.Identifier, "Expected iterator variable name");
        Consume(TokenType.In, "Expected %in after iterator name");
        var collection = ParseExpression();
        Consume(TokenType.Do, "Expected %do after collection");
        ConsumeSemicolon();

        var body = ParseStatementBlock();

        return new ForeachStatement((string)iterName.Value!, collection, body, new SourceSpan(start, _previous.Span.End));
    }

    private Statement ParseDoLoopStatement()
    {
        var start = _current.Span.Start;
        Advance(); // %do

        var iterName = Consume(TokenType.Identifier, "Expected iterator variable name");
        Consume(TokenType.Assign, "Expected '=' after iterator name");
        var startExpr = ParseExpression();
        Consume(TokenType.To, "Expected %to in do loop");
        var endExpr = ParseExpression();
        ConsumeSemicolon();

        var body = ParseStatementBlock();

        return new DoLoopStatement((string)iterName.Value!, startExpr, endExpr, body, new SourceSpan(start, _previous.Span.End));
    }

    private Statement ParseBreakStatement()
    {
        var start = _current.Span.Start;
        Advance(); // %break
        ConsumeSemicolon();
        return new BreakStatement(new SourceSpan(start, _previous.Span.End));
    }

    private Statement ParseContinueStatement()
    {
        var start = _current.Span.Start;
        Advance(); // %continue
        ConsumeSemicolon();
        return new ContinueStatement(new SourceSpan(start, _previous.Span.End));
    }

    private Statement ParseReturnStatement()
    {
        var start = _current.Span.Start;
        Advance(); // %return

        Expression? value = null;
        if (!Check(TokenType.Semicolon))
        {
            value = ParseExpression();
        }
        ConsumeSemicolon();

        return new ReturnStatement(value, new SourceSpan(start, _previous.Span.End));
    }

    private Statement ParseFunctionDeclaration()
    {
        var start = _current.Span.Start;
        Advance(); // %func

        var name = Consume(TokenType.Identifier, "Expected function name");
        Consume(TokenType.LeftParen, "Expected '(' after function name");

        var parameters = new List<string>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                var param = Consume(TokenType.Identifier, "Expected parameter name");
                parameters.Add((string)param.Value!);
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expected ')' after parameters");
        ConsumeSemicolon();

        var body = new List<Statement>();
        while (!Check(TokenType.EndFunc) && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt is not null)
                body.Add(stmt);
        }

        Consume(TokenType.EndFunc, "Expected %endfunc");
        ConsumeSemicolon();

        return new FunctionDeclaration((string)name.Value!, parameters, body, new SourceSpan(start, _previous.Span.End));
    }

    private List<Statement> ParseStatementBlock()
    {
        var statements = new List<Statement>();
        while (!Check(TokenType.End) && !Check(TokenType.Else) && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt is not null)
                statements.Add(stmt);
        }

        if (Check(TokenType.End))
        {
            Advance();
            ConsumeSemicolon();
        }

        return statements;
    }

    // Expression parsing with precedence climbing
    private Expression ParseExpression() => ParseOr();

    private Expression ParseOr()
    {
        var expr = ParseAnd();
        while (Match(TokenType.Or))
        {
            var op = _previous.Lexeme;
            var right = ParseAnd();
            expr = new BinaryExpression(expr, op, right, new SourceSpan(expr.Span.Start, right.Span.End));
        }
        return expr;
    }

    private Expression ParseAnd()
    {
        var expr = ParseEquality();
        while (Match(TokenType.And))
        {
            var op = _previous.Lexeme;
            var right = ParseEquality();
            expr = new BinaryExpression(expr, op, right, new SourceSpan(expr.Span.Start, right.Span.End));
        }
        return expr;
    }

    private Expression ParseEquality()
    {
        var expr = ParseComparison();
        while (Match(TokenType.Equal) || Match(TokenType.NotEqual))
        {
            var op = _previous.Lexeme;
            var right = ParseComparison();
            expr = new BinaryExpression(expr, op, right, new SourceSpan(expr.Span.Start, right.Span.End));
        }
        return expr;
    }

    private Expression ParseComparison()
    {
        var expr = ParseAdditive();
        while (Match(TokenType.LessThan) || Match(TokenType.LessOrEqual) ||
               Match(TokenType.GreaterThan) || Match(TokenType.GreaterOrEqual))
        {
            var op = _previous.Lexeme;
            var right = ParseAdditive();
            expr = new BinaryExpression(expr, op, right, new SourceSpan(expr.Span.Start, right.Span.End));
        }
        return expr;
    }

    private Expression ParseAdditive()
    {
        var expr = ParseMultiplicative();
        while (Match(TokenType.Plus) || Match(TokenType.Minus))
        {
            var op = _previous.Lexeme;
            var right = ParseMultiplicative();
            expr = new BinaryExpression(expr, op, right, new SourceSpan(expr.Span.Start, right.Span.End));
        }
        return expr;
    }

    private Expression ParseMultiplicative()
    {
        var expr = ParseUnary();
        while (Match(TokenType.Multiply) || Match(TokenType.Divide) || Match(TokenType.Modulo))
        {
            var op = _previous.Lexeme;
            var right = ParseUnary();
            expr = new BinaryExpression(expr, op, right, new SourceSpan(expr.Span.Start, right.Span.End));
        }
        return expr;
    }

    private Expression ParseUnary()
    {
        if (Match(TokenType.Not) || Match(TokenType.Minus))
        {
            var op = _previous.Lexeme;
            var start = _previous.Span.Start;
            var operand = ParseUnary();
            return new UnaryExpression(op, operand, new SourceSpan(start, operand.Span.End));
        }
        return ParsePostfix();
    }

    private Expression ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Match(TokenType.LeftParen))
            {
                // Function call
                if (expr is VariableRef vr)
                {
                    var args = ParseArguments();
                    Consume(TokenType.RightParen, "Expected ')' after arguments");
                    expr = new FunctionCall(vr.Name, args, new SourceSpan(expr.Span.Start, _previous.Span.End));
                }
                else if (expr is BuiltInVariable || expr is PropertyAccess)
                {
                    // Could be something like $in.method() - treat as error for now
                    throw Error("Cannot call method on this expression");
                }
                else
                {
                    throw Error("Can only call functions");
                }
            }
            else if (Match(TokenType.Dot))
            {
                var name = Consume(TokenType.Identifier, "Expected property name after '.'");
                expr = new PropertyAccess(expr, (string)name.Value!, new SourceSpan(expr.Span.Start, _previous.Span.End));
            }
            else if (Match(TokenType.LeftBracket))
            {
                var index = ParseExpression();
                Consume(TokenType.RightBracket, "Expected ']' after index");
                expr = new IndexAccess(expr, index, new SourceSpan(expr.Span.Start, _previous.Span.End));
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private Expression ParsePrimary()
    {
        var start = _current.Span.Start;

        if (Match(TokenType.Null))
            return new NullLiteral(_previous.Span);

        if (Match(TokenType.True))
            return new BooleanLiteral(true, _previous.Span);

        if (Match(TokenType.False))
            return new BooleanLiteral(false, _previous.Span);

        if (Match(TokenType.Integer))
            return new NumberLiteral((decimal)(long)_previous.Value!, _previous.Span);

        if (Match(TokenType.Decimal))
            return new NumberLiteral((decimal)_previous.Value!, _previous.Span);

        if (Match(TokenType.String))
            return new StringLiteral((string)_previous.Value!, _previous.Span);

        if (Match(TokenType.VariableRef))
            return new VariableRef((string)_previous.Value!, _previous.Span);

        if (Match(TokenType.Dollar))
        {
            // Check if this is a JSON path like $.foo or just a built-in variable like $in
            if (Check(TokenType.Dot))
            {
                // This is a JSON path like $.foo.bar
                return ParseJsonPath(start);
            }
            // Built-in variable: $in, $out, $meta
            var name = Consume(TokenType.Identifier, "Expected variable name or '.' after '$'");
            var fullName = "$" + (string)name.Value!;
            return new BuiltInVariable(fullName, new SourceSpan(start, _previous.Span.End));
        }

        if (Match(TokenType.Identifier))
        {
            var name = (string)_previous.Value!;
            // Could be a function call without parens - check if followed by (
            if (Check(TokenType.LeftParen))
            {
                Advance();
                var args = ParseArguments();
                Consume(TokenType.RightParen, "Expected ')' after arguments");
                return new FunctionCall(name, args, new SourceSpan(start, _previous.Span.End));
            }
            return new VariableRef(name, _previous.Span);
        }

        if (Match(TokenType.LeftParen))
        {
            var expr = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after expression");
            return expr;
        }

        if (Match(TokenType.LeftBrace))
        {
            return ParseJsonObject(start);
        }

        if (Match(TokenType.LeftBracket))
        {
            return ParseJsonArray(start);
        }

        throw Error($"Unexpected token '{_current.Lexeme}'");
    }

    private List<Expression> ParseArguments()
    {
        var args = new List<Expression>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                args.Add(ParseExpression());
            } while (Match(TokenType.Comma));
        }
        return args;
    }

    private Expression ParseJsonObject(SourcePosition start)
    {
        var properties = new List<(Expression Key, Expression Value)>();

        if (!Check(TokenType.RightBrace))
        {
            do
            {
                Expression key;
                if (Match(TokenType.String))
                {
                    key = new StringLiteral((string)_previous.Value!, _previous.Span);
                }
                else if (Match(TokenType.Identifier))
                {
                    key = new StringLiteral((string)_previous.Value!, _previous.Span);
                }
                else
                {
                    throw Error("Expected property name in object literal");
                }

                Consume(TokenType.Colon, "Expected ':' after property name");
                var value = ParseExpression();
                properties.Add((key, value));
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightBrace, "Expected '}' after object literal");
        return new JsonObjectLiteral(properties, new SourceSpan(start, _previous.Span.End));
    }

    private Expression ParseJsonArray(SourcePosition start)
    {
        var elements = new List<Expression>();

        if (!Check(TokenType.RightBracket))
        {
            do
            {
                elements.Add(ParseExpression());
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightBracket, "Expected ']' after array literal");
        return new JsonArrayLiteral(elements, new SourceSpan(start, _previous.Span.End));
    }

    private Expression ParseJsonPath(SourcePosition start)
    {
        // We've already consumed '$' and checked for '.', now build the path string
        var pathBuilder = new System.Text.StringBuilder("$");
        
        while (!IsAtEnd())
        {
            if (Check(TokenType.Dot))
            {
                Advance();
                pathBuilder.Append('.');
                
                // After dot, expect identifier or continue
                if (Check(TokenType.Identifier))
                {
                    Advance();
                    pathBuilder.Append((string)_previous.Value!);
                }
                else if (Check(TokenType.Integer))
                {
                    // Handle numeric property names
                    Advance();
                    pathBuilder.Append(_previous.Value);
                }
                else
                {
                    break;
                }
            }
            else if (Check(TokenType.LeftBracket))
            {
                Advance();
                pathBuilder.Append('[');
                
                if (Check(TokenType.Integer))
                {
                    Advance();
                    pathBuilder.Append(_previous.Value);
                }
                else if (Check(TokenType.String))
                {
                    Advance();
                    pathBuilder.Append('\'');
                    pathBuilder.Append((string)_previous.Value!);
                    pathBuilder.Append('\'');
                }
                else if (Check(TokenType.Multiply)) // * for wildcard
                {
                    Advance();
                    pathBuilder.Append('*');
                }
                
                Consume(TokenType.RightBracket, "Expected ']' in JSON path");
                pathBuilder.Append(']');
            }
            else
            {
                break;
            }
        }
        
        return new JsonPathExpression(pathBuilder.ToString(), new SourceSpan(start, _previous.Span.End));
    }

    // Helper methods
    private bool IsAtEnd() => _current.Type == TokenType.EOF;

    private bool Check(TokenType type) => !IsAtEnd() && _current.Type == type;

    private bool Match(TokenType type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }
        return false;
    }

    private Token Advance()
    {
        _previous = _current;
        if (!IsAtEnd())
            _current = _lexer.NextToken();
        return _previous;
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type))
            return Advance();
        throw Error(message);
    }

    private void ConsumeSemicolon()
    {
        if (!Match(TokenType.Semicolon))
        {
            // Allow missing semicolons at certain positions (lenient parsing)
            // For now, we'll be strict
            throw Error("Expected ';'");
        }
    }

    private JexCompileException Error(string message)
    {
        return new JexCompileException(message, _current.Span);
    }
}
