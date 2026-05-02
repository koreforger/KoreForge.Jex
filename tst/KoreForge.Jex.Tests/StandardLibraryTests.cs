using Xunit;
using KoreForge.Jex;

using Newtonsoft.Json.Linq;

namespace KoreForge.Jex.Tests;

public class StandardLibraryTests
{
    private readonly Jex _jex = new();

    // JsonPath functions
    [Fact]
    public void Jp1_Should_Extract_Single_Value()
    {
        var script = @"%set $.result = jp1($in, ""$.user.name"");";
        var input = JObject.Parse("{\"user\": {\"name\": \"John\"}}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("John", result["result"]?.Value<string>());
    }

    [Fact]
    public void JpAll_Should_Extract_All_Matches()
    {
        var script = @"%set $.result = jpAll($in, ""$.items[*].id"");";
        var input = JObject.Parse("{\"items\": [{\"id\": 1}, {\"id\": 2}, {\"id\": 3}]}");
        
        var result = _jex.Execute(script, input);
        
        var arr = result["result"] as JArray;
        Assert.NotNull(arr);
        Assert.Equal(3, arr.Count);
    }

    [Fact]
    public void ExistsPath_Should_Return_True_When_Path_Exists()
    {
        var script = @"%set $.exists = existsPath($in, ""$.user.name"");";
        var input = JObject.Parse("{\"user\": {\"name\": \"John\"}}");
        
        var result = _jex.Execute(script, input);
        
        Assert.True(result["exists"]?.Value<bool>());
    }

    [Fact]
    public void ExistsPath_Should_Return_False_When_Path_Missing()
    {
        var script = @"%set $.exists = existsPath($in, ""$.user.email"");";
        var input = JObject.Parse("{\"user\": {\"name\": \"John\"}}");
        
        var result = _jex.Execute(script, input);
        
        Assert.False(result["exists"]?.Value<bool>());
    }

    // String functions
    [Fact]
    public void Replace_Should_Work()
    {
        var script = @"%set $.result = replace(""hello world"", ""world"", ""JEX"");";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("hello JEX", result["result"]?.Value<string>());
    }

    [Fact]
    public void Concat_Should_Join_Strings()
    {
        var script = @"%set $.result = concat(""Hello"", "" "", ""World"");";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("Hello World", result["result"]?.Value<string>());
    }

    [Fact]
    public void Length_Should_Return_String_Length()
    {
        var script = @"%set $.result = length(""hello"");";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(5, result["result"]?.Value<int>());
    }

    [Fact]
    public void Length_Should_Return_Array_Count()
    {
        var script = @"%set $.result = length(arr(1, 2, 3, 4));";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(4, result["result"]?.Value<int>());
    }

    // Math functions
    [Fact]
    public void Abs_Should_Work()
    {
        var script = @"%set $.result = abs(-42);";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(42m, result["result"]?.Value<decimal>());
    }

    [Fact]
    public void Min_Max_Should_Work()
    {
        var script = @"
            %set $.min = min(5, 3);
            %set $.max = max(5, 3);
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(3m, result["min"]?.Value<decimal>());
        Assert.Equal(5m, result["max"]?.Value<decimal>());
    }

    [Fact]
    public void Round_Should_Work()
    {
        var script = @"
            %set $.r1 = round(3.7);
            %set $.r2 = round(3.456, 2);
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(4m, result["r1"]?.Value<decimal>());
        Assert.Equal(3.46m, result["r2"]?.Value<decimal>());
    }

    [Fact]
    public void Floor_Ceil_Should_Work()
    {
        var script = @"
            %set $.floor = floor(3.7);
            %set $.ceil = ceil(3.2);
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(3m, result["floor"]?.Value<decimal>());
        Assert.Equal(4m, result["ceil"]?.Value<decimal>());
    }

    // Type functions
    [Fact]
    public void IsNull_Should_Work()
    {
        var script = @"
            %set $.nullCheck = isNull(null);
            %set $.notNullCheck = isNull(""hello"");
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.True(result["nullCheck"]?.Value<bool>());
        Assert.False(result["notNullCheck"]?.Value<bool>());
    }

    [Fact]
    public void IsEmpty_Should_Work()
    {
        var script = @"
            %set $.emptyString = isEmpty("""");
            %set $.emptyArray = isEmpty(arr());
            %set $.nonEmpty = isEmpty(""hello"");
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.True(result["emptyString"]?.Value<bool>());
        Assert.True(result["emptyArray"]?.Value<bool>());
        Assert.False(result["nonEmpty"]?.Value<bool>());
    }

    [Fact]
    public void TypeOf_Should_Work()
    {
        var script = @"
            %set $.t1 = typeOf(null);
            %set $.t2 = typeOf(true);
            %set $.t3 = typeOf(42);
            %set $.t4 = typeOf(""hello"");
            %set $.t5 = typeOf(arr());
            %set $.t6 = typeOf(obj());
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("null", result["t1"]?.Value<string>());
        Assert.Equal("boolean", result["t2"]?.Value<string>());
        Assert.Equal("number", result["t3"]?.Value<string>());
        Assert.Equal("string", result["t4"]?.Value<string>());
        Assert.Equal("array", result["t5"]?.Value<string>());
        Assert.Equal("object", result["t6"]?.Value<string>());
    }

    // Array functions
    [Fact]
    public void First_Last_Should_Work()
    {
        var script = @"
            %let a = arr(1, 2, 3);
            %set $.first = first(&a);
            %set $.last = last(&a);
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(1, result["first"]?.Value<int>());
        Assert.Equal(3, result["last"]?.Value<int>());
    }

    [Fact]
    public void Count_Should_Work()
    {
        var script = @"%set $.count = count(arr(1, 2, 3, 4, 5));";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(5, result["count"]?.Value<int>());
    }

    [Fact]
    public void Push_Should_Modify_Array()
    {
        var script = @"
            %let a = arr(1, 2);
            push(&a, 3);
            push(&a, 4);
            %set $.items = &a;
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        var items = result["items"] as JArray;
        Assert.NotNull(items);
        Assert.Equal(4, items.Count);
        Assert.Equal(4, items[3]?.Value<int>());
    }

    // Date functions
    [Fact]
    public void Now_Should_Return_DateTime()
    {
        var script = @"%set $.now = now();";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.NotNull(result["now"]);
    }

    [Fact]
    public void FormatDate_Should_Work()
    {
        var script = @"
            %let dt = parseDate(""2024-06-15T10:30:00Z"");
            %set $.formatted = formatDate(&dt, ""yyyy-MM-dd"");
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("2024-06-15", result["formatted"]?.Value<string>());
    }

    // Nested JSON functions
    [Fact]
    public void ExpandJson_Should_Expand_Json_String_At_Path()
    {
        var script = @"%set $.result = expandJson($in, ""$.data"");";
        var input = JObject.Parse("{\"data\": \"{\\\"name\\\": \\\"John\\\", \\\"age\\\": 30}\"}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("John", result["result"]?["data"]?["name"]?.Value<string>());
        Assert.Equal(30, result["result"]?["data"]?["age"]?.Value<int>());
    }

    [Fact]
    public void ExpandJson_Should_Handle_Deeply_Nested_Json_Strings()
    {
        // Test recursive expansion of nested JSON strings using JObject.Parse directly
        var innerObj = new JObject { ["nested"] = true };
        var outerObj = new JObject { ["inner"] = innerObj.ToString(Newtonsoft.Json.Formatting.None) };
        var inputObj = new JObject { ["payload"] = outerObj.ToString(Newtonsoft.Json.Formatting.None) };
        
        var script = @"%set $.result = expandJson($in, ""$.payload"");";
        
        var result = _jex.Execute(script, inputObj);
        
        // Should recursively expand both levels
        Assert.True(result["result"]?["payload"]?["inner"]?["nested"]?.Value<bool>());
    }

    [Fact]
    public void ExpandJsonAll_Should_Expand_All_Json_Strings()
    {
        var script = @"%set $.result = expandJsonAll($in);";
        var input = JObject.Parse("{\"user\": \"{\\\"name\\\": \\\"Alice\\\"}\", \"settings\": \"{\\\"theme\\\": \\\"dark\\\"}\"}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("Alice", result["result"]?["user"]?["name"]?.Value<string>());
        Assert.Equal("dark", result["result"]?["settings"]?["theme"]?.Value<string>());
    }

    [Fact]
    public void ExpandJsonAll_Should_Respect_MaxDepth()
    {
        // Create deeply nested JSON strings using JObject to ensure proper encoding
        // Level 4: {"level": 4}
        // Level 3: {"nested": "{\"level\":4}"}
        // Level 2: {"nested": "{\"nested\":\"{\\\"level\\\":4}\"}"}
        // Level 1: {"nested": "..."}
        // Input: {"data": "..."}
        var level4 = new JObject { ["level"] = 4 };
        var level3 = new JObject { ["nested"] = level4.ToString(Newtonsoft.Json.Formatting.None) };
        var level2 = new JObject { ["nested"] = level3.ToString(Newtonsoft.Json.Formatting.None) };
        var level1 = new JObject { ["nested"] = level2.ToString(Newtonsoft.Json.Formatting.None) };
        var inputObj = new JObject { ["data"] = level1.ToString(Newtonsoft.Json.Formatting.None) };
        
        // With maxDepth=2, we can parse 2 levels of JSON strings:
        // Parse 1: data -> level1 object
        // Parse 2: level1.nested -> level2 object
        // level2.nested should remain as a string
        var script = @"%set $.result = expandJsonAll($in, 2);";
        
        var result = _jex.Execute(script, inputObj);
        
        // level1 and level2 are parsed, level2.nested should be a string (not parsed)
        Assert.NotNull(result["result"]?["data"]?["nested"]?["nested"]);
        Assert.Equal(JTokenType.String, result["result"]?["data"]?["nested"]?["nested"]?.Type);
    }

    [Fact]
    public void ExpandJsonAll_Should_Handle_Arrays()
    {
        var script = @"%set $.result = expandJsonAll($in);";
        var input = JObject.Parse("{\"items\": [\"{\\\"id\\\": 1}\", \"{\\\"id\\\": 2}\"]}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(1, result["result"]?["items"]?[0]?["id"]?.Value<int>());
        Assert.Equal(2, result["result"]?["items"]?[1]?["id"]?.Value<int>());
    }

    [Fact]
    public void ExpandJson_Should_Preserve_NonJson_Strings()
    {
        var script = @"%set $.result = expandJsonAll($in);";
        var input = JObject.Parse("{\"message\": \"Hello World\", \"data\": \"{\\\"valid\\\": true}\"}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("Hello World", result["result"]?["message"]?.Value<string>());
        Assert.True(result["result"]?["data"]?["valid"]?.Value<bool>());
    }
}
