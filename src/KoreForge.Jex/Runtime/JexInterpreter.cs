using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using KoreForge.Jex.Library;
using KoreForge.Jex.Parser;
using KoreForge.Jex.Compiler;

namespace KoreForge.Jex.Runtime;

/// <summary>
/// Interprets and executes a compiled JEX program.
/// Uses a tree-walking interpreter approach for simplicity.
/// </summary>
internal sealed class JexInterpreter
{
    private readonly CompiledProgram _program;
    private readonly JexFunctionRegistry _functionRegistry;
    private readonly JexLibraryManager? _libraryManager;
    private JexRuntimeContext _context = null!;

    public JexInterpreter(CompiledProgram program, JexFunctionRegistry functionRegistry)
        : this(program, functionRegistry, null)
    {
    }

    public JexInterpreter(CompiledProgram program, JexFunctionRegistry functionRegistry, JexLibraryManager? libraryManager)
    {
        _program = program;
        _functionRegistry = functionRegistry;
        _libraryManager = libraryManager;
    }

    public JToken Execute(JToken input, JToken? meta, JexExecutionOptions options)
    {
        _context = new JexRuntimeContext(input, meta, options);

        foreach (var statement in _program.Ast.Statements)
        {
            if (statement is FunctionDeclaration)
                continue; // Skip function declarations during execution

            ExecuteStatement(statement);

            if (_context.ShouldReturn)
                break;
        }

        return _context.Output;
    }

    private void ExecuteStatement(Statement stmt)
    {
        if (_context.ShouldReturn || _context.ShouldBreak || _context.ShouldContinue)
            return;

        switch (stmt)
        {
            case LetStatement let:
                ExecuteLet(let);
                break;
            case SetStatement set:
                ExecuteSet(set);
                break;
            case IfStatement ifStmt:
                ExecuteIf(ifStmt);
                break;
            case ForeachStatement foreachStmt:
                ExecuteForeach(foreachStmt);
                break;
            case DoLoopStatement doLoop:
                ExecuteDoLoop(doLoop);
                break;
            case BreakStatement:
                _context.ShouldBreak = true;
                break;
            case ContinueStatement:
                _context.ShouldContinue = true;
                break;
            case ReturnStatement ret:
                ExecuteReturn(ret);
                break;
            case ExpressionStatement exprStmt:
                Evaluate(exprStmt.Expr);
                break;
            case BlockStatement block:
                foreach (var s in block.Statements)
                {
                    ExecuteStatement(s);
                    if (_context.ShouldReturn || _context.ShouldBreak || _context.ShouldContinue)
                        break;
                }
                break;
            default:
                throw new JexRuntimeException($"Unknown statement type: {stmt.GetType().Name}", stmt.Span);
        }
    }

    private void ExecuteLet(LetStatement let)
    {
        var value = Evaluate(let.Value);
        _context.SetVariable(let.VariableName, value);
    }

    private void ExecuteSet(SetStatement set)
    {
        JToken target;
        string path;

        if (set.Target is null)
        {
            // Form A: %set $.path = value; - target is $out, path is the expression
            target = _context.Output;
            path = GetPathFromExpression(set.Path);
        }
        else
        {
            // Form B: %set target, "path", value;
            var targetValue = Evaluate(set.Target);
            target = targetValue.AsJson() ?? throw new JexRuntimeException("Set target must be a JSON value", set.Span);
            path = Evaluate(set.Path).AsString();
        }

        var value = Evaluate(set.Value);
        SetJsonPath(target, path, value, set.Span);
    }

    private string GetPathFromExpression(Expression expr)
    {
        // Handle expressions like $.user.id or $out.items[0]
        // Convert to a path string
        return BuildPathString(expr);
    }

    private string BuildPathString(Expression expr)
    {
        return expr switch
        {
            BuiltInVariable bv => bv.Name,
            JsonPathExpression jp => jp.Path,
            PropertyAccess pa => $"{BuildPathString(pa.Target)}.{pa.Property}",
            IndexAccess ia => $"{BuildPathString(ia.Target)}[{Evaluate(ia.Index).AsNumber()}]",
            StringLiteral sl => sl.Value,
            VariableRef vr => "&" + vr.Name,
            _ => throw new JexRuntimeException($"Cannot convert expression to path", expr.Span)
        };
    }

    private void SetJsonPath(JToken target, string path, JexValue value, SourceSpan span)
    {
        // Simplified path setting - handles basic paths like $.user.id or $.items[0]
        var pathParts = ParsePath(path);
        var current = target;

        for (int i = 0; i < pathParts.Count - 1; i++)
        {
            var part = pathParts[i];
            if (part.IsIndex)
            {
                if (current is JArray arr)
                {
                    while (arr.Count <= part.Index)
                        arr.Add(JValue.CreateNull());
                    current = arr[part.Index];
                }
                else
                {
                    throw new JexRuntimeException($"Cannot index non-array at path '{path}'", span);
                }
            }
            else
            {
                if (current is JObject obj)
                {
                    if (obj[part.Name] is null)
                    {
                        // Check if next part is index to decide object or array
                        if (i + 1 < pathParts.Count && pathParts[i + 1].IsIndex)
                            obj[part.Name] = new JArray();
                        else
                            obj[part.Name] = new JObject();
                    }
                    current = obj[part.Name]!;
                }
                else
                {
                    throw new JexRuntimeException($"Cannot access property on non-object at path '{path}'", span);
                }
            }
        }

        // Set the final value
        var lastPart = pathParts[^1];
        var jsonValue = value.AsJson();

        if (lastPart.IsIndex)
        {
            if (current is JArray arr)
            {
                while (arr.Count <= lastPart.Index)
                    arr.Add(JValue.CreateNull());
                arr[lastPart.Index] = jsonValue;
            }
            else
            {
                throw new JexRuntimeException($"Cannot index non-array at path '{path}'", span);
            }
        }
        else
        {
            if (current is JObject obj)
            {
                obj[lastPart.Name] = jsonValue;
            }
            else
            {
                throw new JexRuntimeException($"Cannot set property on non-object at path '{path}'", span);
            }
        }
    }

    private record PathPart(string Name, int Index, bool IsIndex);

    private List<PathPart> ParsePath(string path)
    {
        var parts = new List<PathPart>();
        var trimmed = path.TrimStart('$');
        if (trimmed.StartsWith("out") || trimmed.StartsWith("in") || trimmed.StartsWith("meta"))
        {
            // Skip the built-in variable name part
            var dotIdx = trimmed.IndexOf('.');
            if (dotIdx >= 0)
                trimmed = trimmed[(dotIdx + 1)..];
            else
                trimmed = "";
        }

        if (trimmed.StartsWith('.'))
            trimmed = trimmed[1..];

        if (string.IsNullOrEmpty(trimmed))
            return parts;

        int i = 0;
        while (i < trimmed.Length)
        {
            if (trimmed[i] == '[')
            {
                int end = trimmed.IndexOf(']', i);
                if (end < 0) break;
                var indexStr = trimmed[(i + 1)..end];
                if (int.TryParse(indexStr, out int idx))
                {
                    parts.Add(new PathPart("", idx, true));
                }
                i = end + 1;
                if (i < trimmed.Length && trimmed[i] == '.')
                    i++;
            }
            else
            {
                int end = i;
                while (end < trimmed.Length && trimmed[end] != '.' && trimmed[end] != '[')
                    end++;
                var name = trimmed[i..end];
                if (!string.IsNullOrEmpty(name))
                    parts.Add(new PathPart(name, 0, false));
                i = end;
                if (i < trimmed.Length && trimmed[i] == '.')
                    i++;
            }
        }

        return parts;
    }

    private void ExecuteIf(IfStatement ifStmt)
    {
        var condition = Evaluate(ifStmt.Condition);
        if (condition.AsBoolean())
        {
            foreach (var s in ifStmt.ThenBlock)
            {
                ExecuteStatement(s);
                if (_context.ShouldReturn || _context.ShouldBreak || _context.ShouldContinue)
                    break;
            }
        }
        else if (ifStmt.ElseBlock is not null)
        {
            foreach (var s in ifStmt.ElseBlock)
            {
                ExecuteStatement(s);
                if (_context.ShouldReturn || _context.ShouldBreak || _context.ShouldContinue)
                    break;
            }
        }
    }

    private void ExecuteForeach(ForeachStatement foreachStmt)
    {
        var collection = Evaluate(foreachStmt.Collection);
        var items = collection.AsJson();

        if (items is not JArray arr)
        {
            if (items is null || items.Type == JTokenType.Null)
                return;
            arr = new JArray(items);
        }

        _context.PushScope();
        try
        {
            foreach (var item in arr)
            {
                _context.CheckLoopLimit(foreachStmt.Span);
                _context.SetVariable(foreachStmt.IteratorName, JexValue.FromJson(item));

                foreach (var s in foreachStmt.Body)
                {
                    ExecuteStatement(s);
                    if (_context.ShouldReturn || _context.ShouldBreak)
                        break;
                    if (_context.ShouldContinue)
                    {
                        _context.ShouldContinue = false;
                        break;
                    }
                }

                if (_context.ShouldReturn || _context.ShouldBreak)
                {
                    _context.ShouldBreak = false;
                    break;
                }
            }
        }
        finally
        {
            _context.PopScope();
        }
    }

    private void ExecuteDoLoop(DoLoopStatement doLoop)
    {
        var startVal = (int)Evaluate(doLoop.Start).AsNumber();
        var endVal = (int)Evaluate(doLoop.End).AsNumber();

        _context.PushScope();
        try
        {
            for (int i = startVal; i <= endVal; i++)
            {
                _context.CheckLoopLimit(doLoop.Span);
                _context.SetVariable(doLoop.IteratorName, JexValue.FromNumber(i));

                foreach (var s in doLoop.Body)
                {
                    ExecuteStatement(s);
                    if (_context.ShouldReturn || _context.ShouldBreak)
                        break;
                    if (_context.ShouldContinue)
                    {
                        _context.ShouldContinue = false;
                        break;
                    }
                }

                if (_context.ShouldReturn || _context.ShouldBreak)
                {
                    _context.ShouldBreak = false;
                    break;
                }
            }
        }
        finally
        {
            _context.PopScope();
        }
    }

    private void ExecuteReturn(ReturnStatement ret)
    {
        if (ret.Value is not null)
        {
            _context.ReturnValue = Evaluate(ret.Value);
        }
        _context.ShouldReturn = true;
    }

    public JexValue Evaluate(Expression expr)
    {
        return expr switch
        {
            NullLiteral => JexValue.Null,
            BooleanLiteral bl => JexValue.FromBoolean(bl.Value),
            NumberLiteral nl => JexValue.FromNumber(nl.Value),
            StringLiteral sl => ExpandMacros(sl.Value),
            VariableRef vr => _context.GetVariable(vr.Name),
            BuiltInVariable bv => EvaluateBuiltIn(bv),
            JsonPathExpression jp => JexValue.FromString(jp.Path),
            UnaryExpression ue => EvaluateUnary(ue),
            BinaryExpression be => EvaluateBinary(be),
            FunctionCall fc => EvaluateFunctionCall(fc),
            JsonObjectLiteral jol => EvaluateJsonObject(jol),
            JsonArrayLiteral jal => EvaluateJsonArray(jal),
            PropertyAccess pa => EvaluatePropertyAccess(pa),
            IndexAccess ia => EvaluateIndexAccess(ia),
            _ => throw new JexRuntimeException($"Unknown expression type: {expr.GetType().Name}", expr.Span)
        };
    }

    private JexValue ExpandMacros(string value)
    {
        // Expand &variable references in strings
        var result = value;
        int idx = 0;
        while ((idx = result.IndexOf('&', idx)) >= 0)
        {
            int end = idx + 1;
            while (end < result.Length && (char.IsLetterOrDigit(result[end]) || result[end] == '_'))
                end++;

            if (end > idx + 1)
            {
                var varName = result[(idx + 1)..end];
                var varValue = _context.GetVariable(varName);
                result = result[..idx] + varValue.AsString() + result[end..];
            }
            else
            {
                idx++;
            }
        }
        return JexValue.FromString(result);
    }

    private JexValue EvaluateBuiltIn(BuiltInVariable bv)
    {
        return bv.Name switch
        {
            "$in" => JexValue.FromJson(_context.Input),
            "$out" => JexValue.FromJson(_context.Output),
            "$meta" => _context.Meta is not null ? JexValue.FromJson(_context.Meta) : JexValue.Null,
            _ => throw new JexRuntimeException($"Unknown built-in variable '{bv.Name}'", bv.Span)
        };
    }

    private JexValue EvaluateUnary(UnaryExpression ue)
    {
        var operand = Evaluate(ue.Operand);
        return ue.Operator switch
        {
            "!" => JexValue.FromBoolean(!operand.AsBoolean()),
            "-" => JexValue.FromNumber(-operand.AsNumber()),
            _ => throw new JexRuntimeException($"Unknown unary operator '{ue.Operator}'", ue.Span)
        };
    }

    private JexValue EvaluateBinary(BinaryExpression be)
    {
        // Short-circuit for && and ||
        if (be.Operator == "&&")
        {
            var left = Evaluate(be.Left);
            if (!left.AsBoolean())
                return JexValue.False;
            return JexValue.FromBoolean(Evaluate(be.Right).AsBoolean());
        }

        if (be.Operator == "||")
        {
            var left = Evaluate(be.Left);
            if (left.AsBoolean())
                return JexValue.True;
            return JexValue.FromBoolean(Evaluate(be.Right).AsBoolean());
        }

        var leftVal = Evaluate(be.Left);
        var rightVal = Evaluate(be.Right);

        return be.Operator switch
        {
            "+" when leftVal.IsString || rightVal.IsString =>
                JexValue.FromString(leftVal.AsString() + rightVal.AsString()),
            "+" => JexValue.FromNumber(leftVal.AsNumber() + rightVal.AsNumber()),
            "-" => JexValue.FromNumber(leftVal.AsNumber() - rightVal.AsNumber()),
            "*" => JexValue.FromNumber(leftVal.AsNumber() * rightVal.AsNumber()),
            "/" => JexValue.FromNumber(rightVal.AsNumber() == 0 ? 0 : leftVal.AsNumber() / rightVal.AsNumber()),
            "%" => JexValue.FromNumber(rightVal.AsNumber() == 0 ? 0 : leftVal.AsNumber() % rightVal.AsNumber()),
            "==" => JexValue.FromBoolean(ValuesEqual(leftVal, rightVal)),
            "!=" => JexValue.FromBoolean(!ValuesEqual(leftVal, rightVal)),
            "<" => JexValue.FromBoolean(leftVal.AsNumber() < rightVal.AsNumber()),
            "<=" => JexValue.FromBoolean(leftVal.AsNumber() <= rightVal.AsNumber()),
            ">" => JexValue.FromBoolean(leftVal.AsNumber() > rightVal.AsNumber()),
            ">=" => JexValue.FromBoolean(leftVal.AsNumber() >= rightVal.AsNumber()),
            _ => throw new JexRuntimeException($"Unknown binary operator '{be.Operator}'", be.Span)
        };
    }

    private bool ValuesEqual(JexValue left, JexValue right)
    {
        if (left.IsNull && right.IsNull) return true;
        if (left.IsNull || right.IsNull) return false;

        // Compare as same type if both are same kind
        if (left.Kind == right.Kind)
        {
            return left.Equals(right);
        }

        // Otherwise compare as strings
        return left.AsString() == right.AsString();
    }

    private JexValue EvaluateFunctionCall(FunctionCall fc)
    {
        // Check for user-defined function first (in-script functions)
        if (_program.UserFunctions.TryGetValue(fc.Name, out var userFunc))
        {
            return ExecuteUserFunction(userFunc, fc.Arguments, fc.Span);
        }

        // Check for library functions
        if (_libraryManager != null && _libraryManager.TryGetFunction(fc.Name, out var libFunc, out _))
        {
            return ExecuteUserFunction(libFunc!, fc.Arguments, fc.Span);
        }

        // Check for built-in function (registered functions)
        var args = new List<JexValue>();
        foreach (var arg in fc.Arguments)
        {
            args.Add(Evaluate(arg));
        }

        return _functionRegistry.Invoke(fc.Name, _context, args, fc.Span);
    }

    private JexValue ExecuteUserFunction(FunctionDeclaration func, List<Expression> args, SourceSpan callSpan)
    {
        _context.RecursionDepth++;
        _context.CheckRecursionLimit(callSpan);

        _context.PushScope();
        try
        {
            // Bind parameters
            for (int i = 0; i < func.Parameters.Count; i++)
            {
                var value = i < args.Count ? Evaluate(args[i]) : JexValue.Null;
                _context.SetVariable(func.Parameters[i], value);
            }

            // Execute function body
            foreach (var stmt in func.Body)
            {
                ExecuteStatement(stmt);
                if (_context.ShouldReturn)
                {
                    _context.ShouldReturn = false;
                    return _context.ReturnValue;
                }
            }

            return JexValue.Null;
        }
        finally
        {
            _context.PopScope();
            _context.RecursionDepth--;
        }
    }

    private JexValue EvaluateJsonObject(JsonObjectLiteral jol)
    {
        var obj = new JObject();
        foreach (var (key, value) in jol.Properties)
        {
            var keyStr = Evaluate(key).AsString();
            var val = Evaluate(value).AsJson();
            obj[keyStr] = val;
        }
        return JexValue.FromJson(obj);
    }

    private JexValue EvaluateJsonArray(JsonArrayLiteral jal)
    {
        var arr = new JArray();
        foreach (var elem in jal.Elements)
        {
            arr.Add(Evaluate(elem).AsJson());
        }
        return JexValue.FromJson(arr);
    }

    private JexValue EvaluatePropertyAccess(PropertyAccess pa)
    {
        var target = Evaluate(pa.Target);
        var json = target.AsJson();

        if (json is JObject obj && obj.TryGetValue(pa.Property, out var value))
        {
            return JexValue.FromJson(value);
        }

        if (_context.Options.Strict)
        {
            throw new JexRuntimeException($"Property '{pa.Property}' not found", pa.Span);
        }

        return JexValue.Null;
    }

    private JexValue EvaluateIndexAccess(IndexAccess ia)
    {
        var target = Evaluate(ia.Target);
        var index = Evaluate(ia.Index);
        var json = target.AsJson();

        if (json is JArray arr)
        {
            int idx = (int)index.AsNumber();
            if (idx >= 0 && idx < arr.Count)
            {
                return JexValue.FromJson(arr[idx]);
            }
        }
        else if (json is JObject obj && index.IsString)
        {
            if (obj.TryGetValue(index.AsString(), out var value))
            {
                return JexValue.FromJson(value);
            }
        }

        if (_context.Options.Strict)
        {
            throw new JexRuntimeException($"Index access failed", ia.Span);
        }

        return JexValue.Null;
    }
}
