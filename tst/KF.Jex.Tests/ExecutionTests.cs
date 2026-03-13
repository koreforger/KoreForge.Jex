using Xunit;
using KoreForge.Jex;

using Newtonsoft.Json.Linq;

namespace KoreForge.Jex.Tests;

public class ExecutionTests
{
    private readonly Jex _jex = new();

    [Fact]
    public void Execute_Should_Set_Output_Value()
    {
        var script = @"
            %set $.name = ""John"";
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("John", result["name"]?.Value<string>());
    }

    [Fact]
    public void Execute_Should_Use_Variable()
    {
        var script = @"
            %let name = ""Alice"";
            %set $.greeting = ""Hello &name"";
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("Hello Alice", result["greeting"]?.Value<string>());
    }

    [Fact]
    public void Execute_Should_Extract_From_Input()
    {
        var script = @"
            %let userId = jp1($in, ""$.user.id"");
            %set $.extractedId = &userId;
        ";
        var input = JObject.Parse("{\"user\": {\"id\": 12345}}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(12345, result["extractedId"]?.Value<int>());
    }

    [Fact]
    public void Execute_Should_Handle_If_Condition()
    {
        var script = @"
            %let value = 10;
            %if (&value > 5) %then %do;
                %set $.result = ""big"";
            %end;
            %else %do;
                %set $.result = ""small"";
            %end;
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("big", result["result"]?.Value<string>());
    }

    [Fact]
    public void Execute_Should_Handle_Foreach_Loop()
    {
        var script = @"
            %let items = arr();
            %foreach num %in jpAll($in, ""$.numbers[*]"") %do;
                push(&items, &num);
            %end;
            %set $.collected = &items;
        ";
        var input = JObject.Parse("{\"numbers\": [1, 2, 3, 4, 5]}");
        
        var result = _jex.Execute(script, input);
        
        var collected = result["collected"] as JArray;
        Assert.NotNull(collected);
        Assert.Equal(5, collected.Count);
    }

    [Fact]
    public void Execute_Should_Handle_Math_Operations()
    {
        var script = @"
            %let a = 10;
            %let b = 3;
            %set $.sum = &a + &b;
            %set $.diff = &a - &b;
            %set $.prod = &a * &b;
            %set $.quot = &a / &b;
            %set $.mod = &a % &b;
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(13m, result["sum"]?.Value<decimal>());
        Assert.Equal(7m, result["diff"]?.Value<decimal>());
        Assert.Equal(30m, result["prod"]?.Value<decimal>());
        Assert.True(result["quot"]?.Value<decimal>() > 3.3m && result["quot"]?.Value<decimal>() < 3.4m);
        Assert.Equal(1m, result["mod"]?.Value<decimal>());
    }

    [Fact]
    public void Execute_Should_Handle_String_Operations()
    {
        var script = @"
            %let s = ""  Hello World  "";
            %set $.trimmed = trim(&s);
            %set $.upper = upper(&s);
            %set $.lower = lower(&s);
            %set $.left = left(&s, 5);
            %set $.sub = substr(trim(&s), 0, 5);
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("Hello World", result["trimmed"]?.Value<string>());
        Assert.Equal("  HELLO WORLD  ", result["upper"]?.Value<string>());
        Assert.Equal("  hello world  ", result["lower"]?.Value<string>());
        Assert.Equal("  Hel", result["left"]?.Value<string>());
        Assert.Equal("Hello", result["sub"]?.Value<string>());
    }

    [Fact]
    public void Execute_Should_Handle_Split()
    {
        var script = @"
            %let s = ""a|b|c"";
            %let parts = split(&s, ""|"");
            %set $.first = jp1(&parts, ""$[0]"");
            %set $.count = count(&parts);
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("a", result["first"]?.Value<string>());
        Assert.Equal(3, result["count"]?.Value<int>());
    }

    [Fact]
    public void Execute_Should_Handle_CoalescePath()
    {
        var script = @"
            %let id = coalescePath($in, ""$.userId"", ""$.user.id"", ""$.id"");
            %set $.foundId = &id;
        ";
        var input = JObject.Parse("{\"user\": {\"id\": 999}}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(999, result["foundId"]?.Value<int>());
    }

    [Fact]
    public void Execute_Should_Handle_IndexBy_And_Lookup()
    {
        var script = @"
            %let items = jpAll($in, ""$.items[*]"");
            %let index = indexBy(&items, ""id"");
            %let found = lookup(&index, ""b"");
            %set $.foundValue = jp1(&found, ""$.value"");
        ";
        var input = JObject.Parse("{\"items\": [{\"id\": \"a\", \"value\": 1}, {\"id\": \"b\", \"value\": 2}]}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(2, result["foundValue"]?.Value<int>());
    }

    [Fact]
    public void Execute_Should_Handle_Break_In_Loop()
    {
        var script = @"
            %let count = 0;
            %foreach num %in jpAll($in, ""$.numbers[*]"") %do;
                %let count = &count + 1;
                %if (&num == 3) %then %do;
                    %break;
                %end;
            %end;
            %set $.iterations = &count;
        ";
        var input = JObject.Parse("{\"numbers\": [1, 2, 3, 4, 5]}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(3m, result["iterations"]?.Value<decimal>());
    }

    [Fact]
    public void Execute_Should_Handle_Continue_In_Loop()
    {
        var script = @"
            %let sum = 0;
            %foreach num %in jpAll($in, ""$.numbers[*]"") %do;
                %if (&num == 3) %then %do;
                    %continue;
                %end;
                %let sum = &sum + &num;
            %end;
            %set $.sum = &sum;
        ";
        var input = JObject.Parse("{\"numbers\": [1, 2, 3, 4, 5]}");
        
        var result = _jex.Execute(script, input);
        
        // 1 + 2 + 4 + 5 = 12 (skipping 3)
        Assert.Equal(12m, result["sum"]?.Value<decimal>());
    }

    [Fact]
    public void Execute_Should_Handle_Do_Loop()
    {
        var script = @"
            %let sum = 0;
            %do i = 1 %to 5;
                %let sum = &sum + &i;
            %end;
            %set $.sum = &sum;
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(15m, result["sum"]?.Value<decimal>());
    }

    [Fact]
    public void Execute_Should_Handle_Obj_Function()
    {
        var script = @"
            %let person = obj(""name"", ""John"", ""age"", 30);
            %set $.person = &person;
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("John", result["person"]?["name"]?.Value<string>());
        Assert.Equal(30, result["person"]?["age"]?.Value<int>());
    }

    [Fact]
    public void Execute_Should_Handle_Arr_Function()
    {
        var script = @"
            %let items = arr(1, 2, 3);
            %set $.items = &items;
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        var items = result["items"] as JArray;
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
        Assert.Equal(1, items[0]?.Value<int>());
    }

    [Fact]
    public void Execute_Should_Enforce_Loop_Limit()
    {
        var script = @"
            %do i = 1 %to 1000000;
                %let x = &i;
            %end;
        ";
        var input = JObject.Parse("{}");
        var options = new JexExecutionOptions { MaxLoopIterations = 100 };
        
        var program = _jex.Compile(script);
        Assert.Throws<JexLimitExceededException>(() => program.Execute(input, options));
    }

    [Fact]
    public void Execute_Should_Handle_Nested_Objects()
    {
        var script = @"
            %set $.level1.level2.level3 = ""deep"";
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("deep", result["level1"]?["level2"]?["level3"]?.Value<string>());
    }

    [Fact]
    public void Execute_Should_Handle_Boolean_Operations()
    {
        var script = @"
            %let a = true;
            %let b = false;
            %set $.and = &a && &b;
            %set $.or = &a || &b;
            %set $.not = !&a;
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.False(result["and"]?.Value<bool>());
        Assert.True(result["or"]?.Value<bool>());
        Assert.False(result["not"]?.Value<bool>());
    }

    [Fact]
    public void Compile_Should_Be_Reusable()
    {
        var script = @"
            %set $.doubled = jp1($in, ""$.value"") * 2;
        ";
        var program = _jex.Compile(script);
        
        var result1 = program.Execute(JObject.Parse("{\"value\": 5}"));
        var result2 = program.Execute(JObject.Parse("{\"value\": 10}"));
        
        Assert.Equal(10m, result1["doubled"]?.Value<decimal>());
        Assert.Equal(20m, result2["doubled"]?.Value<decimal>());
    }
}
