using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KoreForge.Jex.Runtime;

/// <summary>
/// Internal execution context for a JEX program.
/// Contains all state needed during execution.
/// </summary>
internal sealed class JexRuntimeContext
{
    private readonly Dictionary<string, JexValue> _variables = new();
    private readonly Stack<Dictionary<string, JexValue>> _scopes = new();
    private readonly JexExecutionOptions _options;

    public JToken Input { get; }
    public JToken Output { get; set; }
    public JToken? Meta { get; }

    public int LoopIterations { get; set; }
    public int RecursionDepth { get; set; }

    public bool ShouldBreak { get; set; }
    public bool ShouldContinue { get; set; }
    public bool ShouldReturn { get; set; }
    public JexValue ReturnValue { get; set; }

    public JexRuntimeContext(JToken input, JToken? meta, JexExecutionOptions options)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Output = new JObject();
        Meta = meta;
        _options = options;
    }

    public JexExecutionOptions Options => _options;

    public void SetVariable(string name, JexValue value)
    {
        // First check if variable exists in any scope - if so, update it there
        foreach (var scope in _scopes)
        {
            if (scope.ContainsKey(name))
            {
                scope[name] = value;
                return;
            }
        }

        // Check global scope
        if (_variables.ContainsKey(name))
        {
            _variables[name] = value;
            return;
        }

        // Variable doesn't exist - create in current scope (or global if no scopes)
        if (_scopes.Count > 0)
        {
            _scopes.Peek()[name] = value;
        }
        else
        {
            _variables[name] = value;
        }
    }

    public JexValue GetVariable(string name)
    {
        // Search from innermost scope outward
        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(name, out var value))
                return value;
        }

        if (_variables.TryGetValue(name, out var globalValue))
            return globalValue;

        if (_options.Strict)
            throw new JexRuntimeException($"Undefined variable '&{name}'");

        return JexValue.Null;
    }

    public bool HasVariable(string name)
    {
        foreach (var scope in _scopes)
        {
            if (scope.ContainsKey(name))
                return true;
        }
        return _variables.ContainsKey(name);
    }

    public void PushScope()
    {
        _scopes.Push(new Dictionary<string, JexValue>());
    }

    public void PopScope()
    {
        if (_scopes.Count > 0)
            _scopes.Pop();
    }

    public void CheckLoopLimit(SourceSpan? span = null)
    {
        LoopIterations++;
        if (LoopIterations > _options.MaxLoopIterations)
        {
            throw new JexLimitExceededException("MaxLoopIterations", _options.MaxLoopIterations, span);
        }
    }

    public void CheckRecursionLimit(SourceSpan? span = null)
    {
        if (RecursionDepth > _options.MaxRecursionDepth)
        {
            throw new JexLimitExceededException("MaxRecursionDepth", _options.MaxRecursionDepth, span);
        }
    }

    public void ResetLoopControl()
    {
        ShouldBreak = false;
        ShouldContinue = false;
    }
}
