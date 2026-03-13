using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace KoreForge.Jex.Runtime;

/// <summary>
/// Standard library of built-in functions for JEX.
/// </summary>
internal static class JexStandardLibrary
{
    public static void RegisterAll(JexFunctionRegistry registry)
    {
        RegisterJsonPathFunctions(registry);
        RegisterStringFunctions(registry);
        RegisterMathFunctions(registry);
        RegisterDateFunctions(registry);
        RegisterTypeFunctions(registry);
        RegisterArrayFunctions(registry);
        RegisterOutputFunctions(registry);
    }

    private static void RegisterJsonPathFunctions(JexFunctionRegistry registry)
    {
        // jp1(json, path) - Returns the first token at path
        registry.Register("jp1", (ctx, args) =>
        {
            var json = args[0].AsJson();
            var path = args[1].AsString();
            if (json is null) return JexValue.Null;

            try
            {
                var token = json.SelectToken(path);
                return JexValue.FromJson(token);
            }
            catch
            {
                return JexValue.Null;
            }
        }, minArgs: 2, maxArgs: 2);

        // jpAll(json, path) - Returns array of all matches
        registry.Register("jpAll", (ctx, args) =>
        {
            var json = args[0].AsJson();
            var path = args[1].AsString();
            if (json is null) return JexValue.FromJson(new JArray());

            try
            {
                var tokens = json.SelectTokens(path);
                var arr = new JArray();
                foreach (var t in tokens)
                    arr.Add(t);
                return JexValue.FromJson(arr);
            }
            catch
            {
                return JexValue.FromJson(new JArray());
            }
        }, minArgs: 2, maxArgs: 2);

        // coalescePath(json, path1, path2, ...) - Returns first found value
        registry.Register("coalescePath", (ctx, args) =>
        {
            if (args.Count < 2) return JexValue.Null;
            var json = args[0].AsJson();
            if (json is null) return JexValue.Null;

            for (int i = 1; i < args.Count; i++)
            {
                var path = args[i].AsString();
                try
                {
                    var token = json.SelectToken(path);
                    if (token is not null && token.Type != JTokenType.Null)
                        return JexValue.FromJson(token);
                }
                catch
                {
                    // Continue to next path
                }
            }
            return JexValue.Null;
        }, minArgs: 2);

        // existsPath(json, path) - Returns true if path exists
        registry.Register("existsPath", (ctx, args) =>
        {
            var json = args[0].AsJson();
            var path = args[1].AsString();
            if (json is null) return JexValue.False;

            try
            {
                var token = json.SelectToken(path);
                return JexValue.FromBoolean(token is not null);
            }
            catch
            {
                return JexValue.False;
            }
        }, minArgs: 2, maxArgs: 2);
    }

    private static void RegisterStringFunctions(JexFunctionRegistry registry)
    {
        // trim(s)
        registry.Register("trim", (ctx, args) =>
        {
            var s = args[0].AsString();
            return JexValue.FromString(s.Trim());
        }, minArgs: 1, maxArgs: 1);

        // lower(s)
        registry.Register("lower", (ctx, args) =>
        {
            var s = args[0].AsString();
            return JexValue.FromString(s.ToLowerInvariant());
        }, minArgs: 1, maxArgs: 1);

        // upper(s)
        registry.Register("upper", (ctx, args) =>
        {
            var s = args[0].AsString();
            return JexValue.FromString(s.ToUpperInvariant());
        }, minArgs: 1, maxArgs: 1);

        // substr(s, start, length?)
        registry.Register("substr", (ctx, args) =>
        {
            var s = args[0].AsString();
            var start = (int)args[1].AsNumber();
            if (start < 0) start = 0;
            if (start >= s.Length) return JexValue.FromString(string.Empty);

            if (args.Count > 2)
            {
                var length = (int)args[2].AsNumber();
                if (length <= 0) return JexValue.FromString(string.Empty);
                length = Math.Min(length, s.Length - start);
                return JexValue.FromString(s.Substring(start, length));
            }
            return JexValue.FromString(s[start..]);
        }, minArgs: 2, maxArgs: 3);

        // left(s, n)
        registry.Register("left", (ctx, args) =>
        {
            var s = args[0].AsString();
            var n = (int)args[1].AsNumber();
            if (n <= 0) return JexValue.FromString(string.Empty);
            n = Math.Min(n, s.Length);
            return JexValue.FromString(s[..n]);
        }, minArgs: 2, maxArgs: 2);

        // right(s, n)
        registry.Register("right", (ctx, args) =>
        {
            var s = args[0].AsString();
            var n = (int)args[1].AsNumber();
            if (n <= 0) return JexValue.FromString(string.Empty);
            n = Math.Min(n, s.Length);
            return JexValue.FromString(s[^n..]);
        }, minArgs: 2, maxArgs: 2);

        // split(s, delimiter)
        registry.Register("split", (ctx, args) =>
        {
            var s = args[0].AsString();
            var delim = args[1].AsString();
            var parts = s.Split(delim);
            var arr = new JArray();
            foreach (var p in parts)
                arr.Add(p);
            return JexValue.FromJson(arr);
        }, minArgs: 2, maxArgs: 2);

        // join(array, delimiter)
        registry.Register("join", (ctx, args) =>
        {
            var arr = args[0].AsJson();
            var delim = args[1].AsString();
            if (arr is JArray jarr)
            {
                var strings = jarr.Select(t => t?.ToString() ?? "");
                return JexValue.FromString(string.Join(delim, strings));
            }
            return JexValue.FromString(args[0].AsString());
        }, minArgs: 2, maxArgs: 2);

        // replace(s, find, repl)
        registry.Register("replace", (ctx, args) =>
        {
            var s = args[0].AsString();
            var find = args[1].AsString();
            var repl = args[2].AsString();
            return JexValue.FromString(s.Replace(find, repl));
        }, minArgs: 3, maxArgs: 3);

        // regexMatch(s, pattern)
        registry.Register("regexMatch", (ctx, args) =>
        {
            var s = args[0].AsString();
            var pattern = args[1].AsString();
            try
            {
                var timeout = TimeSpan.FromMilliseconds(ctx.Options.RegexTimeoutMs);
                var regex = new Regex(pattern, RegexOptions.None, timeout);
                return JexValue.FromBoolean(regex.IsMatch(s));
            }
            catch (RegexMatchTimeoutException)
            {
                throw new JexRuntimeException("Regex timeout exceeded");
            }
        }, minArgs: 2, maxArgs: 2);

        // regexReplace(s, pattern, repl)
        registry.Register("regexReplace", (ctx, args) =>
        {
            var s = args[0].AsString();
            var pattern = args[1].AsString();
            var repl = args[2].AsString();
            try
            {
                var timeout = TimeSpan.FromMilliseconds(ctx.Options.RegexTimeoutMs);
                var regex = new Regex(pattern, RegexOptions.None, timeout);
                return JexValue.FromString(regex.Replace(s, repl));
            }
            catch (RegexMatchTimeoutException)
            {
                throw new JexRuntimeException("Regex timeout exceeded");
            }
        }, minArgs: 3, maxArgs: 3);

        // concat(s1, s2, ...)
        registry.Register("concat", (ctx, args) =>
        {
            var sb = new System.Text.StringBuilder();
            foreach (var arg in args)
                sb.Append(arg.AsString());
            return JexValue.FromString(sb.ToString());
        }, minArgs: 0);

        // length(s) or length(array)
        registry.Register("length", (ctx, args) =>
        {
            var val = args[0];
            if (val.IsString)
                return JexValue.FromNumber(val.AsString().Length);
            if (val.IsJson)
            {
                var json = val.AsJson();
                if (json is JArray arr)
                    return JexValue.FromNumber(arr.Count);
                if (json is JObject obj)
                    return JexValue.FromNumber(obj.Count);
            }
            return JexValue.FromNumber(0);
        }, minArgs: 1, maxArgs: 1);
    }

    private static void RegisterMathFunctions(JexFunctionRegistry registry)
    {
        registry.Register("abs", (ctx, args) =>
            JexValue.FromNumber(Math.Abs(args[0].AsNumber())), minArgs: 1, maxArgs: 1);

        registry.Register("min", (ctx, args) =>
            JexValue.FromNumber(Math.Min(args[0].AsNumber(), args[1].AsNumber())), minArgs: 2, maxArgs: 2);

        registry.Register("max", (ctx, args) =>
            JexValue.FromNumber(Math.Max(args[0].AsNumber(), args[1].AsNumber())), minArgs: 2, maxArgs: 2);

        registry.Register("round", (ctx, args) =>
        {
            var n = args[0].AsNumber();
            var digits = args.Count > 1 ? (int)args[1].AsNumber() : 0;
            return JexValue.FromNumber(Math.Round(n, digits));
        }, minArgs: 1, maxArgs: 2);

        registry.Register("floor", (ctx, args) =>
            JexValue.FromNumber(Math.Floor(args[0].AsNumber())), minArgs: 1, maxArgs: 1);

        registry.Register("ceil", (ctx, args) =>
            JexValue.FromNumber(Math.Ceiling(args[0].AsNumber())), minArgs: 1, maxArgs: 1);
    }

    private static void RegisterDateFunctions(JexFunctionRegistry registry)
    {
        // parseDate(s, format?, timezone?)
        registry.Register("parseDate", (ctx, args) =>
        {
            var s = args[0].AsString();
            string? format = args.Count > 1 ? args[1].AsString() : null;

            DateTimeOffset dt;
            if (format is not null)
            {
                if (DateTimeOffset.TryParseExact(s, format, CultureInfo.InvariantCulture, 
                    DateTimeStyles.AssumeUniversal, out dt))
                    return JexValue.FromDateTime(dt);
            }
            else
            {
                if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, 
                    DateTimeStyles.AssumeUniversal, out dt))
                    return JexValue.FromDateTime(dt);
            }
            return JexValue.Null;
        }, minArgs: 1, maxArgs: 3);

        // formatDate(dt, format)
        registry.Register("formatDate", (ctx, args) =>
        {
            var dt = args[0].AsDateTime();
            var format = args[1].AsString();
            return JexValue.FromString(dt.ToString(format, CultureInfo.InvariantCulture));
        }, minArgs: 2, maxArgs: 2);

        // dateAdd(dt, unit, amount)
        registry.Register("dateAdd", (ctx, args) =>
        {
            var dt = args[0].AsDateTime();
            var unit = args[1].AsString().ToLowerInvariant();
            var amount = (int)args[2].AsNumber();

            var result = unit switch
            {
                "days" => dt.AddDays(amount),
                "hours" => dt.AddHours(amount),
                "minutes" => dt.AddMinutes(amount),
                "seconds" => dt.AddSeconds(amount),
                "months" => dt.AddMonths(amount),
                "years" => dt.AddYears(amount),
                _ => dt
            };
            return JexValue.FromDateTime(result);
        }, minArgs: 3, maxArgs: 3);

        // dateDiff(a, b, unit)
        registry.Register("dateDiff", (ctx, args) =>
        {
            var a = args[0].AsDateTime();
            var b = args[1].AsDateTime();
            var unit = args[2].AsString().ToLowerInvariant();
            var diff = a - b;

            decimal result = unit switch
            {
                "days" => (decimal)diff.TotalDays,
                "hours" => (decimal)diff.TotalHours,
                "minutes" => (decimal)diff.TotalMinutes,
                "seconds" => (decimal)diff.TotalSeconds,
                _ => (decimal)diff.TotalDays
            };
            return JexValue.FromNumber(result);
        }, minArgs: 3, maxArgs: 3);

        // now()
        registry.Register("now", (ctx, args) =>
            JexValue.FromDateTime(DateTimeOffset.UtcNow), minArgs: 0, maxArgs: 0);
    }

    private static void RegisterTypeFunctions(JexFunctionRegistry registry)
    {
        registry.Register("toString", (ctx, args) =>
            JexValue.FromString(args[0].AsString()), minArgs: 1, maxArgs: 1);

        registry.Register("toNumber", (ctx, args) =>
            JexValue.FromNumber(args[0].AsNumber()), minArgs: 1, maxArgs: 1);

        registry.Register("toBool", (ctx, args) =>
            JexValue.FromBoolean(args[0].AsBoolean()), minArgs: 1, maxArgs: 1);

        registry.Register("toDate", (ctx, args) =>
            JexValue.FromDateTime(args[0].AsDateTime()), minArgs: 1, maxArgs: 1);

        registry.Register("isNull", (ctx, args) =>
            JexValue.FromBoolean(args[0].IsNull), minArgs: 1, maxArgs: 1);

        registry.Register("isEmpty", (ctx, args) =>
        {
            var val = args[0];
            if (val.IsNull) return JexValue.True;
            if (val.IsString) return JexValue.FromBoolean(string.IsNullOrEmpty(val.AsString()));
            if (val.IsJson)
            {
                var json = val.AsJson();
                if (json is JArray arr) return JexValue.FromBoolean(arr.Count == 0);
                if (json is JObject obj) return JexValue.FromBoolean(obj.Count == 0);
            }
            return JexValue.False;
        }, minArgs: 1, maxArgs: 1);

        registry.Register("typeOf", (ctx, args) =>
        {
            var val = args[0];
            string typeName = val.Kind switch
            {
                JexValueKind.Null => "null",
                JexValueKind.Boolean => "boolean",
                JexValueKind.Number => "number",
                JexValueKind.String => "string",
                JexValueKind.DateTime => "datetime",
                JexValueKind.Json => val.AsJson() switch
                {
                    JArray => "array",
                    JObject => "object",
                    _ => "json"
                },
                _ => "unknown"
            };
            return JexValue.FromString(typeName);
        }, minArgs: 1, maxArgs: 1);
    }

    private static void RegisterArrayFunctions(JexFunctionRegistry registry)
    {
        // arr(v1, v2, ...) - Creates an array
        registry.Register("arr", (ctx, args) =>
        {
            var arr = new JArray();
            foreach (var arg in args)
                arr.Add(arg.AsJson());
            return JexValue.FromJson(arr);
        }, minArgs: 0);

        // obj(k1, v1, k2, v2, ...) - Creates an object
        registry.Register("obj", (ctx, args) =>
        {
            var obj = new JObject();
            for (int i = 0; i + 1 < args.Count; i += 2)
            {
                var key = args[i].AsString();
                var val = args[i + 1].AsJson();
                obj[key] = val;
            }
            return JexValue.FromJson(obj);
        }, minArgs: 0);

        // push(array, value) - Adds value to array
        registry.Register("push", (ctx, args) =>
        {
            var json = args[0].AsJson();
            if (json is JArray arr)
            {
                arr.Add(args[1].AsJson());
            }
            return JexValue.Null;
        }, minArgs: 2, maxArgs: 2);

        // indexBy(array, keyPath) - Creates a lookup map
        registry.Register("indexBy", (ctx, args) =>
        {
            var json = args[0].AsJson();
            var keyPath = args[1].AsString();

            if (json is not JArray arr)
                return JexValue.FromJson(new JObject());

            var result = new JObject();
            foreach (var item in arr)
            {
                var key = item.SelectToken(keyPath);
                if (key is not null)
                {
                    var keyStr = key.Type == JTokenType.String ? key.Value<string>() : key.ToString();
                    if (keyStr is not null)
                        result[keyStr] = item;
                }
            }
            return JexValue.FromJson(result);
        }, minArgs: 2, maxArgs: 2);

        // lookup(map, key) - Looks up value in indexed map
        registry.Register("lookup", (ctx, args) =>
        {
            var map = args[0].AsJson();
            var key = args[1].AsString();

            if (map is JObject obj && obj.TryGetValue(key, out var value))
            {
                return JexValue.FromJson(value);
            }
            return JexValue.Null;
        }, minArgs: 2, maxArgs: 2);

        // first(array) - Returns first element
        registry.Register("first", (ctx, args) =>
        {
            var json = args[0].AsJson();
            if (json is JArray arr && arr.Count > 0)
                return JexValue.FromJson(arr[0]);
            return JexValue.Null;
        }, minArgs: 1, maxArgs: 1);

        // last(array) - Returns last element
        registry.Register("last", (ctx, args) =>
        {
            var json = args[0].AsJson();
            if (json is JArray arr && arr.Count > 0)
                return JexValue.FromJson(arr[^1]);
            return JexValue.Null;
        }, minArgs: 1, maxArgs: 1);

        // count(array) - Returns element count
        registry.Register("count", (ctx, args) =>
        {
            var json = args[0].AsJson();
            if (json is JArray arr)
                return JexValue.FromNumber(arr.Count);
            return JexValue.FromNumber(0);
        }, minArgs: 1, maxArgs: 1);
    }

    private static void RegisterOutputFunctions(JexFunctionRegistry registry)
    {
        // setPath(target, path, value) - Sets a value at a path
        registry.Register("setPath", (ctx, args) =>
        {
            var target = args[0].AsJson();
            var path = args[1].AsString();
            var value = args[2].AsJson();

            if (target is JObject obj)
            {
                // Simple implementation - just set direct property for now
                var pathParts = path.TrimStart('$', '.').Split('.');
                JToken current = obj;
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    var part = pathParts[i];
                    if (current is JObject co)
                    {
                        if (co[part] is null)
                            co[part] = new JObject();
                        current = co[part]!;
                    }
                }
                if (current is JObject final && pathParts.Length > 0)
                {
                    final[pathParts[^1]] = value;
                }
            }
            return JexValue.Null;
        }, minArgs: 3, maxArgs: 3);

        // expandJson(json, path) - Recursively parses JSON string at a specific path
        // expandJson(json, path, maxDepth) - With depth limit (default 10)
        registry.Register("expandJson", (ctx, args) =>
        {
            var json = args[0].AsJson()?.DeepClone();
            var path = args[1].AsString();
            int maxDepth = args.Count > 2 ? (int)args[2].AsNumber() : 10;

            if (json is null) return JexValue.Null;

            try
            {
                var token = json.SelectToken(path);
                if (token is null) return JexValue.FromJson(json);

                var expanded = ExpandNestedJson(token, maxDepth, 0);
                
                // Replace the token at the path with the expanded version
                if (token.Parent is JProperty prop)
                {
                    prop.Value = expanded;
                }
                else if (token.Parent is JArray arr)
                {
                    var index = arr.IndexOf(token);
                    arr[index] = expanded;
                }
                else if (token == json)
                {
                    // If the path points to the root, return the expanded version directly
                    return JexValue.FromJson(expanded);
                }

                return JexValue.FromJson(json);
            }
            catch
            {
                return JexValue.FromJson(json);
            }
        }, minArgs: 2, maxArgs: 3);

        // expandJsonAll(json) - Recursively parses all JSON strings in the entire document
        // expandJsonAll(json, maxDepth) - With depth limit (default 10)
        registry.Register("expandJsonAll", (ctx, args) =>
        {
            var json = args[0].AsJson();
            int maxDepth = args.Count > 1 ? (int)args[1].AsNumber() : 10;

            if (json is null) return JexValue.Null;

            var expanded = ExpandNestedJson(json.DeepClone(), maxDepth, 0);
            return JexValue.FromJson(expanded);
        }, minArgs: 1, maxArgs: 2);
    }

    /// <summary>
    /// Recursively expands JSON strings within a JSON token.
    /// </summary>
    private static JToken ExpandNestedJson(JToken token, int maxDepth, int currentDepth)
    {
        if (currentDepth >= maxDepth)
            return token;

        switch (token)
        {
            case JValue jValue when jValue.Type == JTokenType.String:
                var str = jValue.Value<string>();
                if (str is not null && IsLikelyJson(str))
                {
                    try
                    {
                        var parsed = JToken.Parse(str);
                        // Recursively expand the parsed content with incremented depth
                        return ExpandNestedJson(parsed, maxDepth, currentDepth + 1);
                    }
                    catch
                    {
                        // Not valid JSON, return as-is
                        return token;
                    }
                }
                return token;

            case JObject jObj:
                var newObj = new JObject();
                foreach (var prop in jObj.Properties())
                {
                    newObj[prop.Name] = ExpandNestedJson(prop.Value, maxDepth, currentDepth);
                }
                return newObj;

            case JArray jArr:
                var newArr = new JArray();
                foreach (var item in jArr)
                {
                    newArr.Add(ExpandNestedJson(item, maxDepth, currentDepth));
                }
                return newArr;

            default:
                return token;
        }
    }

    /// <summary>
    /// Quick check if a string might be JSON.
    /// </summary>
    private static bool IsLikelyJson(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return false;

        str = str.TrimStart();
        return str.StartsWith('{') || str.StartsWith('[') || str.StartsWith('"');
    }
}
