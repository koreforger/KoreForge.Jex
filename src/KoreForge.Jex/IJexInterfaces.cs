using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KoreForge.Jex;

/// <summary>
/// Interface for compiling JEX scripts.
/// </summary>
public interface IJexCompiler
{
    /// <summary>
    /// Compiles a JEX script into an executable program.
    /// </summary>
    IJexProgram Compile(string script, JexCompileOptions? options = null);
}

/// <summary>
/// Interface for a compiled JEX program.
/// </summary>
public interface IJexProgram
{
    /// <summary>
    /// Executes the program with the given input JSON.
    /// </summary>
    JToken Execute(JToken input, JexExecutionOptions? options = null);

    /// <summary>
    /// Executes the program with the given input JSON and metadata.
    /// </summary>
    JToken Execute(JToken input, JToken? meta, JexExecutionOptions? options = null);
}

/// <summary>
/// Interface for registering custom functions.
/// </summary>
public interface IJexFunctionRegistry
{
    /// <summary>
    /// Registers a custom function.
    /// </summary>
    void Register(string name, IJexFunction function);
}

/// <summary>
/// Interface for a JEX function.
/// </summary>
public interface IJexFunction
{
    /// <summary>
    /// Invokes the function with the given arguments.
    /// </summary>
    JexValue Invoke(JexExecutionContext context, IReadOnlyList<JexValue> args);
}

/// <summary>
/// Execution context passed to functions.
/// </summary>
public class JexExecutionContext
{
    internal KoreForge.Jex.Runtime.JexRuntimeContext InternalContext { get; }

    internal JexExecutionContext(KoreForge.Jex.Runtime.JexRuntimeContext context)
    {
        InternalContext = context;
    }

    /// <summary>
    /// The input JSON document.
    /// </summary>
    public JToken Input => InternalContext.Input;

    /// <summary>
    /// The output JSON document being constructed.
    /// </summary>
    public JToken Output => InternalContext.Output;

    /// <summary>
    /// Optional metadata provided by the host.
    /// </summary>
    public JToken? Meta => InternalContext.Meta;

    /// <summary>
    /// Execution options.
    /// </summary>
    public JexExecutionOptions Options => InternalContext.Options;
}
