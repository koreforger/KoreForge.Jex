using Xunit;
using KoreForge.Jex;

using KoreForge.Jex.Preprocessing;
using Newtonsoft.Json.Linq;

namespace KoreForge.Jex.Tests;

public class JsonNormalizerTests
{
    [Fact]
    public void Normalizer_Should_Parse_Json_In_String()
    {
        var input = JObject.Parse("{\"data\": \"{\\\"name\\\":\\\"John\\\",\\\"age\\\":30}\"}");
        
        var result = JsonStringNormalizer.NormalizeJsonStrings(input);
        
        Assert.Equal("John", result["data"]?["name"]?.Value<string>());
        Assert.Equal(30, result["data"]?["age"]?.Value<int>());
    }

    [Fact]
    public void Normalizer_Should_Handle_Array_In_String()
    {
        var input = JObject.Parse("{\"items\": \"[1,2,3]\"}");
        
        var result = JsonStringNormalizer.NormalizeJsonStrings(input);
        
        var items = result["items"] as JArray;
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public void Normalizer_Should_Leave_Regular_Strings_Alone()
    {
        var input = JObject.Parse("{\"message\": \"Hello World\", \"number\": 42}");
        
        var result = JsonStringNormalizer.NormalizeJsonStrings(input);
        
        Assert.Equal("Hello World", result["message"]?.Value<string>());
        Assert.Equal(42, result["number"]?.Value<int>());
    }

    [Fact]
    public void Normalizer_Should_Respect_MaxNodesVisited()
    {
        var input = JObject.Parse("{\"a\": 1, \"b\": 2, \"c\": 3, \"d\": 4, \"e\": 5}");
        
        var options = new JsonStringNormalizationOptions(MaxNodesVisited: 3);
        
        Assert.Throws<InvalidOperationException>(() => 
            JsonStringNormalizer.NormalizeJsonStrings(input, options));
    }

    [Fact]
    public void Normalizer_Should_Not_Modify_Original()
    {
        var input = JObject.Parse("{\"data\": \"{\\\"x\\\":1}\"}");
        var originalData = input["data"]?.Value<string>();
        
        var result = JsonStringNormalizer.NormalizeJsonStrings(input);
        
        // Original should be unchanged
        Assert.Equal(originalData, input["data"]?.Value<string>());
        // Result should have parsed JSON
        Assert.IsType<JObject>(result["data"]);
    }

    [Fact]
    public void Normalizer_Should_Handle_Empty_Objects()
    {
        var input = JObject.Parse("{\"empty\": \"{}\", \"emptyArray\": \"[]\"}");
        
        var result = JsonStringNormalizer.NormalizeJsonStrings(input);
        
        Assert.IsType<JObject>(result["empty"]);
        Assert.IsType<JArray>(result["emptyArray"]);
    }

    [Fact]
    public void Normalizer_Should_Skip_Long_Strings()
    {
        var longString = new string('x', 1000);
        var input = new JObject { ["long"] = longString };
        
        var options = new JsonStringNormalizationOptions(MaxStringLength: 100);
        var result = JsonStringNormalizer.NormalizeJsonStrings(input, options);
        
        // Should remain a string since it exceeds max length
        Assert.Equal(longString, result["long"]?.Value<string>());
    }

    [Fact]
    public void Normalizer_Should_Handle_Nested_Objects_In_Array()
    {
        var input = JObject.Parse("{\"items\": \"[{\\\"id\\\":1},{\\\"id\\\":2}]\"}");
        
        var result = JsonStringNormalizer.NormalizeJsonStrings(input);
        
        var items = result["items"] as JArray;
        Assert.NotNull(items);
        Assert.Equal(2, items.Count);
        Assert.Equal(1, items[0]?["id"]?.Value<int>());
    }
}
