using Xunit;

using Newtonsoft.Json.Linq;

namespace KoreForge.Jex.Tests;

public class JexValueTests
{
    [Fact]
    public void JexValue_Null_Should_Be_Null()
    {
        var value = JexValue.Null;
        
        Assert.True(value.IsNull);
        Assert.Equal(JexValueKind.Null, value.Kind);
    }

    [Fact]
    public void JexValue_FromBoolean_Should_Work()
    {
        var trueVal = JexValue.FromBoolean(true);
        var falseVal = JexValue.FromBoolean(false);
        
        Assert.True(trueVal.IsBoolean);
        Assert.True(trueVal.AsBoolean());
        Assert.False(falseVal.AsBoolean());
    }

    [Fact]
    public void JexValue_FromNumber_Should_Work()
    {
        var value = JexValue.FromNumber(42.5m);
        
        Assert.True(value.IsNumber);
        Assert.Equal(42.5m, value.AsNumber());
    }

    [Fact]
    public void JexValue_FromString_Should_Work()
    {
        var value = JexValue.FromString("hello");
        
        Assert.True(value.IsString);
        Assert.Equal("hello", value.AsString());
    }

    [Fact]
    public void JexValue_FromJson_Should_Work()
    {
        var obj = JObject.Parse("{\"x\": 1}");
        var value = JexValue.FromJson(obj);
        
        Assert.True(value.IsJson);
        var json = value.AsJson();
        Assert.NotNull(json);
        Assert.Equal(1, json["x"]?.Value<int>());
    }

    [Fact]
    public void JexValue_AsBoolean_Should_Convert()
    {
        Assert.False(JexValue.Null.AsBoolean());
        Assert.False(JexValue.FromNumber(0).AsBoolean());
        Assert.True(JexValue.FromNumber(1).AsBoolean());
        Assert.False(JexValue.FromString("").AsBoolean());
        Assert.True(JexValue.FromString("hello").AsBoolean());
    }

    [Fact]
    public void JexValue_AsNumber_Should_Convert()
    {
        Assert.Equal(0m, JexValue.Null.AsNumber());
        Assert.Equal(1m, JexValue.FromBoolean(true).AsNumber());
        Assert.Equal(0m, JexValue.FromBoolean(false).AsNumber());
        Assert.Equal(42m, JexValue.FromString("42").AsNumber());
    }

    [Fact]
    public void JexValue_AsString_Should_Convert()
    {
        Assert.Equal(string.Empty, JexValue.Null.AsString());
        Assert.Equal("true", JexValue.FromBoolean(true).AsString());
        Assert.Equal("42.5", JexValue.FromNumber(42.5m).AsString());
    }

    [Fact]
    public void JexValue_Equality_Should_Work()
    {
        Assert.Equal(JexValue.Null, JexValue.Null);
        Assert.Equal(JexValue.FromNumber(42), JexValue.FromNumber(42));
        Assert.NotEqual(JexValue.FromNumber(42), JexValue.FromNumber(43));
        Assert.Equal(JexValue.FromString("test"), JexValue.FromString("test"));
    }
}
