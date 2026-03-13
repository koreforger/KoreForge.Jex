using Xunit;
using KoreForge.Jex;

using KoreForge.Jex.Library;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

namespace KoreForge.Jex.Tests;

public class FunctionTests
{
    private readonly Jex _jex = new();

    // ========== User-Defined Functions ==========

    [Fact]
    public void UserFunction_Should_Return_Value()
    {
        var script = @"
            %func double(x);
                %return &x * 2;
            %endfunc;

            %let result = double(21);
            %set $.result = &result;
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(42m, result["result"]?.Value<decimal>());
    }

    [Fact]
    public void UserFunction_Should_Work_With_Multiple_Params()
    {
        var script = @"
            %func add(a, b, c);
                %return &a + &b + &c;
            %endfunc;

            %set $.sum = add(1, 2, 3);
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(6m, result["sum"]?.Value<decimal>());
    }

    [Fact]
    public void UserFunction_Should_Work_With_No_Params()
    {
        var script = @"
            %func getAnswer();
                %return 42;
            %endfunc;

            %set $.answer = getAnswer();
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(42m, result["answer"]?.Value<decimal>());
    }

    [Fact]
    public void UserFunction_Void_Should_Modify_Output()
    {
        var script = @"
            %func setGreeting();
                %set $.greeting = ""Hello, World!"";
            %endfunc;

            setGreeting();
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("Hello, World!", result["greeting"]?.Value<string>());
    }

    [Fact]
    public void UserFunction_Should_Access_Input()
    {
        var script = @"
            %func extractName();
                %return jp1($in, ""$.user.name"");
            %endfunc;

            %set $.extracted = extractName();
        ";
        var input = JObject.Parse("{\"user\": {\"name\": \"Alice\"}}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("Alice", result["extracted"]?.Value<string>());
    }

    [Fact]
    public void UserFunction_Should_Return_Object()
    {
        var script = @"
            %func createPerson(name, age);
                %return obj(""name"", &name, ""age"", &age);
            %endfunc;

            %set $.person = createPerson(""Bob"", 30);
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal("Bob", result["person"]?["name"]?.Value<string>());
        Assert.Equal(30, result["person"]?["age"]?.Value<int>());
    }

    [Fact]
    public void UserFunction_Should_Return_Array()
    {
        var script = @"
            %func createRange(start, end);
                %let result = arr();
                %do i = &start %to &end;
                    push(&result, &i);
                %end;
                %return &result;
            %endfunc;

            %set $.range = createRange(1, 5);
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        var range = result["range"] as JArray;
        Assert.NotNull(range);
        Assert.Equal(5, range!.Count);
        Assert.Equal(1, range[0]?.Value<int>());
        Assert.Equal(5, range[4]?.Value<int>());
    }

    [Fact]
    public void UserFunction_Recursive_Should_Work()
    {
        var script = @"
            %func factorial(n);
                %if (&n <= 1) %then %do;
                    %return 1;
                %end;
                %return &n * factorial(&n - 1);
            %endfunc;

            %set $.result = factorial(5);
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(120m, result["result"]?.Value<decimal>());
    }

    [Fact]
    public void UserFunction_Should_Access_Outer_Scope()
    {
        var script = @"
            %let multiplier = 10;
            
            %func applyMultiplier(x);
                %return &x * &multiplier;
            %endfunc;

            %set $.result = applyMultiplier(5);
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        // Function can read from outer scope
        Assert.Equal(50m, result["result"]?.Value<decimal>());
    }

    [Fact]
    public void UserFunction_Parameters_Should_Be_Usable()
    {
        var script = @"
            %func getDoubled(x);
                %return &x * 2;
            %endfunc;

            %set $.result = getDoubled(21);
        ";
        var input = JObject.Parse("{}");
        
        var result = _jex.Execute(script, input);
        
        Assert.Equal(42m, result["result"]?.Value<decimal>());
    }

    // ========== Host-Registered Functions ==========

    [Fact]
    public void RegisteredFunction_Should_Return_Value()
    {
        var jex = new Jex();
        jex.RegisterFunction("customAdd", (ctx, args) =>
        {
            var a = args[0].AsNumber();
            var b = args[1].AsNumber();
            return JexValue.FromNumber(a + b);
        }, minArgs: 2, maxArgs: 2);

        var script = @"%set $.result = customAdd(10, 32);";
        var input = JObject.Parse("{}");
        
        var result = jex.Execute(script, input);
        
        Assert.Equal(42m, result["result"]?.Value<decimal>());
    }

    [Fact]
    public void RegisteredVoidFunction_Should_Modify_Output()
    {
        var jex = new Jex();
        jex.RegisterVoidFunction("setTimestamp", (ctx, args) =>
        {
            var output = ctx.Output as JObject;
            output!["timestamp"] = "2024-01-27T12:00:00Z";
        });

        var script = @"
            setTimestamp();
            %set $.data = ""test"";
        ";
        var input = JObject.Parse("{}");
        
        var result = jex.Execute(script, input);
        
        Assert.Equal("2024-01-27T12:00:00Z", result["timestamp"]?.Value<string>());
        Assert.Equal("test", result["data"]?.Value<string>());
    }

    [Fact]
    public void RegisteredFunction_Should_Access_Context()
    {
        var jex = new Jex();
        jex.RegisterFunction("getInputField", (ctx, args) =>
        {
            var path = args[0].AsString();
            var token = ctx.Input.SelectToken(path);
            return token != null ? JexValue.FromJson(token) : JexValue.Null;
        }, minArgs: 1, maxArgs: 1);

        var script = @"%set $.name = getInputField(""user.name"");";
        var input = JObject.Parse("{\"user\": {\"name\": \"Charlie\"}}");
        
        var result = jex.Execute(script, input);
        
        Assert.Equal("Charlie", result["name"]?.Value<string>());
    }
}

public class LibraryTests
{
    // ========== Library Loading ==========

    [Fact]
    public void Library_Should_Load_From_Source()
    {
        var jex = new Jex();
        
        var librarySource = @"
            %func triple(x);
                %return &x * 3;
            %endfunc;

            %func square(x);
                %return &x * &x;
            %endfunc;
        ";
        
        var library = jex.LoadLibraryFromSource("MathLib", librarySource);
        
        Assert.Equal("MathLib", library.Name);
        Assert.Contains("triple", library.FunctionNames);
        Assert.Contains("square", library.FunctionNames);
    }

    [Fact]
    public void Library_Should_Load_From_Stream()
    {
        var jex = new Jex();
        
        var librarySource = @"
            %func greet(name);
                %return concat(""Hello, "", &name, ""!"");
            %endfunc;
        ";
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(librarySource));
        var library = jex.LoadLibrary("Greetings", stream);
        
        Assert.Equal("Greetings", library.Name);
        Assert.Contains("greet", library.FunctionNames);
    }

    [Fact]
    public void Library_Functions_Should_Be_Callable()
    {
        var jex = new Jex();
        
        var librarySource = @"
            %func double(x);
                %return &x * 2;
            %endfunc;

            %func addTen(x);
                %return &x + 10;
            %endfunc;
        ";
        jex.LoadLibraryFromSource("MathOps", librarySource);

        var script = @"
            %let a = double(5);
            %let b = addTen(&a);
            %set $.result = &b;
        ";
        var input = JObject.Parse("{}");
        
        var result = jex.Execute(script, input);
        
        Assert.Equal(20m, result["result"]?.Value<decimal>());
    }

    [Fact]
    public void Library_Functions_Can_Call_Each_Other()
    {
        var jex = new Jex();
        
        var librarySource = @"
            %func square(x);
                %return &x * &x;
            %endfunc;

            %func sumOfSquares(a, b);
                %return square(&a) + square(&b);
            %endfunc;
        ";
        jex.LoadLibraryFromSource("MathLib", librarySource);

        var script = @"%set $.result = sumOfSquares(3, 4);";
        var input = JObject.Parse("{}");
        
        var result = jex.Execute(script, input);
        
        Assert.Equal(25m, result["result"]?.Value<decimal>()); // 9 + 16 = 25
    }

    [Fact]
    public void Multiple_Libraries_Should_Work()
    {
        var jex = new Jex();
        
        var mathLib = @"
            %func multiply(a, b);
                %return &a * &b;
            %endfunc;
        ";
        
        var stringLib = @"
            %func shout(text);
                %return upper(&text);
            %endfunc;
        ";
        
        jex.LoadLibraryFromSource("MathLib", mathLib);
        jex.LoadLibraryFromSource("StringLib", stringLib);

        var script = @"
            %set $.number = multiply(6, 7);
            %set $.text = shout(""hello"");
        ";
        var input = JObject.Parse("{}");
        
        var result = jex.Execute(script, input);
        
        Assert.Equal(42m, result["number"]?.Value<decimal>());
        Assert.Equal("HELLO", result["text"]?.Value<string>());
    }

    [Fact]
    public void Script_Functions_Override_Library_Functions()
    {
        var jex = new Jex();
        
        var librarySource = @"
            %func getValue();
                %return ""library"";
            %endfunc;
        ";
        jex.LoadLibraryFromSource("Lib", librarySource);

        var script = @"
            %func getValue();
                %return ""script"";
            %endfunc;

            %set $.result = getValue();
        ";
        var input = JObject.Parse("{}");
        
        var result = jex.Execute(script, input);
        
        // Script-defined function should take precedence
        Assert.Equal("script", result["result"]?.Value<string>());
    }

    [Fact]
    public void Library_Should_Only_Accept_Functions()
    {
        var jex = new Jex();
        
        var invalidSource = @"
            %let x = 5;
            %func myFunc();
                %return 1;
            %endfunc;
        ";
        
        Assert.Throws<JexCompileException>(() => jex.LoadLibraryFromSource("Invalid", invalidSource));
    }

    [Fact]
    public void Library_Should_Require_At_Least_One_Function()
    {
        var jex = new Jex();
        
        var emptySource = @"
            // Just a comment, no functions
        ";
        
        Assert.Throws<JexCompileException>(() => jex.LoadLibraryFromSource("Empty", emptySource));
    }
}
