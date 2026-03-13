using System;
using System.Collections.Generic;

namespace KoreForge.Jex.Runtime;

/// <summary>
/// Interface for JEX functions (both built-in and host-registered).
/// </summary>
internal interface IInternalJexFunction
{
    string Name { get; }
    int MinArgs { get; }
    int MaxArgs { get; }
    JexValue Invoke(JexRuntimeContext context, IReadOnlyList<JexValue> args);
}

/// <summary>
/// Registry for JEX functions.
/// </summary>
internal sealed class JexFunctionRegistry
{
    private readonly Dictionary<string, IInternalJexFunction> _functions = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IInternalJexFunction function)
    {
        _functions[function.Name] = function;
    }

    public void Register(string name, Func<JexRuntimeContext, IReadOnlyList<JexValue>, JexValue> func, 
        int minArgs = 0, int maxArgs = int.MaxValue)
    {
        _functions[name] = new DelegateFunction(name, func, minArgs, maxArgs);
    }

    public bool HasFunction(string name) => _functions.ContainsKey(name);

    public JexValue Invoke(string name, JexRuntimeContext context, IReadOnlyList<JexValue> args, SourceSpan? span = null)
    {
        if (!_functions.TryGetValue(name, out var func))
        {
            throw new JexRuntimeException($"Unknown function '{name}'", span, functionName: name);
        }

        if (args.Count < func.MinArgs)
        {
            throw new JexRuntimeException(
                $"Function '{name}' requires at least {func.MinArgs} argument(s), but got {args.Count}",
                span, functionName: name);
        }

        if (args.Count > func.MaxArgs)
        {
            throw new JexRuntimeException(
                $"Function '{name}' accepts at most {func.MaxArgs} argument(s), but got {args.Count}",
                span, functionName: name);
        }

        try
        {
            return func.Invoke(context, args);
        }
        catch (JexException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new JexRuntimeException($"Error executing function '{name}': {ex.Message}", span, 
                functionName: name, inner: ex);
        }
    }

    private sealed class DelegateFunction : IInternalJexFunction
    {
        private readonly Func<JexRuntimeContext, IReadOnlyList<JexValue>, JexValue> _func;

        public DelegateFunction(string name, Func<JexRuntimeContext, IReadOnlyList<JexValue>, JexValue> func, 
            int minArgs, int maxArgs)
        {
            Name = name;
            _func = func;
            MinArgs = minArgs;
            MaxArgs = maxArgs;
        }

        public string Name { get; }
        public int MinArgs { get; }
        public int MaxArgs { get; }

        public JexValue Invoke(JexRuntimeContext context, IReadOnlyList<JexValue> args)
        {
            return _func(context, args);
        }
    }
}
