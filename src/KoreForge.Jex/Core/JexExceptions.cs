using System;

namespace KoreForge.Jex;

/// <summary>
/// Represents a position in source code.
/// </summary>
public readonly record struct SourcePosition(int Line, int Column, int Offset)
{
    public static SourcePosition Unknown => new(0, 0, 0);
    public override string ToString() => $"({Line},{Column})";
}

/// <summary>
/// Represents a span in source code.
/// </summary>
public readonly record struct SourceSpan(SourcePosition Start, SourcePosition End)
{
    public static SourceSpan Unknown => new(SourcePosition.Unknown, SourcePosition.Unknown);
    public override string ToString() => $"{Start}-{End}";
}

/// <summary>
/// Base class for all JEX exceptions.
/// </summary>
public abstract class JexException : Exception
{
    public SourceSpan? Span { get; }

    protected JexException(string message, SourceSpan? span = null, Exception? inner = null)
        : base(FormatMessage(message, span), inner)
    {
        Span = span;
    }

    private static string FormatMessage(string message, SourceSpan? span)
    {
        return span.HasValue ? $"{message} at {span.Value}" : message;
    }
}

/// <summary>
/// Exception thrown during compilation (lexing, parsing, semantic analysis).
/// </summary>
public class JexCompileException : JexException
{
    public JexCompileException(string message, SourceSpan? span = null, Exception? inner = null)
        : base(message, span, inner) { }
}

/// <summary>
/// Exception thrown during runtime execution.
/// </summary>
public class JexRuntimeException : JexException
{
    public string? FunctionName { get; }
    public string? Path { get; }

    public JexRuntimeException(string message, SourceSpan? span = null, string? functionName = null, 
        string? path = null, Exception? inner = null)
        : base(FormatRuntimeMessage(message, functionName, path), span, inner)
    {
        FunctionName = functionName;
        Path = path;
    }

    private static string FormatRuntimeMessage(string message, string? functionName, string? path)
    {
        var parts = new System.Collections.Generic.List<string> { message };
        if (functionName is not null) parts.Add($"in function '{functionName}'");
        if (path is not null) parts.Add($"at path '{path}'");
        return string.Join(" ", parts);
    }
}

/// <summary>
/// Exception thrown when execution limits are exceeded.
/// </summary>
public class JexLimitExceededException : JexRuntimeException
{
    public string LimitName { get; }
    public int LimitValue { get; }

    public JexLimitExceededException(string limitName, int limitValue, SourceSpan? span = null)
        : base($"Limit exceeded: {limitName} (max: {limitValue})", span)
    {
        LimitName = limitName;
        LimitValue = limitValue;
    }
}
