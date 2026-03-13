using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using KoreForge.Jex.Compiler;
using KoreForge.Jex.Library;
using KoreForge.Jex.Runtime;
using KoreForge.Jex.Preprocessing;

namespace KoreForge.Jex;

/// <summary>
/// Main entry point for the JEX engine.
/// Provides methods to compile and execute JEX scripts.
/// </summary>
public sealed class Jex : IJexCompiler
{
    private readonly JexFunctionRegistry _functionRegistry;
    private readonly JexLibraryManager _libraryManager;

    /// <summary>
    /// Creates a new JEX engine instance.
    /// </summary>
    public Jex()
    {
        _functionRegistry = new JexFunctionRegistry();
        _libraryManager = new JexLibraryManager();
        JexStandardLibrary.RegisterAll(_functionRegistry);
    }

    /// <summary>
    /// Gets the library manager for this engine instance.
    /// </summary>
    public JexLibraryManager Libraries => _libraryManager;

    /// <summary>
    /// Registers a custom function with the engine.
    /// </summary>
    public void RegisterFunction(string name, Func<JexExecutionContext, IReadOnlyList<JexValue>, JexValue> func,
        int minArgs = 0, int maxArgs = int.MaxValue)
    {
        _functionRegistry.Register(name, (ctx, args) =>
        {
            var publicCtx = new JexExecutionContext(ctx);
            return func(publicCtx, args);
        }, minArgs, maxArgs);
    }

    /// <summary>
    /// Registers a void function (operates on $in and $out) with the engine.
    /// </summary>
    public void RegisterVoidFunction(string name, Action<JexExecutionContext, IReadOnlyList<JexValue>> action,
        int minArgs = 0, int maxArgs = int.MaxValue)
    {
        _functionRegistry.Register(name, (ctx, args) =>
        {
            var publicCtx = new JexExecutionContext(ctx);
            action(publicCtx, args);
            return JexValue.Null;
        }, minArgs, maxArgs);
    }

    /// <summary>
    /// Loads a library from a file and registers it with this engine.
    /// </summary>
    public JexLibrary LoadLibrary(string filePath)
    {
        return _libraryManager.LoadFromFile(filePath);
    }

    /// <summary>
    /// Loads a library from a stream and registers it with this engine.
    /// </summary>
    public JexLibrary LoadLibrary(string name, Stream stream)
    {
        return _libraryManager.LoadFromStream(name, stream);
    }

    /// <summary>
    /// Loads a library from source code and registers it with this engine.
    /// </summary>
    public JexLibrary LoadLibraryFromSource(string name, string source)
    {
        return _libraryManager.LoadFromSource(name, source);
    }

    /// <summary>
    /// Compiles a JEX script into an executable program.
    /// </summary>
    public IJexProgram Compile(string script, JexCompileOptions? options = null)
    {
        var compiler = new JexCompiler(options);
        var compiled = compiler.Compile(script);
        return new JexProgram(compiled, _functionRegistry, _libraryManager);
    }

    /// <summary>
    /// Compiles and immediately executes a JEX script.
    /// For better performance with repeated executions, use Compile() and cache the result.
    /// </summary>
    public JToken Execute(string script, JToken input, JexExecutionOptions? options = null)
    {
        var program = Compile(script);
        return program.Execute(input, options);
    }

    /// <summary>
    /// Normalizes JSON-in-string fields in the input.
    /// </summary>
    public static JToken NormalizeJsonStrings(JToken input, JsonStringNormalizationOptions? options = null)
    {
        return JsonStringNormalizer.NormalizeJsonStrings(input, options);
    }
}

/// <summary>
/// A compiled JEX program that can be executed multiple times.
/// </summary>
internal sealed class JexProgram : IJexProgram
{
    private readonly CompiledProgram _compiled;
    private readonly JexFunctionRegistry _functionRegistry;
    private readonly JexLibraryManager _libraryManager;

    public JexProgram(CompiledProgram compiled, JexFunctionRegistry functionRegistry, JexLibraryManager libraryManager)
    {
        _compiled = compiled;
        _functionRegistry = functionRegistry;
        _libraryManager = libraryManager;
    }

    public JToken Execute(JToken input, JexExecutionOptions? options = null)
    {
        return Execute(input, null, options);
    }

    public JToken Execute(JToken input, JToken? meta, JexExecutionOptions? options = null)
    {
        options ??= JexExecutionOptions.Default;
        var interpreter = new JexInterpreter(_compiled, _functionRegistry, _libraryManager);
        return interpreter.Execute(input, meta, options);
    }
}
