# JEX Language User Guide

## Introduction

JEX (JSON Expression eXecution) is a domain-specific language (DSL) designed for transforming JSON documents. Inspired by SAS macro language, JEX provides a familiar syntax for developers who need to manipulate, transform, and extract data from JSON structures.

JEX is ideal for:
- **ETL processes** - Extract, transform, and load JSON data
- **API response transformation** - Normalize and reshape API responses
- **Data mapping** - Convert between different JSON schemas
- **Report generation** - Aggregate and summarize JSON data

## Quick Start

### Basic Example

```csharp
using JexEngine;
using Newtonsoft.Json.Linq;

// Create a JEX engine instance
var jex = new Jex();

// Your transformation script
var script = @"
    %let name = jp1($in, ""$.user.name"");
    %set $.greeting = concat(""Hello, "", &name, ""!"");
";

// Input JSON
var input = JObject.Parse(@"{ ""user"": { ""name"": ""Alice"" } }");

// Execute and get result
var result = jex.Execute(script, input);
// result: { "greeting": "Hello, Alice!" }
```

## Language Basics

### Variables

Variables store values temporarily during script execution. Use `%let` to declare or update a variable, and `&` to reference its value.

```jex
// Declare a variable
%let count = 10;

// Use the variable (prefix with &)
%let doubled = &count * 2;

// Update the variable
%let count = &count + 1;
```

### Special Variables

| Variable | Description |
|----------|-------------|
| `$in` | The input JSON document (read-only) |
| `$out` | The output JSON document being built |
| `$meta` | Optional metadata passed to the script |

### Setting Output Values

Use `%set` to write values to the output document:

```jex
// Set a simple value
%set $.name = "John";

// Set a nested value (creates intermediate objects automatically)
%set $.user.profile.age = 30;

// Set using a variable
%let score = 95;
%set $.result.score = &score;
```

### Data Types

JEX supports these data types:
- **String** - `"Hello"` or `'Hello'`
- **Number** - `42`, `3.14`, `-10`
- **Boolean** - `true`, `false`
- **Null** - `null`
- **Array** - Created with `arr()` function
- **Object** - Created with `obj()` function

## JSONPath Functions

### jp1 - Get Single Value

Extracts a single value using JSONPath:

```jex
// Get a simple property
%let name = jp1($in, "$.user.name");

// Get a nested property
%let city = jp1($in, "$.address.city");

// Get from an array by index
%let first = jp1($in, "$.items[0]");
```

### jpAll - Get Multiple Values

Extracts all matching values as an array:

```jex
// Get all items from an array
%let items = jpAll($in, "$.orders[*]");

// Get all prices
%let prices = jpAll($in, "$.products[*].price");
```

### coalescePath - First Non-Null Value

Returns the first non-null value from multiple paths:

```jex
%let phone = coalescePath($in, "$.mobile", "$.home", "$.work");
```

### existsPath - Check Path Exists

Returns true if a path exists and has a value:

```jex
%if (existsPath($in, "$.user.email")) %then %do;
    %set $.hasEmail = true;
%end;
```

## Control Flow

### If-Then-Else

```jex
%if (&age >= 18) %then %do;
    %set $.status = "adult";
%end;
%else %do;
    %set $.status = "minor";
%end;
```

### If-Then (without Else)

```jex
%if (&discount > 0) %then %do;
    %let price = &price - &discount;
%end;
```

### Do Loop (Counted)

Repeats a block a specific number of times:

```jex
%let sum = 0;
%do i = 1 %to 10;
    %let sum = &sum + &i;
%end;
// sum = 55
```

With step value:

```jex
%do i = 0 %to 100 %by 10;
    // i = 0, 10, 20, ..., 100
%end;
```

### Foreach Loop

Iterates over array elements:

```jex
%let items = jpAll($in, "$.products[*]");
%let total = 0;

%foreach item %in &items %do;
    %let price = jp1(&item, "$.price");
    %let total = &total + &price;
%end;
```

### Do While Loop

Repeats while a condition is true:

```jex
%let n = 10;
%do %while (&n > 0);
    %let n = &n - 1;
%end;
```

### Do Until Loop

Repeats until a condition becomes true:

```jex
%let n = 0;
%do %until (&n >= 10);
    %let n = &n + 1;
%end;
```

## Functions

### User-Defined Functions

Create reusable functions with `%func`:

```jex
// Function that returns a value
%func double(x);
    %return &x * 2;
%endfunc;

// Function with multiple parameters
%func add(a, b);
    %return &a + &b;
%endfunc;

// Use the functions
%let result = add(double(5), 3);  // result = 13
```

### Recursive Functions

Functions can call themselves:

```jex
%func factorial(n);
    %if (&n <= 1) %then %do;
        %return 1;
    %end;
    %return &n * factorial(&n - 1);
%endfunc;

%let result = factorial(5);  // result = 120
```

### Void Functions (Modifying Output)

Functions without a return value can modify `$out`:

```jex
%func setMetadata();
    %set $.metadata.timestamp = now();
    %set $.metadata.version = "1.0";
%endfunc;

setMetadata();  // Adds metadata to output
```

## Built-in Functions Reference

### String Functions

| Function | Description | Example |
|----------|-------------|---------|
| `concat(...)` | Join strings | `concat("Hello", " ", "World")` → `"Hello World"` |
| `length(s)` | String or array length | `length("test")` → `4` |
| `upper(s)` | Uppercase | `upper("hello")` → `"HELLO"` |
| `lower(s)` | Lowercase | `lower("HELLO")` → `"hello"` |
| `trim(s)` | Remove whitespace | `trim("  hi  ")` → `"hi"` |
| `substr(s, start, len)` | Extract substring | `substr("hello", 0, 2)` → `"he"` |
| `left(s, n)` | Left n characters | `left("hello", 2)` → `"he"` |
| `right(s, n)` | Right n characters | `right("hello", 2)` → `"lo"` |
| `split(s, delim)` | Split into array | `split("a,b,c", ",")` → `["a","b","c"]` |
| `replace(s, old, new)` | Replace text | `replace("hello", "l", "L")` → `"heLLo"` |
| `regexMatch(s, pattern)` | Regex match | `regexMatch("test123", "\\d+")` → `"123"` |
| `regexReplace(s, pattern, repl)` | Regex replace | `regexReplace("a1b2", "\\d", "X")` → `"aXbX"` |

### Math Functions

| Function | Description | Example |
|----------|-------------|---------|
| `abs(n)` | Absolute value | `abs(-5)` → `5` |
| `round(n, decimals)` | Round number | `round(3.14159, 2)` → `3.14` |
| `floor(n)` | Round down | `floor(3.9)` → `3` |
| `ceil(n)` | Round up | `ceil(3.1)` → `4` |
| `min(a, b)` | Minimum | `min(5, 3)` → `3` |
| `max(a, b)` | Maximum | `max(5, 3)` → `5` |

### Date Functions

| Function | Description | Example |
|----------|-------------|---------|
| `now()` | Current date/time | `now()` → `"2024-01-27T12:00:00Z"` |
| `parseDate(s, format?)` | Parse date string | `parseDate("2024-01-27")` |
| `formatDate(dt, format)` | Format date | `formatDate(&dt, "yyyy-MM-dd")` |
| `dateAdd(dt, n, unit)` | Add to date | `dateAdd(&dt, 7, "days")` |
| `dateDiff(dt1, dt2, unit)` | Difference | `dateDiff(&d1, &d2, "days")` |

### Type Functions

| Function | Description | Example |
|----------|-------------|---------|
| `toString(v)` | Convert to string | `toString(42)` → `"42"` |
| `toNumber(v)` | Convert to number | `toNumber("42")` → `42` |
| `toBool(v)` | Convert to boolean | `toBool(1)` → `true` |
| `typeOf(v)` | Get type name | `typeOf("hi")` → `"string"` |
| `isNull(v)` | Check if null | `isNull(null)` → `true` |
| `isEmpty(v)` | Check if empty | `isEmpty("")` → `true` |

### Array/Object Functions

| Function | Description | Example |
|----------|-------------|---------|
| `arr(...)` | Create array | `arr(1, 2, 3)` → `[1,2,3]` |
| `obj(k1, v1, ...)` | Create object | `obj("a", 1, "b", 2)` → `{"a":1,"b":2}` |
| `push(arr, val)` | Add to array | `push(&list, "item")` |
| `first(arr)` | First element | `first(&items)` |
| `last(arr)` | Last element | `last(&items)` |
| `count(arr)` | Array length | `count(&items)` |
| `indexBy(arr, key)` | Create lookup | `indexBy(&users, "id")` |
| `lookup(idx, key)` | Lookup by key | `lookup(&index, "user1")` |

## Libraries

Libraries allow you to package reusable functions and share them across scripts.

### Creating a Library

A library contains only function definitions:

```jex
// MathUtils library
%func square(x);
    %return &x * &x;
%endfunc;

%func cube(x);
    %return &x * &x * &x;
%endfunc;

%func sumOfSquares(a, b);
    %return square(&a) + square(&b);
%endfunc;
```

### Loading Libraries

```csharp
var jex = new Jex();

// Load from a string
jex.LoadLibraryFromSource("MathUtils", librarySource);

// Load from a file
jex.LoadLibrary("c:/libs/string-utils.jex");

// Load from a stream
using var stream = File.OpenRead("my-lib.jex");
jex.LoadLibrary("MyLib", stream);
```

### Using Library Functions

Once loaded, library functions are available in all scripts:

```jex
// Library functions are called like built-in functions
%let result = sumOfSquares(3, 4);  // Uses MathUtils library
%set $.result = &result;  // 25
```

### Function Resolution Order

When calling a function, JEX searches in this order:
1. User-defined functions (in the current script)
2. Library functions
3. Built-in functions

This allows scripts to override library functions if needed.

## Host-Registered Functions

JEX can be extended with custom C# functions, enabling you to integrate domain-specific logic, call external services, or access host application data directly from JEX scripts.

### RegisterFunction - Functions That Return Values

Use `RegisterFunction` for functions that compute and return a value:

```csharp
var jex = new Jex();

// Simple function with fixed argument count
jex.RegisterFunction("customHash", (ctx, args) =>
{
    var input = args[0].AsString();
    var hash = ComputeMD5Hash(input);  // Your implementation
    return JexValue.FromString(hash);
}, minArgs: 1, maxArgs: 1);

// Function with variable arguments
jex.RegisterFunction("formatNumber", (ctx, args) =>
{
    var number = args[0].AsNumber();
    var decimals = args.Count > 1 ? (int)args[1].AsNumber() : 2;
    var formatted = number.ToString($"N{decimals}");
    return JexValue.FromString(formatted);
}, minArgs: 1, maxArgs: 2);

// Function returning JSON
jex.RegisterFunction("lookupUser", (ctx, args) =>
{
    var userId = args[0].AsString();
    var user = UserService.GetUser(userId);  // Your service
    return JexValue.FromJson(JObject.FromObject(user));
}, minArgs: 1, maxArgs: 1);

// Function accessing input/output context
jex.RegisterFunction("getInputField", (ctx, args) =>
{
    var fieldName = args[0].AsString();
    var token = ctx.Input.SelectToken($"$.{fieldName}");
    return JexValue.FromJson(token);
}, minArgs: 1, maxArgs: 1);
```

### RegisterVoidFunction - Functions That Modify State

Use `RegisterVoidFunction` for functions that modify the output document or perform side effects without returning a value:

```csharp
var jex = new Jex();

// Add audit information to output
jex.RegisterVoidFunction("addAudit", (ctx, args) =>
{
    var output = ctx.Output as JObject;
    output!["_audit"] = new JObject
    {
        ["timestamp"] = DateTime.UtcNow.ToString("o"),
        ["user"] = GetCurrentUser(),
        ["version"] = "1.0"
    };
});

// Log transformation for debugging
jex.RegisterVoidFunction("log", (ctx, args) =>
{
    var message = args[0].AsString();
    Logger.Info($"JEX: {message}");
}, minArgs: 1, maxArgs: 1);

// Copy a section from input to output
jex.RegisterVoidFunction("copySection", (ctx, args) =>
{
    var sectionPath = args[0].AsString();
    var section = ctx.Input.SelectToken(sectionPath);
    if (section != null)
    {
        var output = ctx.Output as JObject;
        output![sectionPath.Split('.').Last()] = section.DeepClone();
    }
}, minArgs: 1, maxArgs: 1);
```

### Method Signatures

```csharp
// RegisterFunction - for functions that return a value
public void RegisterFunction(
    string name,                                                    // Function name to use in scripts
    Func<JexExecutionContext, IReadOnlyList<JexValue>, JexValue> func,  // Implementation
    int minArgs = 0,                                               // Minimum required arguments
    int maxArgs = int.MaxValue                                     // Maximum allowed arguments
);

// RegisterVoidFunction - for functions with side effects
public void RegisterVoidFunction(
    string name,                                                     // Function name to use in scripts
    Action<JexExecutionContext, IReadOnlyList<JexValue>> action,    // Implementation
    int minArgs = 0,                                                // Minimum required arguments
    int maxArgs = int.MaxValue                                      // Maximum allowed arguments
);
```

### JexExecutionContext

The execution context provides access to the transformation state:

| Property | Type | Description |
|----------|------|-------------|
| `Input` | `JToken` | The input JSON document (read-only) |
| `Output` | `JToken` | The output JSON document being built |
| `Meta` | `JToken?` | Optional metadata passed from the host |
| `Options` | `JexExecutionOptions` | Execution options (timeouts, limits) |

### JexValue

`JexValue` is JEX's internal value type. Use static factory methods to create return values:

| Method | Description |
|--------|-------------|
| `JexValue.Null` | Returns a null value |
| `JexValue.True` / `JexValue.False` | Boolean values |
| `JexValue.FromBoolean(bool)` | Create from boolean |
| `JexValue.FromNumber(decimal)` | Create from number |
| `JexValue.FromString(string)` | Create from string |
| `JexValue.FromDateTime(DateTimeOffset)` | Create from date/time |
| `JexValue.FromJson(JToken)` | Create from JSON token |

Reading argument values:

| Method | Description |
|--------|-------------|
| `args[i].AsBoolean()` | Get as boolean (with type coercion) |
| `args[i].AsNumber()` | Get as decimal number |
| `args[i].AsString()` | Get as string |
| `args[i].AsDateTime()` | Get as DateTimeOffset |
| `args[i].AsJson()` | Get as JToken |
| `args[i].Kind` | Get the value type (`JexValueKind` enum) |
| `args[i].IsNull` / `IsString` / etc. | Check value type |

### Usage in Scripts

```jex
// Call a function that returns a value
%let hash = customHash($in.password);
%set $.passwordHash = &hash;

// Call a void function (no return value needed)
addAudit();

// Functions with multiple arguments
%let formatted = formatNumber($in.amount, 4);
log(concat("Processing order: ", $in.orderId));

// Use returned JSON objects
%let user = lookupUser($in.userId);
%set $.userName = jp1(&user, "$.name");
```

### Best Practices for Custom Functions

1. **Validate arguments** - Check argument count and types before processing
2. **Handle null values** - Arguments may be null; decide how to handle them
3. **Return appropriate types** - Match the expected usage in scripts
4. **Keep functions pure when possible** - Prefer `RegisterFunction` over `RegisterVoidFunction`
5. **Document your functions** - Provide clear names and usage examples

## Best Practices

### 1. Use Meaningful Variable Names

```jex
// Good
%let customerName = jp1($in, "$.customer.name");
%let orderTotal = &subtotal + &tax;

// Avoid
%let x = jp1($in, "$.customer.name");
%let t = &s + &tx;
```

### 2. Extract Repeated Logic into Functions

```jex
// Good - reusable function
%func formatCurrency(amount);
    %return concat("$", toString(round(&amount, 2)));
%endfunc;

%set $.subtotal = formatCurrency(&subtotal);
%set $.tax = formatCurrency(&tax);
%set $.total = formatCurrency(&total);
```

### 3. Use Libraries for Shared Code

Put commonly used functions in libraries that can be shared across projects.

### 4. Validate Input Data

```jex
%if (!existsPath($in, "$.required.field")) %then %do;
    %set $.error = "Missing required field";
    %return;
%end;
```

### 5. Build Output Progressively

```jex
// Build complex output step by step
%set $.result.status = "success";
%set $.result.data.items = &processedItems;
%set $.result.data.count = length(&processedItems);
%set $.result.metadata.processedAt = now();
```

## Common Patterns

### Filtering Arrays

```jex
%let items = jpAll($in, "$.products[*]");
%let filtered = arr();

%foreach item %in &items %do;
    %let price = jp1(&item, "$.price");
    %if (&price < 100) %then %do;
        push(&filtered, &item);
    %end;
%end;

%set $.affordableProducts = &filtered;
```

### Transforming Arrays

```jex
%let users = jpAll($in, "$.users[*]");
%let transformed = arr();

%foreach user %in &users %do;
    %let display = obj(
        "id", jp1(&user, "$.id"),
        "fullName", concat(jp1(&user, "$.first"), " ", jp1(&user, "$.last")),
        "email", lower(jp1(&user, "$.email"))
    );
    push(&transformed, &display);
%end;

%set $.users = &transformed;
```

### Aggregating Data

```jex
%let orders = jpAll($in, "$.orders[*]");
%let total = 0;
%let count = 0;

%foreach order %in &orders %do;
    %let amount = jp1(&order, "$.amount");
    %let total = &total + &amount;
    %let count = &count + 1;
%end;

%set $.summary.totalAmount = &total;
%set $.summary.orderCount = &count;
%set $.summary.averageOrder = &total / &count;
```

### Creating Lookup Tables

```jex
%let users = jpAll($in, "$.users[*]");
%let userIndex = indexBy(&users, "id");

// Later, look up a specific user
%let userId = jp1($in, "$.order.userId");
%let user = lookup(&userIndex, &userId);
%set $.orderUser = &user;
```

## Error Handling

JEX provides meaningful error messages with source locations:

```
JexCompileException: Unexpected token 'xyz' at (5,10)-(5,13)
JexRuntimeException: Division by zero at (12,5)-(12,15)
```

### Compile-Time Errors

- Syntax errors (missing semicolons, unclosed blocks)
- Unknown tokens
- Invalid expressions

### Runtime Errors

- Division by zero
- Unknown function calls
- Type mismatches
- JSONPath errors

## API Reference

### Jex Class

```csharp
// Create instance
var jex = new Jex();

// Compile a script (for reuse)
var program = jex.Compile(script);

// Execute directly
var result = jex.Execute(script, input);

// Execute compiled program
var result = program.Execute(input);

// Load libraries
jex.LoadLibrary(filePath);
jex.LoadLibrary(name, stream);
jex.LoadLibraryFromSource(name, source);

// Register custom functions
jex.RegisterFunction(name, handler, minArgs, maxArgs);
jex.RegisterVoidFunction(name, handler);
```

### Execution Options

```csharp
var options = new JexExecutionOptions
{
    MaxIterations = 100000,     // Loop iteration limit
    MaxRecursionDepth = 100,    // Function call depth limit
    TimeoutMs = 30000           // Execution timeout
};

var result = jex.Execute(script, input, options);
```

## Conclusion

JEX provides a powerful yet approachable language for JSON transformation. Its SAS-inspired syntax, rich function library, and extensibility make it suitable for a wide range of data transformation tasks.

For more examples, see the test files in the `JexEngine.Tests` project.
