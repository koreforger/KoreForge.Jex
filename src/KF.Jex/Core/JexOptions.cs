namespace KoreForge.Jex;

/// <summary>
/// Configuration options for JEX compilation.
/// </summary>
public sealed class JexCompileOptions
{
    /// <summary>
    /// If true, enables strict mode during compilation.
    /// </summary>
    public bool Strict { get; set; } = false;

    /// <summary>
    /// If true, allows user-defined functions.
    /// </summary>
    public bool AllowUserFunctions { get; set; } = true;

    public static JexCompileOptions Default => new();
}

/// <summary>
/// Configuration options for JEX execution.
/// </summary>
public sealed class JexExecutionOptions
{
    /// <summary>
    /// If true, missing paths and variables cause errors. Otherwise returns null.
    /// </summary>
    public bool Strict { get; set; } = false;

    /// <summary>
    /// Maximum number of iterations allowed in loops.
    /// </summary>
    public int MaxLoopIterations { get; set; } = 100_000;

    /// <summary>
    /// Maximum recursion depth for function calls.
    /// </summary>
    public int MaxRecursionDepth { get; set; } = 100;

    /// <summary>
    /// Timeout in milliseconds for regex operations.
    /// </summary>
    public int RegexTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Maximum output size in bytes (0 = unlimited).
    /// </summary>
    public int MaxOutputSizeBytes { get; set; } = 0;

    public static JexExecutionOptions Default => new();
}

/// <summary>
/// Options for JSON string normalization preprocessing.
/// </summary>
public sealed record JsonStringNormalizationOptions(
    int MaxDepthPerString = 5,
    int MaxNodesVisited = 250_000,
    int MaxTotalReplacements = 50_000,
    int MaxStringLength = 256_000,
    bool Strict = false
)
{
    public static JsonStringNormalizationOptions Default => new();
}
