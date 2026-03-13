namespace KoreForge.Jex.Parser;

/// <summary>
/// Base class for all AST nodes.
/// </summary>
public abstract record AstNode(SourceSpan Span);

// ============ Expressions ============

public abstract record Expression(SourceSpan Span) : AstNode(Span);

public sealed record NullLiteral(SourceSpan Span) : Expression(Span);
public sealed record BooleanLiteral(bool Value, SourceSpan Span) : Expression(Span);
public sealed record NumberLiteral(decimal Value, SourceSpan Span) : Expression(Span);
public sealed record StringLiteral(string Value, SourceSpan Span) : Expression(Span);

public sealed record VariableRef(string Name, SourceSpan Span) : Expression(Span);

public sealed record UnaryExpression(string Operator, Expression Operand, SourceSpan Span) : Expression(Span);
public sealed record BinaryExpression(Expression Left, string Operator, Expression Right, SourceSpan Span) : Expression(Span);

public sealed record FunctionCall(string Name, List<Expression> Arguments, SourceSpan Span) : Expression(Span);

public sealed record JsonObjectLiteral(List<(Expression Key, Expression Value)> Properties, SourceSpan Span) : Expression(Span);
public sealed record JsonArrayLiteral(List<Expression> Elements, SourceSpan Span) : Expression(Span);

public sealed record BuiltInVariable(string Name, SourceSpan Span) : Expression(Span); // $in, $out, $meta

public sealed record JsonPathExpression(string Path, SourceSpan Span) : Expression(Span); // $.foo.bar

public sealed record PropertyAccess(Expression Target, string Property, SourceSpan Span) : Expression(Span);
public sealed record IndexAccess(Expression Target, Expression Index, SourceSpan Span) : Expression(Span);

// ============ Statements ============

public abstract record Statement(SourceSpan Span) : AstNode(Span);

public sealed record LetStatement(string VariableName, Expression Value, SourceSpan Span) : Statement(Span);

/// <summary>
/// %set $.path = expr; or %set target, "path", expr;
/// </summary>
public sealed record SetStatement(Expression? Target, Expression Path, Expression Value, SourceSpan Span) : Statement(Span);

public sealed record IfStatement(Expression Condition, List<Statement> ThenBlock, List<Statement>? ElseBlock, SourceSpan Span) : Statement(Span);

public sealed record ForeachStatement(string IteratorName, Expression Collection, List<Statement> Body, SourceSpan Span) : Statement(Span);

public sealed record DoLoopStatement(string IteratorName, Expression Start, Expression End, List<Statement> Body, SourceSpan Span) : Statement(Span);

public sealed record BreakStatement(SourceSpan Span) : Statement(Span);
public sealed record ContinueStatement(SourceSpan Span) : Statement(Span);
public sealed record ReturnStatement(Expression? Value, SourceSpan Span) : Statement(Span);

public sealed record ExpressionStatement(Expression Expr, SourceSpan Span) : Statement(Span);

public sealed record FunctionDeclaration(string Name, List<string> Parameters, List<Statement> Body, SourceSpan Span) : Statement(Span);

public sealed record BlockStatement(List<Statement> Statements, SourceSpan Span) : Statement(Span);

// ============ Program ============

public sealed record Program(List<Statement> Statements, SourceSpan Span) : AstNode(Span);
