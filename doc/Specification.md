Yes — you’ve given enough to write a complete spec that an engineer can implement without “guessing the intent”.

Below is a **single, end-to-end specification document** that includes: problem definition, how it works, requirements, and the full language + runtime specification (including the **C# host preprocessor extension** for nested/escaped JSON-in-strings).

---

# JEX Specification

**Version:** 1.0 (Initial)
**Status:** Draft for implementation
**Target runtime:** .NET 10+
**JSON library:** Newtonsoft.Json (`JToken`, `JObject`, `JArray`)
**Primary use-case:** High-throughput JSON extraction + light transformation into new JSON.

---

## 1. Problem Description

### 1.1 Context

The system processes approximately **30 million JSON messages per day**, with each message up to **~1MB**, extracting and transforming **~50 fields** per message. Input data can contain:

* Nested objects/arrays
* Multiple arrays that must be “merged” (join-like) to construct correct output
* Variant schemas where the same logical value appears in different JSON paths across instances
* String fields containing **escaped JSON**, sometimes nested multiple times (JSON inside JSON string inside JSON string)

The transformation logic must be:

* **Simple to read and write**
* **Deterministic**
* **Fast enough for production scale**
* **Safe** (no arbitrary code execution)

### 1.2 Objective

Define a DSL named **JEX** that:

* Accepts **one JSON document** as input (`$in`)
* Produces **one JSON document** as output (`$out`)
* Supports JsonPath extraction, variables, loops, string ops, basic math/date ops
* Feels like **SAS macro language** (syntax & semantics)
* Is implemented as a **compile-once, run-many** engine in C#/.NET 10+

---

## 2. What We Are Building

### 2.1 Components

JEX consists of:

1. **Compiler**

   * Tokenizer (lexer)
   * Parser → AST
   * Semantic analysis (scope, function signatures, basic type checks)
   * Compilation → executable program representation (bytecode VM recommended)
2. **Runtime**

   * Executes compiled JEX program for each input message
   * Provides built-in functions (stdlib)
   * Provides JSON manipulation + path extraction
   * Enforces safety limits (iterations, recursion, regex timeouts)
3. **Host integration**

   * C# API: compile scripts and execute against `JToken`
   * Ability to register **host extension functions**
   * Optional **input preprocessing step** to normalize “JSON in string fields”

### 2.2 High-level Flow

For each message type:

1. Compile `script.jex` once → `IJexProgram`
2. For each input JSON message:

   1. Parse JSON to `JToken` (or receive already-parsed token)
   2. Optional: run input preprocessor (JSON-in-string normalization)
   3. Execute compiled JEX program with `$in` bound to input token
   4. Output produced as `$out` (a `JToken`)

---

## 3. Requirements

### 3.1 Functional Requirements

**FR-1** Input/Output

* JEX MUST accept one JSON document as input and produce one JSON document as output.

**FR-2 JsonPath Extraction**

* JEX MUST support extracting values from input via JsonPath-like expressions.
* JEX MUST support extracting:

  * single token (`SelectToken` semantics)
  * multi tokens / arrays (`SelectTokens` semantics)

**FR-3 Variables**

* JEX MUST support temporary variables.

**FR-4 Control Flow**

* JEX MUST support `%if/%then/%else`.
* JEX MUST support looping over arrays/tokens.
* JEX MUST support `%break` and `%continue` inside loops.

**FR-5 Output Construction**

* JEX MUST support:

  * assigning scalar values into output object paths
  * building arrays in output
  * creating objects/arrays from expressions

**FR-6 String Ops**

* JEX MUST support common string operations:

  * trim, lower/upper
  * split
  * substring/left/right
  * replace (and optionally regex replace)

**FR-7 Math Ops**

* JEX MUST support basic numeric operations (+,-,*,/,%, round/floor/ceil/min/max/abs).

**FR-8 Date Ops**

* JEX MUST support basic date parsing/formatting and date add/diff.

**FR-9 “Try Multiple Paths Until Found”**

* JEX MUST support fallback extraction across multiple paths (coalesce-first-found).

**FR-10 Array Merge / Join**

* JEX MUST support join-like behavior between arrays, at minimum via indexing (`indexBy`) + lookup, and optionally a `join()` helper.

**FR-11 User-defined Functions (Nice-to-have)**

* JEX SHOULD support defining functions in-script.
* JEX MUST support host-registered functions.

**FR-12 Preprocessing Extension (Host)**

* JEX MUST allow a host-side preprocessing step that:

  * traverses input JSON
  * detects strings that contain JSON (object/array)
  * parses and replaces the string with the parsed JSON token
  * repeats for nested JSON-in-string up to configurable depth/limits

### 3.2 Non-Functional Requirements

**NFR-1 Performance**

* Scripts MUST be compiled once and reused.
* Runtime MUST be safe for multi-threaded execution (program immutable; per-execution state isolated).
* Must sustain high throughput for stated scale; avoid per-message heavy allocations.

**NFR-2 Simplicity**

* Syntax MUST remain small and readable, macro-like, avoiding a “full programming language”.

**NFR-3 Determinism & Safety**

* No file IO, no network, no reflection, no dynamic code execution.
* Runtime MUST enforce configurable limits:

  * maximum loop iterations
  * maximum function recursion depth
  * regex timeouts
  * maximum preprocessing depth / node count (for preprocessor)

**NFR-4 Diagnostics**

* Compiler and runtime MUST provide:

  * line/column errors
  * failing expression/function name/path where possible
  * optional “strict vs lenient” handling for missing fields

---

## 4. Language Specification

## 4.1 Lexical Rules

### 4.1.1 Character Set

* UTF-8 input scripts.

### 4.1.2 Tokens

* **Identifiers:** `[A-Za-z_][A-Za-z0-9_]*`
* **Integers:** `123`
* **Decimals:** `123.45`
* **String literals:** `"..."` with escapes `\" \\ \n \r \t`
* **JsonPath literals:** `$.a.b[0].c` MUST be provided as a string literal (e.g. `"$.a.b"`).
* **Operators:** `=`, `==`, `!=`, `<`, `<=`, `>`, `>=`, `+`, `-`, `*`, `/`, `%`, `&&`, `||`, `!`
* **Punctuation:** `(` `)` `{` `}` `[` `]` `,` `;`

### 4.1.3 Comments

* Single-line: `// comment`
* Block: `/* comment */`

### 4.1.4 Keywords (case-insensitive)

`%let %set %if %then %else %do %end %foreach %in %break %continue %return %func %endfunc`

---

## 4.2 Runtime Values and Types

JEX is dynamically typed at runtime, with optional compile-time checks.

### 4.2.1 Value Kinds

* `Null`
* `Boolean`
* `Number` (recommended: `decimal` for deterministic math; allow conversion to double where needed)
* `String`
* `DateTime` (`DateTimeOffset`)
* `Json` (`JToken` including object/array/value)

### 4.2.2 Null Handling

Two modes:

* **Lenient (default):** missing paths resolve to `null` and operations typically propagate `null` (unless function defines defaults).
* **Strict:** missing required values throw runtime errors.

---

## 4.3 Program Model

### 4.3.1 Built-in variables

* `$in` : input `JToken` (read-only)
* `$out`: output `JToken` (initially `{}` by default; configurable)
* `$meta` (optional host-provided object): message metadata/constants

### 4.3.2 Variables

* Declared via `%let`:

  * `%let x = expr;`
* Variables are referenced SAS-style with `&name`:

  * `%let a = 5; %let b = &a + 1;`
* Inside strings, macro expansion is supported:

  * `"Hello &name"` replaces `&name` with its string value.
  * If variable is missing: replaces with empty string in lenient mode; error in strict mode.

---

## 4.4 Statements

### 4.4.1 Variable assignment

* `%let name = expr;`
* Re-assignment permitted.

### 4.4.2 Output assignment

Two forms:

**A) Direct property assignment (object only):**

* `%set $.path = expr;`

  * Applies to `$out` by default.
  * Example: `%set $.user.id = jp1($in, "$.userId");`

**B) Explicit set with target token:**

* `%set target, "$.path", expr;`

  * Example: `%set $out, "$.items[0].name", &x;`

(Implementation note: form A is sugar for B.)

### 4.4.3 Conditionals

```
%if (condition) %then %do;
   statements
%end;
%else %do;
   statements
%end;
```

* `%else` block optional.
* Condition uses boolean semantics: `null` is false in lenient mode.

### 4.4.4 Loops

**A) Foreach over JsonPath results**

```
%foreach item %in jpAll($in, "$.arr[*]") %do;
   ...
%end;
```

* `item` is a variable bound each iteration to an element `JToken`.

**B) Numeric loop (optional, SAS-like)**

```
%do i = 1 %to 10;
   ...
%end;
```

### 4.4.5 Loop control

* `%break;`
* `%continue;`

### 4.4.6 Return

* `%return;` stops execution and returns current `$out`.

### 4.4.7 Functions (in-script, nice-to-have)

```
%func Name(arg1, arg2);
   statements
   // last evaluated expression may be return value OR require explicit:
   %return expr;
%endfunc;
```

* Function overloading NOT supported in v1.
* No closures.
* Recursion allowed only if enabled; enforce max recursion depth.

---

## 4.5 Expressions

### 4.5.1 Literals

* numbers, strings, booleans (`true/false`), `null`
* Inline JSON literals (optional but useful):

  * object: `{ "a": 1, "b": "x" }`
  * array: `[1, 2, 3]`

### 4.5.2 Operators & precedence (highest to lowest)

1. Unary: `!`, unary `-`
2. Multiplicative: `*`, `/`, `%`
3. Additive: `+`, `-`
4. Comparison: `<`, `<=`, `>`, `>=`
5. Equality: `==`, `!=`
6. AND: `&&`
7. OR: `||`

### 4.5.3 Function calls

* `funcName(arg1, arg2, ...)`

---

## 4.6 JSON Extraction and Construction Functions (StdLib)

### 4.6.1 JsonPath

**jp1(json, pathString) -> value**

* Returns the first token at path (or null).

**jpAll(json, pathString) -> array**

* Returns array of all matches (empty array if none).

**coalescePath(json, "path1", "path2", ...) -> value**

* Evaluates paths in order, returning the first that is:

  * present and non-null (default rule)
* Mode option:

  * `present` vs `nonNull` matching (configurable)

**existsPath(json, "path") -> bool**

* True if token exists (even if null) unless configured.

### 4.6.2 Output construction helpers

**setPath(targetJson, "path", value) -> void**

* Creates intermediate objects/arrays as needed.
* If target is null and path starts at root: create object by default.

**push(arrayToken, value) -> void**

* Adds to JArray.

**obj(k1, v1, k2, v2, ...) -> JObject**

* Builds an object from alternating key/value arguments.

**arr(v1, v2, ...) -> JArray**

* Builds an array.

### 4.6.3 String functions

* `trim(s)`, `lower(s)`, `upper(s)`
* `substr(s, start, length?)`
* `left(s, n)`, `right(s, n)`
* `split(s, delimiter) -> array`
* `replace(s, find, repl)`
* Nice-to-have:

  * `regexMatch(s, pattern)`
  * `regexReplace(s, pattern, repl)` (MUST enforce timeout)

### 4.6.4 Math functions

* `abs(n)`, `min(a,b)`, `max(a,b)`
* `round(n, digits?)`, `floor(n)`, `ceil(n)`

### 4.6.5 Date functions

* `parseDate(s, format?, timezone?) -> datetime`
* `formatDate(dt, format) -> string`
* `dateAdd(dt, unit, amount) -> datetime` (units: "days","hours","minutes","seconds")
* `dateDiff(a, b, unit) -> number`

### 4.6.6 Type conversion

* `toString(x)`, `toNumber(x)`, `toBool(x)`, `toDate(x)`
* `isNull(x)`, `isEmpty(x)` (empty string/empty array/empty object)

---

## 4.7 Array Merge / Join Specification

### 4.7.1 Indexing

**indexBy(array, keyPathOrFn) -> map**

* Produces a dictionary-like structure usable by `lookup`.
* If duplicates:

  * default: last wins (configurable)
  * optional: `indexByAll` to store lists

**lookup(map, key) -> value**

* Returns mapped token or null.

### 4.7.2 Join helper (optional but very practical)

**join(leftArr, rightArr, leftKeyFn, rightKeyFn, mergeFn?) -> array**

* Default behavior: inner join producing merged objects.
* Optional behavior flags: inner/left/right.
* In v1 you can implement join via `indexBy` + `%foreach`.

---

## 4.8 Error Handling and Diagnostics

### 4.8.1 Compile-time errors

Must include:

* line/column range
* token near error
* expected constructs (where practical)

### 4.8.2 Runtime errors

Must include:

* script location (statement span)
* function name/operator
* path string (for path ops)
* optional: message-type identifier (from host metadata)

### 4.8.3 Strict vs Lenient

* Missing path:

  * Lenient: returns null
  * Strict: throws (or throws only if marked required; v1 can just use strict global)

---

## 4.9 Performance and Limits

### 4.9.1 Compilation

* A JEX script MUST be compiled once and reused across messages.
* Compiled program MUST be immutable and thread-safe.

### 4.9.2 Execution

* Execution MUST allocate minimal per-message state:

  * variable map / stack
  * output token
* JsonPath results SHOULD be cached per-execution for repeated calls.

### 4.9.3 Limits (configurable)

* `MaxLoopIterations`
* `MaxFunctionRecursionDepth`
* `RegexTimeoutMs`
* `MaxOutputSizeBytes` (optional)
* `MaxPreprocessDepth`, `MaxPreprocessNodes`, `MaxPreprocessStringLength`

---

## 4.10 Security

* No IO, no network, no reflection.
* Host functions must be explicitly registered (whitelist).
* Regex must enforce timeouts and ideally restrict patterns in strict mode.

---

# 5. Host Integration Specification (C#)

## 5.1 Public API Contracts

Recommended interfaces (names are suggestions; spec requires these capabilities):

* `IJexCompiler`

  * `IJexProgram Compile(string script, JexCompileOptions options);`

* `IJexProgram`

  * `JToken Execute(JToken input, JexExecutionOptions options);`

* `IJexFunctionRegistry`

  * `void Register(string name, IJexFunction func);`

* `IJexFunction`

  * `JexValue Invoke(JexExecutionContext ctx, IReadOnlyList<JexValue> args);`

Execution options include strict/lenient, limits, and meta/constants injection.

---

# 6. Input Preprocessing Extension (Host) — JSON-in-String Normalizer

## 6.1 Problem

Some input JSON documents contain properties where the value is a **string that itself contains JSON**, often escaped (e.g. `"{"a":1}"` stored as `"{\"a\":1}"`). This can be nested multiple times.

JEX scripts must not have to repeatedly decode these; the host can normalize once per message.

## 6.2 Required Behavior

The host preprocessor MUST:

1. Traverse the entire input `JToken` tree (objects and arrays).
2. For each `JValue` string:

   * Detect if the string “looks like” JSON object/array (heuristic).
   * Attempt to parse into a `JToken`.
   * If parsed successfully:

     * Replace the original string token with the parsed token.
     * Then recursively process the newly inserted token (because it may still contain nested JSON-in-strings).
3. Repeat until no further replacements are possible or limits are hit.

## 6.3 Limits / Safety Requirements

Preprocessor MUST support:

* `MaxDepth` (max nested parsing levels per encountered string)
* `MaxNodesVisited` (prevent pathological huge documents)
* `MaxStringLength` (don’t attempt parse on massive strings)
* `MaxTotalReplacements` (cap replacements per document)
* `StrictMode`:

  * strict: malformed JSON-like strings cause error
  * lenient: ignore parse failures

## 6.4 Heuristic Detection (Spec)

A string qualifies for parse attempt if:

* After trimming whitespace, it starts with `{` and ends with `}` OR starts with `[` and ends with `]`
* AND length is within `MaxStringLength`
* AND does not exceed replacement limits

Additionally, the parser SHOULD handle JSON that is itself quoted/escaped. The implementation MUST attempt:

1. `JToken.Parse(trimmed)`
2. If that fails:

   * Try unescaping one layer (e.g., if string is `"\"{\\\"a\\\":1}\""` style)
   * Then parse again
3. Repeat up to `MaxDepth`

## 6.5 Reference Implementation (C# / Newtonsoft)

This is the built-in host extension you asked for. It is written to be reusable, testable, and safe.

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed record JsonStringNormalizationOptions(
    int MaxDepthPerString = 5,
    int MaxNodesVisited = 250_000,
    int MaxTotalReplacements = 50_000,
    int MaxStringLength = 256_000,
    bool Strict = false
);

public static class JsonStringNormalizer
{
    public static JToken NormalizeJsonStrings(JToken input, JsonStringNormalizationOptions options)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (options is null) throw new ArgumentNullException(nameof(options));

        var state = new State(options);
        var clone = input.DeepClone(); // keep original untouched; remove if you want in-place
        NormalizeToken(clone, depth: 0, state);
        return clone;
    }

    private static void NormalizeToken(JToken token, int depth, State state)
    {
        state.CountNode();
        switch (token.Type)
        {
            case JTokenType.Object:
            {
                var obj = (JObject)token;
                foreach (var prop in obj.Properties())
                {
                    NormalizeToken(prop.Value, depth + 1, state);
                }
                break;
            }
            case JTokenType.Array:
            {
                var arr = (JArray)token;
                for (var i = 0; i < arr.Count; i++)
                {
                    NormalizeToken(arr[i], depth + 1, state);
                }
                break;
            }
            case JTokenType.String:
            {
                var jv = (JValue)token;
                var s = (string?)jv.Value;
                if (string.IsNullOrWhiteSpace(s)) return;
                if (s.Length > state.Options.MaxStringLength) return;

                if (!LooksLikeJson(s))
                    return;

                if (TryParsePossiblyEscapedJson(s, state.Options.MaxDepthPerString, out var parsed))
                {
                    state.CountReplacement();

                    // Replace this token with parsed JSON
                    ReplaceTokenInParent(jv, parsed);

                    // Then normalize inside the newly inserted structure too
                    NormalizeToken(parsed, depth + 1, state);
                }
                else if (state.Options.Strict)
                {
                    throw new JsonException("String looked like JSON but could not be parsed.");
                }

                break;
            }
            default:
                // other primitive types ignored
                break;
        }
    }

    private static bool LooksLikeJson(string s)
    {
        var t = s.AsSpan().Trim();
        if (t.Length < 2) return false;

        var first = t[0];
        var last = t[^1];

        return (first == '{' && last == '}') || (first == '[' && last == ']');
    }

    private static bool TryParsePossiblyEscapedJson(string s, int maxDepth, out JToken parsed)
    {
        parsed = null!;

        var current = s.Trim();

        for (var i = 0; i < maxDepth; i++)
        {
            // Attempt direct parse
            if (TryParseJson(current, out parsed))
                return true;

            // Attempt to unescape one layer:
            // If current is a JSON string literal containing JSON, deserialize to string.
            // Example: "\"{\\\"a\\\":1}\"" -> "{\"a\":1}"
            if (!TryUnescapeJsonStringLiteral(current, out var unescaped))
                return false;

            current = unescaped.Trim();
        }

        return false;
    }

    private static bool TryParseJson(string s, out JToken token)
    {
        try
        {
            token = JToken.Parse(s);
            return true;
        }
        catch
        {
            token = null!;
            return false;
        }
    }

    private static bool TryUnescapeJsonStringLiteral(string s, out string unescaped)
    {
        // This tries to interpret s as a JSON string literal.
        // If s isn't a JSON string literal, it will fail.
        try
        {
            var result = JsonConvert.DeserializeObject<string>(s);
            if (result is null)
            {
                unescaped = string.Empty;
                return false;
            }

            unescaped = result;
            return true;
        }
        catch
        {
            unescaped = string.Empty;
            return false;
        }
    }

    private static void ReplaceTokenInParent(JToken oldToken, JToken newToken)
    {
        var parent = oldToken.Parent;
        if (parent is null)
            throw new InvalidOperationException("Cannot replace root token in-place.");

        switch (parent)
        {
            case JProperty prop:
                prop.Value = newToken;
                break;

            case JArray arr:
            {
                var index = arr.IndexOf(oldToken);
                if (index < 0) throw new InvalidOperationException("Array parent did not contain token.");
                arr[index] = newToken;
                break;
            }

            default:
                throw new NotSupportedException($"Unsupported parent type for replacement: {parent.Type}");
        }
    }

    private sealed class State
    {
        public State(JsonStringNormalizationOptions options) => Options = options;

        public JsonStringNormalizationOptions Options { get; }
        private int _nodesVisited;
        private int _replacements;

        public void CountNode()
        {
            _nodesVisited++;
            if (_nodesVisited > Options.MaxNodesVisited)
                throw new InvalidOperationException($"Preprocess exceeded MaxNodesVisited ({Options.MaxNodesVisited}).");
        }

        public void CountReplacement()
        {
            _replacements++;
            if (_replacements > Options.MaxTotalReplacements)
                throw new InvalidOperationException($"Preprocess exceeded MaxTotalReplacements ({Options.MaxTotalReplacements}).");
        }
    }
}
```

### Notes (behavioral, not “nice commentary”)

* This is **safe by default** (limits enforced).
* It handles:

  * raw JSON in strings: `"{\"a\":1}"` (if already unescaped enough)
  * deeply escaped JSON-string-literal forms by iteratively deserializing string literals then parsing
* It returns a **deep clone** so you can keep the original input if you want. If you want max performance, you can do in-place by removing the clone (spec allows either; default recommended is clone for safety).

---

# 7. Example JEX Script (Demonstrates Key Features)

```sas
%let userId = coalescePath($in, "$.user.id", "$.userId", "$.identity.userId");

%set $.user.id = &userId;
%set $.user.name = jp1($in, "$.user.name");

%let parts = split(jp1($in, "$.device.fingerprint"), "|");
%set $.device.os = jp1(&parts, "$[0]");
%set $.device.browser = jp1(&parts, "$[1]");

%let txArr = jpAll($in, "$.transactions[*]");
%let payArr = jpAll($in, "$.payments[*]");

%let payIndex = indexBy(&payArr, "id");

%let outItems = arr();

%foreach tx %in &txArr %do;
  %let pid = jp1(&tx, "$.paymentId");
  %let pay = lookup(&payIndex, &pid);

  %if (&pay != null) %then %do;
     %let item = obj(
        "txId", jp1(&tx, "$.id"),
        "amount", toNumber(jp1(&tx, "$.amount")),
        "payRef", jp1(&pay, "$.ref")
     );
     push(&outItems, &item);
  %end;
%end;

%set $.items = &outItems;
```

---

# 8. Implementation Guidance (Normative)

This section is normative: the implementation must satisfy these behaviors.

## 8.1 Compiler stages

* Lexer → Parser → AST → semantic analysis → compiled program.
* Must preserve source spans for diagnostics.

## 8.2 Runtime

* Program immutable, thread-safe.
* Execution uses an isolated context containing:

  * `$in`, `$out`, `$meta`
  * variable store
  * runtime config/limits

## 8.3 JsonPath caching

* Repeated identical paths SHOULD be cached per execution, keyed by `(rootTokenIdentity, pathString, mode)`.

---

# 9. Acceptance Criteria

A JEX implementation is considered conforming if:

* It executes scripts matching the statement/expression semantics above.
* It provides all required stdlib functions.
* It supports strict/lenient missing handling.
* It can preprocess JSON-in-string using the host extension capability.
* It produces deterministic output for the same input/script/options.
* It provides line/col diagnostics for compile and runtime errors.

---

