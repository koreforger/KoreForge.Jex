using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KoreForge.Jex;
using Newtonsoft.Json.Linq;

namespace KoreForge.Jex.Benchmarks;

/// <summary>
/// Benchmarks for JEX engine performance.
/// </summary>
[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class JexBenchmarks
{
    private Jex _jex = null!;
    private IJexProgram _simpleProgram = null!;
    private IJexProgram _complexProgram = null!;
    private IJexProgram _loopProgram = null!;
    private IJexProgram _functionProgram = null!;
    private IJexProgram _expandJsonProgram = null!;
    private IJexProgram _expandJsonAllProgram = null!;
    
    private JToken _simpleInput = null!;
    private JToken _complexInput = null!;
    private JToken _loopInput = null!;
    private JToken _nestedJsonInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        _jex = new Jex();
        
        // Simple assignment script
        _simpleProgram = _jex.Compile(@"
            %set $.name = $in.user.name;
            %set $.email = $in.user.email;
        ");
        
        // Complex transformation script
        _complexProgram = _jex.Compile(@"
            %set $.fullName = concat($in.firstName, "" "", $in.lastName);
            %set $.age = $in.age;
            %set $.isAdult = $in.age >= 18;
            %if ($in.age < 13) %then %do;
                %set $.category = ""child"";
            %else %do;
                %if ($in.age < 20) %then %do;
                    %set $.category = ""teen"";
                %else %do;
                    %set $.category = ""adult"";
                %end;
            %end;
            %set $.upperName = upper($.fullName);
            %set $.hasEmail = existsPath($in, ""$.email"");
            %if ($.hasEmail) %then %do;
                %set $.contact = $in.email;
            %else %do;
                %set $.contact = ""N/A"";
            %end;
        ");
        
        // Loop-heavy script
        _loopProgram = _jex.Compile(@"
            %set $.processedItems = [];
            %foreach item %in $in.items %do;
                %let processed = {
                    ""id"": &item.id,
                    ""value"": &item.value * 2,
                    ""label"": upper(&item.name)
                };
                push($.processedItems, &processed);
            %end;
            %set $.count = length($.processedItems);
        ");
        
        // Function-heavy script
        _functionProgram = _jex.Compile(@"
            %set $.strings.original = $in.text;
            %set $.strings.upper = upper($in.text);
            %set $.strings.lower = lower($in.text);
            %set $.strings.trimmed = trim($in.text);
            %set $.strings.length = length($in.text);
            %set $.strings.words = split($in.text, "" "");
            %set $.strings.joined = join($.strings.words, ""-"");
            %set $.math.rounded = round($in.number, 2);
            %set $.math.floored = floor($in.number);
            %set $.math.ceiled = ceil($in.number);
            %set $.math.absolute = abs($in.negative);
            %set $.json.path = jp1($in, ""$.nested.value"");
        ");
        
        // Nested JSON expansion script
        _expandJsonProgram = _jex.Compile(@"
            %set $.result = expandJson($in, ""$.payload"");
        ");
        
        _expandJsonAllProgram = _jex.Compile(@"
            %set $.result = expandJsonAll($in);
        ");
        
        // Simple input
        _simpleInput = JObject.Parse(@"{
            ""user"": {
                ""name"": ""John Doe"",
                ""email"": ""john@example.com""
            }
        }");
        
        // Complex input
        _complexInput = JObject.Parse(@"{
            ""firstName"": ""John"",
            ""lastName"": ""Doe"",
            ""age"": 25,
            ""email"": ""john@example.com""
        }");
        
        // Loop input
        _loopInput = JObject.Parse(@"{
            ""items"": [
                {""id"": 1, ""name"": ""item1"", ""value"": 10},
                {""id"": 2, ""name"": ""item2"", ""value"": 20},
                {""id"": 3, ""name"": ""item3"", ""value"": 30},
                {""id"": 4, ""name"": ""item4"", ""value"": 40},
                {""id"": 5, ""name"": ""item5"", ""value"": 50}
            ]
        }");
        
        // Nested JSON input - create properly escaped nested JSON strings
        var innerData = new JObject
        {
            ["name"] = "John",
            ["values"] = new JArray(1, 2, 3, 4, 5)
        };
        var payloadString = innerData.ToString(Newtonsoft.Json.Formatting.None);
        _nestedJsonInput = new JObject
        {
            ["payload"] = payloadString,
            ["metadata"] = new JObject
            {
                ["nested1"] = new JObject { ["key"] = "value1" }.ToString(Newtonsoft.Json.Formatting.None),
                ["nested2"] = new JObject { ["key"] = "value2" }.ToString(Newtonsoft.Json.Formatting.None)
            }.ToString(Newtonsoft.Json.Formatting.None)
        };
    }

    [Benchmark(Description = "Simple: Copy 2 fields")]
    public JToken SimpleAssignment()
    {
        return _simpleProgram.Execute(_simpleInput);
    }

    [Benchmark(Description = "Complex: Transform with conditions")]
    public JToken ComplexTransformation()
    {
        return _complexProgram.Execute(_complexInput);
    }

    [Benchmark(Description = "Loop: Process 5 items")]
    public JToken LoopProcessing()
    {
        return _loopProgram.Execute(_loopInput);
    }

    [Benchmark(Description = "Functions: String & math ops")]
    public JToken FunctionCalls()
    {
        return _functionProgram.Execute(JObject.Parse(@"{
            ""text"": ""  Hello World  "",
            ""number"": 3.14159,
            ""negative"": -42.5,
            ""nested"": { ""value"": ""found"" }
        }"));
    }

    [Benchmark(Description = "ExpandJson: Single field")]
    public JToken ExpandJsonSingleField()
    {
        return _expandJsonProgram.Execute(_nestedJsonInput);
    }

    [Benchmark(Description = "ExpandJsonAll: Entire document")]
    public JToken ExpandJsonAllDocument()
    {
        return _expandJsonAllProgram.Execute(_nestedJsonInput);
    }

    [Benchmark(Description = "Compile: Simple script")]
    public IJexProgram CompileSimpleScript()
    {
        return _jex.Compile(@"%set $.name = $in.name;");
    }

    [Benchmark(Description = "Compile: Complex script")]
    public IJexProgram CompileComplexScript()
    {
        return _jex.Compile(@"
            %set $.fullName = concat($in.firstName, "" "", $in.lastName);
            %set $.results = [];
            %foreach item %in $in.items %do;
                push($.results, upper(&item.name));
            %end;
            %if ($in.enabled) %then %do;
                %set $.status = ""active"";
            %end;
        ");
    }
}

/// <summary>
/// Benchmarks for comparing different approaches to nested JSON handling.
/// </summary>
[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class NestedJsonBenchmarks
{
    private Jex _jex = null!;
    private JToken _shallowNestedInput = null!;
    private JToken _deepNestedInput = null!;
    private IJexProgram _expandAll = null!;
    private IJexProgram _expandDepth2 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _jex = new Jex();
        
        // Shallow nested: 1 level of JSON strings
        var innerObj = new JObject { ["data"] = "value" };
        _shallowNestedInput = new JObject
        {
            ["field1"] = innerObj.ToString(Newtonsoft.Json.Formatting.None),
            ["field2"] = innerObj.ToString(Newtonsoft.Json.Formatting.None),
            ["field3"] = innerObj.ToString(Newtonsoft.Json.Formatting.None)
        };
        
        // Deep nested: 3 levels of JSON strings
        var level3 = new JObject { ["level"] = 3 };
        var level2 = new JObject { ["nested"] = level3.ToString(Newtonsoft.Json.Formatting.None) };
        var level1 = new JObject { ["nested"] = level2.ToString(Newtonsoft.Json.Formatting.None) };
        _deepNestedInput = new JObject
        {
            ["deep"] = level1.ToString(Newtonsoft.Json.Formatting.None)
        };
        
        _expandAll = _jex.Compile(@"%set $.result = expandJsonAll($in);");
        _expandDepth2 = _jex.Compile(@"%set $.result = expandJsonAll($in, 2);");
    }

    [Benchmark(Description = "Shallow (1 level, 3 fields)")]
    public JToken ShallowNestedExpand()
    {
        return _expandAll.Execute(_shallowNestedInput);
    }

    [Benchmark(Description = "Deep (3 levels)")]
    public JToken DeepNestedExpand()
    {
        return _expandAll.Execute(_deepNestedInput);
    }

    [Benchmark(Description = "Deep with maxDepth=2")]
    public JToken DeepNestedExpandLimited()
    {
        return _expandDepth2.Execute(_deepNestedInput);
    }
}
