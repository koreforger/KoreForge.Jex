using Newtonsoft.Json.Linq;
using System;

namespace KoreForge.Jex;

/// <summary>
/// Represents the kind of value in the JEX runtime.
/// </summary>
public enum JexValueKind
{
    Null,
    Boolean,
    Number,
    String,
    DateTime,
    Json
}

/// <summary>
/// Represents a runtime value in the JEX language.
/// JEX is dynamically typed, so values can be any of the supported types.
/// </summary>
public readonly struct JexValue : IEquatable<JexValue>
{
    public JexValueKind Kind { get; }
    private readonly object? _value;

    private JexValue(JexValueKind kind, object? value)
    {
        Kind = kind;
        _value = value;
    }

    public static JexValue Null => new(JexValueKind.Null, null);
    public static JexValue True => new(JexValueKind.Boolean, true);
    public static JexValue False => new(JexValueKind.Boolean, false);

    public static JexValue FromBoolean(bool value) => new(JexValueKind.Boolean, value);
    public static JexValue FromNumber(decimal value) => new(JexValueKind.Number, value);
    public static JexValue FromString(string? value) => value is null ? Null : new(JexValueKind.String, value);
    public static JexValue FromDateTime(DateTimeOffset value) => new(JexValueKind.DateTime, value);
    public static JexValue FromJson(JToken? token) => token is null || token.Type == JTokenType.Null 
        ? Null 
        : new(JexValueKind.Json, token);

    public bool IsNull => Kind == JexValueKind.Null;
    public bool IsBoolean => Kind == JexValueKind.Boolean;
    public bool IsNumber => Kind == JexValueKind.Number;
    public bool IsString => Kind == JexValueKind.String;
    public bool IsDateTime => Kind == JexValueKind.DateTime;
    public bool IsJson => Kind == JexValueKind.Json;

    public bool AsBoolean() => Kind switch
    {
        JexValueKind.Null => false,
        JexValueKind.Boolean => (bool)_value!,
        JexValueKind.Number => (decimal)_value! != 0m,
        JexValueKind.String => !string.IsNullOrEmpty((string)_value!),
        JexValueKind.Json => _value is not null,
        _ => false
    };

    public decimal AsNumber() => Kind switch
    {
        JexValueKind.Null => 0m,
        JexValueKind.Boolean => (bool)_value! ? 1m : 0m,
        JexValueKind.Number => (decimal)_value!,
        JexValueKind.String => decimal.TryParse((string)_value!, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m,
        JexValueKind.Json => ConvertJsonToNumber((JToken)_value!),
        _ => 0m
    };

    private static decimal ConvertJsonToNumber(JToken token)
    {
        return token.Type switch
        {
            JTokenType.Integer => token.Value<long>(),
            JTokenType.Float => (decimal)token.Value<double>(),
            JTokenType.String => decimal.TryParse(token.Value<string>(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m,
            JTokenType.Boolean => token.Value<bool>() ? 1m : 0m,
            _ => 0m
        };
    }

    public string AsString() => Kind switch
    {
        JexValueKind.Null => string.Empty,
        JexValueKind.Boolean => (bool)_value! ? "true" : "false",
        JexValueKind.Number => ((decimal)_value!).ToString(System.Globalization.CultureInfo.InvariantCulture),
        JexValueKind.String => (string)_value!,
        JexValueKind.DateTime => ((DateTimeOffset)_value!).ToString("o"),
        JexValueKind.Json => ((JToken)_value!).ToString(),
        _ => string.Empty
    };

    public DateTimeOffset AsDateTime() => Kind switch
    {
        JexValueKind.DateTime => (DateTimeOffset)_value!,
        JexValueKind.String => DateTimeOffset.TryParse((string)_value!, out var dt) ? dt : default,
        JexValueKind.Number => DateTimeOffset.FromUnixTimeSeconds((long)(decimal)_value!),
        _ => default
    };

    public JToken? AsJson() => Kind switch
    {
        JexValueKind.Null => JValue.CreateNull(),
        JexValueKind.Boolean => new JValue((bool)_value!),
        JexValueKind.Number => new JValue((decimal)_value!),
        JexValueKind.String => new JValue((string)_value!),
        JexValueKind.DateTime => new JValue((DateTimeOffset)_value!),
        JexValueKind.Json => (JToken)_value!,
        _ => JValue.CreateNull()
    };

    public object? RawValue => _value;

    public bool Equals(JexValue other)
    {
        if (Kind != other.Kind) return false;
        return Kind switch
        {
            JexValueKind.Null => true,
            JexValueKind.Boolean => (bool)_value! == (bool)other._value!,
            JexValueKind.Number => (decimal)_value! == (decimal)other._value!,
            JexValueKind.String => (string)_value! == (string)other._value!,
            JexValueKind.DateTime => (DateTimeOffset)_value! == (DateTimeOffset)other._value!,
            JexValueKind.Json => JToken.DeepEquals((JToken)_value!, (JToken)other._value!),
            _ => false
        };
    }

    public override bool Equals(object? obj) => obj is JexValue other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Kind, _value);

    public static bool operator ==(JexValue left, JexValue right) => left.Equals(right);
    public static bool operator !=(JexValue left, JexValue right) => !left.Equals(right);

    public override string ToString() => $"JexValue({Kind}: {AsString()})";
}
