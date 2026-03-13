# KoreForge.Jex

**KoreForge.Jex** is a high-performance JSON Expression language engine for .NET that enables runtime JSON transformation using a simple, expressive scripting syntax. It's designed for scenarios where you need to dynamically transform JSON documents without recompiling your application.

## Features

- **Declarative Transformations**: Transform JSON documents using intuitive `%set` statements with dot-notation paths
- **Control Flow**: Full support for conditionals (`%if/%then/%else`) and loops (`%foreach`)
- **Rich Standard Library**: Built-in functions for strings, math, arrays, dates, JSON operations, and more
- **Custom Functions**: Register your own C# functions for domain-specific transformations
- **Nested JSON Expansion**: Automatically parse escaped JSON strings embedded within documents
- **JSONPath Support**: Query JSON using JSONPath expressions (`jp1`, `jpn`)
- **Type Safety**: Strong error reporting with source locations for debugging
- **High Performance**: Sub-microsecond execution for simple transformations

## Installation

```bash
dotnet add package KoreForge.Jex
```

## Quick Start

```csharp
using KoreForge;
using Newtonsoft.Json.Linq;

// Create a JEX instance
var jex = new Jex();

// Compile a transformation script
var program = jex.Compile(@"
    %set $.fullName = concat($in.firstName, "" "", $in.lastName);
    %set $.isAdult = $in.age >= 18;
");

// Execute on JSON input
var input = JObject.Parse(@"{
    ""firstName"": ""John"",
    ""lastName"": ""Doe"",
    ""age"": 25
}");

var result = program.Execute(input);
// Result: { "fullName": "John Doe", "isAdult": true }
```

## Syntax Overview

### Variable Scopes

| Prefix | Description |
|--------|-------------|
| `$in` | Read-only input document |
| `$` or `$out` | Output document being constructed |
| `$meta` | Optional metadata document |
| `&var` | Local variables (let bindings) |

### Statements

```jex
// Set a value in the output
%set $.name = $in.name;

// Create a local variable
%let temp = $in.value * 2;

// Conditional logic
%if ($in.age >= 18) %then %do;
    %set $.status = "adult";
%else %do;
    %set $.status = "minor";
%end;

// Loop over arrays
%foreach item %in $in.items %do;
    push($.processed, upper(&item.name));
%end;
```

### Object Literals

```jex
%set $.person = {
    "name": $in.firstName,
    "age": $in.age,
    "tags": ["user", "active"]
};
```

## Built-in Functions

### String Functions
| Function | Description |
|----------|-------------|
| `trim(s)` | Remove leading/trailing whitespace |
| `upper(s)` | Convert to uppercase |
| `lower(s)` | Convert to lowercase |
| `concat(s1, s2, ...)` | Concatenate strings |
| `length(s)` | Get string length |
| `split(s, delimiter)` | Split string into array |
| `join(array, delimiter)` | Join array elements with delimiter |
| `substr(s, start, length?)` | Extract substring |
| `replace(s, find, replacement)` | Replace occurrences |
| `regexMatch(s, pattern)` | Test regex match |
| `regexReplace(s, pattern, replacement)` | Regex replace |

### Math Functions
| Function | Description |
|----------|-------------|
| `round(n, decimals?)` | Round number |
| `floor(n)` | Round down |
| `ceil(n)` | Round up |
| `abs(n)` | Absolute value |
| `min(a, b, ...)` | Minimum value |
| `max(a, b, ...)` | Maximum value |

### Array Functions
| Function | Description |
|----------|-------------|
| `length(array)` | Get array length |
| `push(array, item)` | Add item to array |
| `first(array)` | Get first element |
| `last(array)` | Get last element |
| `contains(array, item)` | Check if array contains item |
| `filter(array, path, value)` | Filter array by property |
| `map(array, path)` | Extract property from each element |
| `sort(array, path?)` | Sort array |
| `distinct(array)` | Remove duplicates |
| `flatten(array)` | Flatten nested arrays |

### JSON Functions
| Function | Description |
|----------|-------------|
| `jp1(doc, path)` | JSONPath query, returns first match |
| `jpn(doc, path)` | JSONPath query, returns all matches |
| `expandJson(doc, path)` | Parse nested JSON string at path |
| `expandJsonAll(doc, maxDepth?)` | Recursively parse all nested JSON strings |
| `toJson(value)` | Convert to JSON string |
| `parseJson(string)` | Parse JSON string |

### Date Functions
| Function | Description |
|----------|-------------|
| `now()` | Current UTC timestamp (ISO 8601) |
| `formatDate(date, format)` | Format date string |
| `parseDate(string, format?)` | Parse date string |
| `addDays(date, n)` | Add days to date |

### Utility Functions
| Function | Description |
|----------|-------------|
| `guid()` | Generate new GUID |
| `coalesce(a, b, ...)` | Return first non-null value |
| `iif(condition, trueValue, falseValue)` | Inline conditional |
| `typeof(value)` | Get type name |
| `isNull(value)` | Check if null |
| `throw(message)` | Throw runtime error |

## Custom Functions

Register custom functions to extend JEX with your own logic:

```csharp
var jex = new Jex();

// Register a function that returns a value
jex.RegisterFunction("calculateTax", (amount, rate) => 
    amount.AsNumber() * rate.AsNumber());

// Register a void function (for side effects like push)
jex.RegisterVoidFunction("log", (context, args) => 
    Console.WriteLine(args[0].AsString()));

var program = jex.Compile(@"
    %set $.tax = calculateTax($in.price, 0.08);
");
```

## Nested JSON Expansion

JEX can automatically parse JSON strings embedded within your documents:

```csharp
var input = JObject.Parse(@"{
    ""payload"": ""{\""name\"":\""John\"",\""age\"":30}""
}");

// Expand a specific field
var program = jex.Compile(@"
    %set $.data = expandJson($in, ""$.payload"");
");
// Result: { "data": { "name": "John", "age": 30 } }

// Or expand all nested JSON strings recursively
var programAll = jex.Compile(@"
    %set $ = expandJsonAll($in);
");
```

## Performance

Benchmark results on .NET 10.0.1 (13th Gen Intel Core i7-13650HX, Windows 11):

| Operation | Mean | Error | Allocated |
|-----------|------|-------|-----------|
| Simple: Copy 2 fields | 348.2 ns | ±6.22 ns | 1.47 KB |
| Complex: Transform with conditions | 1,540.1 ns | ±24.16 ns | 4.83 KB |
| Loop: Process 5 items | 3,005.8 ns | ±15.09 ns | 9.2 KB |
| Functions: String & math ops | 4,733.1 ns | ±93.89 ns | 17.13 KB |
| ExpandJson: Single field | 1,601.9 ns | ±11.01 ns | 7.95 KB |
| ExpandJsonAll: Entire document | 3,527.3 ns | ±62.96 ns | 22 KB |
| Compile: Simple script | 812.2 ns | ±8.81 ns | 1.27 KB |
| Compile: Complex script | 5,962.3 ns | ±30.78 ns | 6.38 KB |

Run benchmarks yourself:
```bash
cd KoreForge.Jex
.\bin\build-benchmark.ps1
```

## Error Handling

JEX provides detailed error messages with source locations:

```csharp
try
{
    var program = jex.Compile(@"
        %set $.value = unknownFunction($in.x);
    ");
}
catch (JexCompileException ex)
{
    // "Unknown function 'unknownFunction' at (2,23)-(2,38)"
    Console.WriteLine(ex.Message);
}
```

Runtime errors include execution context:

```csharp
try
{
    program.Execute(input);
}
catch (JexRuntimeException ex)
{
    // "Division by zero at (5,15)-(5,25)"
    Console.WriteLine(ex.Message);
}
```

## Libraries

JEX supports reusable function libraries:

```csharp
// Define a library with custom functions
var librarySource = @"
    %function greet(name)
        %return concat(""Hello, "", &name, ""!"");
    %end;
";

var library = jex.CompileLibrary("MyLib", librarySource);

// Use in programs
var program = jex.Compile(@"
    %set $.greeting = MyLib.greet($in.name);
", library);
```

## Requirements

- .NET 10.0 or later
- Newtonsoft.Json 13.0.4+

## Related Packages

This package is part of the KoreForge suite of libraries:

- `KoreForge.Pipeline` - Chainable middleware pipeline for processing
- `KoreForge.Flow` - State-machine workflow orchestration
- `KoreForge.Kafka` - Kafka messaging integration

## License

MIT License - see [LICENSE.md](LICENSE.md) for details.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.
