# JEX VS Code Extension - User Guide

This guide covers how to use the JEX VS Code extension effectively for writing JSON transformation scripts.

## Table of Contents

- [Getting Started](#getting-started)
- [Syntax Highlighting](#syntax-highlighting)
- [Code Completion](#code-completion)
- [Hover Information](#hover-information)
- [Code Snippets](#code-snippets)
- [Error Diagnostics](#error-diagnostics)
- [Working with Function Manifests](#working-with-function-manifests)
- [JEX Language Reference](#jex-language-reference)
- [Tips and Best Practices](#tips-and-best-practices)

---

## Getting Started

### Creating a JEX File

1. Create a new file with the `.jex` extension
2. VS Code will automatically:
   - Apply JEX syntax highlighting
   - Activate the Language Server
   - Enable completions and hover

### Your First JEX Script

```jex
// Extract a value from input
%let name = jp1($in, "$.customer.name");

// Transform and set to output
%set $.greeting = concat("Hello, ", name, "!");
```

---

## Syntax Highlighting

The extension provides rich syntax highlighting for all JEX elements:

| Element | Color Theme Token | Example |
|---------|------------------|---------|
| Keywords | `keyword.control` | `%let`, `%set`, `%if`, `%foreach` |
| Built-in Variables | `variable.language` | `$in`, `$out`, `$meta` |
| Strings | `string.quoted` | `"hello world"` |
| Numbers | `constant.numeric` | `42`, `3.14` |
| JSONPath | `string.interpolated` | `$.customer.name` |
| Comments | `comment.line` | `// this is a comment` |
| Functions | `entity.name.function` | `jp1`, `concat`, `upper` |
| User Functions | `entity.name.function` | `%func myFunc(...)` |
| Parameters | `variable.parameter` | Parameters in function declarations |

### Comment Support

```jex
// Single-line comments are supported
%let x = 1; // End-of-line comments too
```

---

## Code Completion

Press `Ctrl+Space` (or type) to trigger completions. The extension provides:

### Keyword Completions

Type `%` to see all JEX keywords:

- `%let` - Declare a variable
- `%set` - Set a value in output
- `%if` / `%then` / `%else` - Conditional statements
- `%foreach` / `%in` - Loop over collections
- `%do` / `%end` - Block delimiters
- `%func` / `%endfunc` - Function declaration
- `%return` - Return from function
- `%break` / `%continue` - Loop control
- `%to` - Range operator

### Built-in Variable Completions

Type `$` to see built-in variables:

- `$in` - The input JSON document
- `$out` - The output JSON document being built
- `$meta` - Metadata dictionary

### Standard Library Functions

Over 80 functions are available with intelligent completions:

**JSONPath Functions**
- `jp1(json, path)` - Extract single value
- `jpAll(json, path)` - Extract all matches
- `jpFirst(json, ...paths)` - First non-null from multiple paths
- `existsPath(json, path)` - Check if path exists

**String Functions**
- `trim`, `lower`, `upper`, `substring`, `replace`
- `split`, `join`, `concat`, `format`
- `startsWith`, `endsWith`, `contains`, `indexOf`
- `padLeft`, `padRight`, `length`
- `regex`, `regexReplace`, `regexMatch`, `regexMatches`

**Math Functions**
- `abs`, `round`, `floor`, `ceiling`
- `min`, `max`, `sum`, `avg`
- `pow`, `sqrt`, `mod`

**Date/Time Functions**
- `now`, `utcNow`, `today`
- `formatDate`, `parseDate`
- `addDays`, `addHours`, `addMinutes`, `addSeconds`
- `dateDiff`, `year`, `month`, `day`, `hour`, `minute`, `second`

**Array Functions**
- `first`, `last`, `count`, `distinct`
- `where`, `select`, `orderBy`, `orderByDesc`
- `take`, `skip`, `reverse`, `flatten`
- `any`, `all`, `contains`
- `push`, `pop`, `insert`, `removeAt`
- `groupBy`, `aggregate`

**Type Functions**
- `isNull`, `isNumber`, `isString`, `isBool`, `isArray`, `isObject`
- `toNumber`, `toString`, `toBool`
- `typeOf`

**JSON Functions**
- `newObject`, `newArray`, `merge`, `clone`
- `remove`, `expandJson`

**Utility Functions**
- `coalesce`, `iif`, `default`
- `guid`, `hash`

### Variable Completions

Variables you've declared with `%let` appear in completions:

```jex
%let customerName = jp1($in, "$.name");
%let cust  // <- Typing here shows "customerName" in completions
```

### User Function Completions

Functions declared with `%func` appear in completions:

```jex
%func formatPrice(amount);
    %return concat("$", toString(&amount));
%endfunc;

%let price = formatPrice(99);  // <- "formatPrice" appears in completions
```

---

## Hover Information

Hover over any element to see documentation:

### Keyword Hover

Hover over `%foreach` to see:

```
%foreach - Loop over array

%foreach item %in array %do;
    // statements using item
%end;
```

### Function Hover

Hover over `jp1` to see:

```
jp1(json, path)

Returns the first token at the JSONPath. Returns null if not found.

Parameters: 2
```

### Variable Hover

Hover over a variable to see its declaration location.

### User Function Hover

Hover over a user function to see its signature and parameter count.

---

## Code Snippets

Type a prefix and press `Tab` to expand snippets:

| Prefix | Expands To |
|--------|------------|
| `let` or `%let` | `%let variableName = expression;` |
| `set` or `%set` | `%set $.path = expression;` |
| `if` or `%if` | If statement block |
| `ifelse` | If-else statement block |
| `foreach` | Foreach loop |
| `doloop` | Numeric do loop |
| `func` or `%func` | Function declaration |
| `return` | Return statement |
| `break` | Break statement |
| `continue` | Continue statement |
| `jp1` | JSONPath single extraction |
| `jpAll` | JSONPath all matches |
| `transform` | Complete transformation template |
| `array` | Array processing pattern |
| `conditional` | Conditional value mapping |
| `lookup` | Lookup/dictionary pattern |

### Example: Transform Template

Type `transform` and press `Tab`:

```jex
// JEX Transformation Script

// Extract input values
%let value = jp1($in, "$.path");

// Transform data

// Set output
%set $.result = value;
```

---

## Error Diagnostics

The Language Server reports syntax errors in real-time:

### Error Indicators

- **Red squiggly underlines** appear under syntax errors
- **Problems panel** shows all errors (`Ctrl+Shift+M`)
- **Error count** appears in status bar

### Common Errors

| Error | Cause | Fix |
|-------|-------|-----|
| "Expected ';'" | Missing semicolon | Add `;` at end of statement |
| "Expected identifier" | Missing variable name | Add variable name after `%let` |
| "Expected %endfunc" | Unclosed function | Add `%endfunc;` to close function |
| "Expected %end" | Unclosed block | Add `%end;` to close if/foreach |

### Example Error

```jex
%let x = 42  // Error: Expected ';'
%let y = ;   // Error: Expected expression
```

---

## Working with Function Manifests

If your application registers custom C# functions, create a manifest to get IDE support:

### Creating a Manifest

Create `myapp.jex.functions.json`:

```json
{
    "$schema": "https://raw.githubusercontent.com/koreforger/KoreForge.Jex/main/KoreForge.Jex.VSCode/schemas/jex.functions.schema.json",
    "functions": [
        {
            "name": "encryptValue",
            "description": "Encrypts a string value using the configured encryption key",
            "signature": "encryptValue(plainText)",
            "parameters": [
                {
                    "name": "plainText",
                    "type": "string",
                    "description": "The value to encrypt"
                }
            ],
            "returnType": "string"
        },
        {
            "name": "lookupCode",
            "description": "Looks up a code from the reference data table",
            "signature": "lookupCode(table, code, defaultValue?)",
            "parameters": [
                { "name": "table", "type": "string" },
                { "name": "code", "type": "string" },
                { "name": "defaultValue", "type": "any", "optional": true }
            ],
            "returnType": "any"
        }
    ]
}
```

### Registering the Manifest

Add the manifest path to your VS Code settings:

```json
{
    "jex.functionManifest.paths": [
        "${workspaceFolder}/myapp.jex.functions.json"
    ]
}
```

---

## JEX Language Reference

### Variables

```jex
// Declare a variable
%let name = "John";
%let age = 30;
%let items = [1, 2, 3];
%let config = { "enabled": true };

// Variables are referenced by name
%let greeting = concat("Hello, ", name);
```

### Setting Output

```jex
// Set a value in the output document
%set $.customer.name = name;
%set $.items[0] = "first";

// Set using JSONPath expression
%set jp1($out, "$.nested.path") = value;
```

### Conditionals

```jex
%if (age >= 18) %then %do;
    %set $.status = "adult";
%end;
%else %do;
    %set $.status = "minor";
%end;
```

### Loops

```jex
// Foreach loop
%foreach item %in items %do;
    %let processed = upper(item);
    push($.results, processed);
%end;

// Numeric loop
%do i = 0 %to 10 %do;
    push($.numbers, i);
%end;

// Loop control
%foreach item %in items %do;
    %if (item == null) %then %do;
        %continue;
    %end;
    %if (item == "stop") %then %do;
        %break;
    %end;
%end;
```

### User-Defined Functions

```jex
// Declare a function (note: semicolon after params, use & for parameters)
%func calculateTotal(price, quantity);
    %let subtotal = &price * &quantity;
    %let tax = &subtotal * 0.1;
    %return &subtotal + &tax;
%endfunc;

// Call the function
%let total = calculateTotal(100, 5);
```

### Built-in Variables

```jex
// $in - Input document (read-only)
%let name = jp1($in, "$.customer.name");

// $out - Output document (read-write)
%set $.result = "done";
%let current = jp1($out, "$.result");

// $meta - Metadata dictionary
%let source = jp1($meta, "$.sourceSystem");
```

---

## Tips and Best Practices

### 1. Use Descriptive Variable Names

```jex
// Good
%let customerEmail = jp1($in, "$.customer.email");
%let orderTotal = sum(jpAll($in, "$.orders[*].amount"));

// Avoid
%let x = jp1($in, "$.customer.email");
%let t = sum(jpAll($in, "$.orders[*].amount"));
```

### 2. Extract Common Values Early

```jex
// Extract frequently used values at the top
%let customer = jp1($in, "$.customer");
%let orders = jpAll($in, "$.orders[*]");

// Use them throughout the script
%set $.customerName = jp1(customer, "$.name");
%set $.orderCount = count(orders);
```

### 3. Use Comments Liberally

```jex
// Transform customer order data
// Input: Raw order from external system
// Output: Normalized order for internal processing

// Step 1: Extract customer details
%let customerId = jp1($in, "$.cust_id");

// Step 2: Map status codes
%let rawStatus = jp1($in, "$.stat");
%let status = iif(rawStatus == "A", "active", "inactive");
```

### 4. Use Functions for Reusable Logic

```jex
// Define reusable transformations
%func formatCurrency(amount);
    %return concat("$", toString(round(&amount, 2)));
%endfunc;

%func normalizePhone(phone);
    %let digits = regexReplace(&phone, "[^0-9]", "");
    %return concat("(", substring(&digits, 0, 3), ") ", 
                   substring(&digits, 3, 3), "-",
                   substring(&digits, 6, 4));
%endfunc;

// Use them consistently
%set $.price = formatCurrency(jp1($in, "$.amount"));
%set $.phone = normalizePhone(jp1($in, "$.contact.phone"));
```

### 5. Handle Null Values

```jex
// Use coalesce for defaults
%let name = coalesce(jp1($in, "$.name"), "Unknown");

// Use existsPath to check before accessing
%if (existsPath($in, "$.optional.field")) %then %do;
    %set $.optional = jp1($in, "$.optional.field");
%end;

// Use iif for conditional values
%set $.status = iif(isNull(jp1($in, "$.status")), "pending", jp1($in, "$.status"));
```

### 6. Use Snippets for Speed

- Type `foreach` + Tab instead of typing the full loop structure
- Type `if` + Tab for quick conditionals
- Type `func` + Tab for function declarations

---

## See Also

- [Installation Guide](Installation-Guide.md) - Installing and configuring the extension
- [Developer Guide](Developer-Guide.md) - Contributing to the extension
