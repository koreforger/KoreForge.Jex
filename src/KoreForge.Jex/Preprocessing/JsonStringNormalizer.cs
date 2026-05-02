using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KoreForge.Jex.Preprocessing;

/// <summary>
/// Normalizes JSON-in-string fields by parsing embedded JSON strings
/// and replacing them with their parsed structure.
/// </summary>
public static class JsonStringNormalizer
{
    /// <summary>
    /// Normalizes JSON strings in the input token, replacing string values
    /// that contain valid JSON with their parsed representation.
    /// </summary>
    public static JToken NormalizeJsonStrings(JToken input, JsonStringNormalizationOptions? options = null)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        options ??= JsonStringNormalizationOptions.Default;

        var state = new NormalizationState(options);
        var clone = input.DeepClone(); // Keep original untouched
        NormalizeToken(clone, depth: 0, state);
        return clone;
    }

    private static void NormalizeToken(JToken token, int depth, NormalizationState state)
    {
        state.CountNode();

        switch (token.Type)
        {
            case JTokenType.Object:
            {
                var obj = (JObject)token;
                foreach (var prop in obj.Properties())
                {
                    NormalizeToken(prop.Value, depth + 1, state);
                }
                break;
            }
            case JTokenType.Array:
            {
                var arr = (JArray)token;
                for (var i = 0; i < arr.Count; i++)
                {
                    NormalizeToken(arr[i], depth + 1, state);
                }
                break;
            }
            case JTokenType.String:
            {
                var jv = (JValue)token;
                var s = (string?)jv.Value;
                if (string.IsNullOrWhiteSpace(s)) return;
                if (s.Length > state.Options.MaxStringLength) return;

                if (!LooksLikeJson(s))
                    return;

                if (TryParsePossiblyEscapedJson(s, state.Options.MaxDepthPerString, out var parsed))
                {
                    state.CountReplacement();

                    // Replace this token with parsed JSON
                    ReplaceTokenInParent(jv, parsed);

                    // Then normalize inside the newly inserted structure too
                    NormalizeToken(parsed, depth + 1, state);
                }
                else if (state.Options.Strict)
                {
                    throw new JsonException("String looked like JSON but could not be parsed.");
                }

                break;
            }
            // Other primitive types are ignored
        }
    }

    private static bool LooksLikeJson(string s)
    {
        var t = s.AsSpan().Trim();
        if (t.Length < 2) return false;

        var first = t[0];
        var last = t[^1];

        return (first == '{' && last == '}') || (first == '[' && last == ']');
    }

    private static bool TryParsePossiblyEscapedJson(string s, int maxDepth, out JToken parsed)
    {
        parsed = null!;

        var current = s.Trim();

        for (var i = 0; i < maxDepth; i++)
        {
            // Attempt direct parse
            if (TryParseJson(current, out parsed))
                return true;

            // Attempt to unescape one layer:
            // If current is a JSON string literal containing JSON, deserialize to string.
            // Example: "\"{\\\"a\\\":1}\"" -> "{\"a\":1}"
            if (!TryUnescapeJsonStringLiteral(current, out var unescaped))
                return false;

            current = unescaped.Trim();
        }

        return false;
    }

    private static bool TryParseJson(string s, out JToken token)
    {
        try
        {
            token = JToken.Parse(s);
            return true;
        }
        catch
        {
            token = null!;
            return false;
        }
    }

    private static bool TryUnescapeJsonStringLiteral(string s, out string unescaped)
    {
        // This tries to interpret s as a JSON string literal.
        // If s isn't a JSON string literal, it will fail.
        try
        {
            var result = JsonConvert.DeserializeObject<string>(s);
            if (result is null)
            {
                unescaped = string.Empty;
                return false;
            }

            unescaped = result;
            return true;
        }
        catch
        {
            unescaped = string.Empty;
            return false;
        }
    }

    private static void ReplaceTokenInParent(JToken oldToken, JToken newToken)
    {
        var parent = oldToken.Parent;
        if (parent is null)
            throw new InvalidOperationException("Cannot replace root token in-place.");

        switch (parent)
        {
            case JProperty prop:
                prop.Value = newToken;
                break;

            case JArray arr:
            {
                var index = arr.IndexOf(oldToken);
                if (index < 0) throw new InvalidOperationException("Array parent did not contain token.");
                arr[index] = newToken;
                break;
            }

            default:
                throw new NotSupportedException($"Unsupported parent type for replacement: {parent.Type}");
        }
    }

    private sealed class NormalizationState
    {
        public NormalizationState(JsonStringNormalizationOptions options) => Options = options;

        public JsonStringNormalizationOptions Options { get; }
        private int _nodesVisited;
        private int _replacements;

        public void CountNode()
        {
            _nodesVisited++;
            if (_nodesVisited > Options.MaxNodesVisited)
                throw new InvalidOperationException($"Preprocess exceeded MaxNodesVisited ({Options.MaxNodesVisited}).");
        }

        public void CountReplacement()
        {
            _replacements++;
            if (_replacements > Options.MaxTotalReplacements)
                throw new InvalidOperationException($"Preprocess exceeded MaxTotalReplacements ({Options.MaxTotalReplacements}).");
        }
    }
}
