# JEX VS Code Extension - Developer Guide

This guide is for developers who want to contribute to, extend, or understand the internals of the JEX VS Code extension.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Development Environment Setup](#development-environment-setup)
- [Building the Extension](#building-the-extension)
- [Running Tests](#running-tests)
- [Debugging](#debugging)
- [Language Server Deep Dive](#language-server-deep-dive)
- [VS Code Extension Deep Dive](#vs-code-extension-deep-dive)
- [Adding New Features](#adding-new-features)
- [Code Style and Conventions](#code-style-and-conventions)
- [Release Process](#release-process)

---

## Architecture Overview

The JEX VS Code extension follows a client-server architecture using the Language Server Protocol (LSP):

```
┌─────────────────────────────────────────────────────────────────┐
│                        VS Code                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              JEX Extension (TypeScript)                   │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │  │
│  │  │  TextMate   │  │  Snippets   │  │   LSP Client    │  │  │
│  │  │   Grammar   │  │             │  │                 │  │  │
│  │  └─────────────┘  └─────────────┘  └────────┬────────┘  │  │
│  └─────────────────────────────────────────────┼────────────┘  │
└────────────────────────────────────────────────┼────────────────┘
                                                 │ LSP (JSON-RPC)
                                                 │ stdio
┌────────────────────────────────────────────────┼────────────────┐
│                    Language Server (C#)        │                │
│  ┌─────────────────────────────────────────────┴────────────┐  │
│  │                   LSP Handlers                            │  │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐  │  │
│  │  │ Completion   │ │    Hover     │ │  TextDocSync     │  │  │
│  │  │   Handler    │ │   Handler    │ │    Handler       │  │  │
│  │  └──────┬───────┘ └──────┬───────┘ └────────┬─────────┘  │  │
│  └─────────┼────────────────┼──────────────────┼────────────┘  │
│            │                │                  │                │
│  ┌─────────┴────────────────┴──────────────────┴────────────┐  │
│  │                      Services                             │  │
│  │  ┌─────────────┐ ┌─────────────┐ ┌───────────────────┐   │  │
│  │  │  Document   │ │  Standard   │ │     Function      │   │  │
│  │  │   Manager   │ │   Library   │ │  ManifestLoader   │   │  │
│  │  └──────┬──────┘ └─────────────┘ └───────────────────┘   │  │
│  │         │                                                 │  │
│  │  ┌──────┴──────┐                                         │  │
│  │  │  Document   │ ◄─── Uses JEX Parser/Lexer              │  │
│  │  │    State    │                                         │  │
│  │  └─────────────┘                                         │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Language | Purpose |
|-----------|----------|---------|
| VS Code Extension | TypeScript | UI integration, LSP client, grammar, snippets |
| Language Server | C# (.NET 10) | Parsing, completions, hover, diagnostics |
| JEX Parser | C# | Reused from KoreForge.Jex for accurate parsing |

---

## Project Structure

The JEX VS Code extension is split across three separate projects at the workspace root:

```
c:\My\KoreForge\
├── KoreForge.Jex/                        # Core JEX library
│   ├── src/
│   │   └── KF.Jex/                       # Parser, runtime, standard library
│   ├── tst/
│   │   ├── KF.Jex.Tests/
│   │   └── KF.Jex.Benchmarks/
│   ├── bin/                          # build-*.ps1, git-*.ps1
│   └── doc/
│
├── KoreForge.Jex.LanguageServer/         # Language Server (separate project)
│   ├── src/
│   │   └── KoreForge.Jex.LanguageServer/ # LSP implementation
│   │       ├── Program.cs            # Entry point
│   │       ├── Handlers/             # Completion, Hover, TextDocSync
│   │       └── Services/             # DocumentManager, etc.
│   ├── tests/
│   │   └── KoreForge.Jex.LanguageServer.Tests/
│   └── scripts/                      # build.ps1, test.ps1, clean.ps1
│
├── KoreForge.Jex.VSCode/                 # VS Code Extension (separate project)
│   ├── src/
│   │   ├── extension.ts              # Extension entry point
│   │   └── test/                     # Extension tests
│   ├── syntaxes/
│   │   └── jex.tmLanguage.json       # TextMate grammar
│   ├── snippets/
│   │   └── jex.json                  # Code snippets
│   ├── schemas/                      # JSON schemas
│   ├── icons/                        # Extension icons
│   ├── examples/                     # Sample files
│   ├── scripts/                      # build.ps1, pack.ps1, clean.ps1
│   ├── server/                       # Published Language Server (created by build)
│   └── package.json                  # Extension manifest
│
└── artifacts/                        # Build outputs
    └── vsix/
        └── koreforge-jex-x.x.x.vsix
```

---

## Development Environment Setup

### Prerequisites

```powershell
# Check .NET SDK
dotnet --version  # Should be 10.0 or later

# Check Node.js
node --version    # Should be 18.x or later
npm --version     # Should be 9.x or later

# Check PowerShell
$PSVersionTable.PSVersion  # Should be 7.x or later
```

### Initial Setup

```powershell
# Clone the repository
git clone <repo-url>
cd KoreForge.Jex

# Restore NuGet packages
dotnet restore

# Install VS Code extension dependencies
cd KoreForge.Jex.VSCode
npm install
cd ..

# Build everything
.\scripts\build.ps1
```

### IDE Setup

**For C# (Language Server)**:
- Open `KoreForge.Jex.sln` in Visual Studio 2022 or VS Code with C# Dev Kit
- Ensure .NET 10 SDK is selected

**For TypeScript (VS Code Extension)**:
- Open `KoreForge.Jex.VSCode` folder in VS Code
- Install recommended extensions when prompted

---

## Building

### Build Each Project Separately

```powershell
# Build core JEX library
cd c:\My\KoreForge\KoreForge.Jex
.\scripts\build.ps1

# Build Language Server
cd c:\My\KoreForge\KoreForge.Jex.LanguageServer
.\scripts\build.ps1

# Build VS Code Extension (includes Language Server)
cd c:\My\KoreForge\KoreForge.Jex.VSCode
.\scripts\build.ps1
```

### Build Language Server Only

```powershell
cd c:\My\KoreForge\KoreForge.Jex.LanguageServer
dotnet build src/KoreForge.Jex.LanguageServer
```

### Build VS Code Extension Only

```powershell
cd c:\My\KoreForge\KoreForge.Jex.VSCode
npm run compile
```

### Create VSIX Package

```powershell
cd c:\My\KoreForge\KoreForge.Jex.VSCode
.\scripts\pack.ps1
# Output: c:\My\KoreForge\artifacts\vsix\koreforge-jex-x.x.x.vsix
```

---

## Running Tests

### All Tests for Each Project

```powershell
# Test core JEX library
cd c:\My\KoreForge\KoreForge.Jex
.\scripts\test.ps1

# Test Language Server
cd c:\My\KoreForge\KoreForge.Jex.LanguageServer
.\scripts\test.ps1
```

### Language Server Tests Only

```powershell
cd c:\My\KoreForge\KoreForge.Jex.LanguageServer
dotnet test tests/KoreForge.Jex.LanguageServer.Tests
```

### With Code Coverage

```powershell
dotnet test tests/KoreForge.Jex.LanguageServer.Tests `
    --collect:"XPlat Code Coverage" `
    --results-directory TestResults
```

The coverage report is generated at `TestResults/*/coverage.cobertura.xml`.

### Coverage Threshold

The project maintains a **60% minimum code coverage** threshold for the Language Server. Current coverage is approximately **70%**.

### VS Code Extension Tests

```powershell
cd KoreForge.Jex.VSCode
npm test
```

Note: VS Code extension tests require a VS Code instance and may need to run in a headed environment.

---

## Debugging

### Debugging the Language Server

**Option 1: Attach to Running Process**

1. Start VS Code with the extension
2. Open a `.jex` file to start the Language Server
3. In Visual Studio, go to `Debug > Attach to Process`
4. Find `KoreForge.Jex.LanguageServer.exe`

**Option 2: Configure Extension to Wait for Debugger**

Edit `extension.ts` to add `--debug` flag:

```typescript
const serverOptions: ServerOptions = {
    command: serverPath,
    args: ['--debug']  // Add this
};
```

Then in `Program.cs`, add at the start:

```csharp
#if DEBUG
System.Diagnostics.Debugger.Launch();
#endif
```

### Debugging the VS Code Extension

1. Open `KoreForge.Jex.VSCode` folder in VS Code
2. Press `F5` to launch Extension Development Host
3. Set breakpoints in `extension.ts`
4. The debugger will stop at breakpoints

### Viewing LSP Messages

Add logging to see LSP communication:

```typescript
// In extension.ts
const clientOptions: LanguageClientOptions = {
    // ...
    outputChannel: vscode.window.createOutputChannel('JEX LSP Trace'),
    traceOutputChannel: vscode.window.createOutputChannel('JEX LSP Trace'),
};
```

---

## Language Server Deep Dive

### Entry Point (Program.cs)

```csharp
var server = await LanguageServer.From(options =>
{
    options
        .WithInput(Console.OpenStandardInput())
        .WithOutput(Console.OpenStandardOutput())
        .WithHandler<TextDocumentSyncHandler>()
        .WithHandler<CompletionHandler>()
        .WithHandler<HoverHandler>()
        .WithServices(services =>
        {
            services.AddSingleton<DocumentManager>();
            services.AddSingleton<FunctionManifestLoader>();
        });
});
```

### Document Management

**DocumentManager**: Thread-safe tracking of open documents.

```csharp
public class DocumentManager
{
    private readonly ConcurrentDictionary<DocumentUri, DocumentState> _documents;
    
    public DocumentState? GetDocument(DocumentUri uri);
    public void UpdateDocument(DocumentUri uri, string content, int? version = null);
    public void RemoveDocument(DocumentUri uri);
}
```

**DocumentState**: Per-document state including parsed AST.

```csharp
public class DocumentState
{
    public DocumentUri Uri { get; }
    public string Content { get; }
    public Program? Ast { get; }  // Parsed AST from JEX parser
    public List<DiagnosticInfo> ParseErrors { get; }
    
    public IEnumerable<FunctionInfo> GetUserFunctions();
    public IEnumerable<VariableInfo> GetVariablesAtPosition(int line, int column);
}
```

### Handlers

**CompletionHandler**: Provides IntelliSense completions.

```csharp
public class CompletionHandler : CompletionHandlerBase
{
    protected override Task<CompletionList> Handle(
        CompletionParams request, CancellationToken token)
    {
        // 1. Get document state
        // 2. Determine context (after %, after $, in expression, etc.)
        // 3. Return appropriate completions:
        //    - Keywords (%let, %set, etc.)
        //    - Built-ins ($in, $out, $meta)
        //    - Standard library functions
        //    - User-defined functions
        //    - Variables in scope
        //    - Manifest functions
    }
}
```

**HoverHandler**: Provides hover documentation.

```csharp
public class HoverHandler : HoverHandlerBase
{
    protected override Task<Hover?> Handle(
        HoverParams request, CancellationToken token)
    {
        // 1. Get word at position
        // 2. Look up in keywords, builtins, stdlib, user functions
        // 3. Return formatted Markdown documentation
    }
}
```

**TextDocumentSyncHandler**: Handles document open/change/close.

```csharp
public class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
{
    // On open: Parse document, store state, publish diagnostics
    // On change: Re-parse, update state, publish diagnostics
    // On close: Remove from tracking
}
```

### Adding a New Standard Library Function

Edit `StandardLibraryProvider.cs`:

```csharp
private static readonly List<StdLibFunction> _functions = new()
{
    // ... existing functions ...
    
    // Add new function
    new("myNewFunc", "myNewFunc(arg1, arg2?)", 
        "Description of what this function does.", 
        minArgs: 1, maxArgs: 2),
};
```

---

## VS Code Extension Deep Dive

### Extension Activation

The extension activates when a `.jex` file is opened:

```json
// package.json
"activationEvents": ["onLanguage:jex"]
```

### LSP Client Setup

```typescript
// extension.ts
function startLanguageServer(context: vscode.ExtensionContext): void {
    const serverPath = context.asAbsolutePath(
        path.join('server', 'KoreForge.Jex.LanguageServer.exe')
    );

    const serverOptions: ServerOptions = { command: serverPath };
    
    const clientOptions: LanguageClientOptions = {
        documentSelector: [{ scheme: 'file', language: 'jex' }],
        synchronize: {
            fileEvents: vscode.workspace.createFileSystemWatcher(
                '**/*.jex.functions.json'
            )
        }
    };

    client = new LanguageClient(
        'jexLanguageServer',
        'JEX Language Server',
        serverOptions,
        clientOptions
    );

    client.start();
}
```

### TextMate Grammar

The grammar (`jex.tmLanguage.json`) defines syntax highlighting:

```json
{
    "scopeName": "source.jex",
    "patterns": [
        { "include": "#keywords" },
        { "include": "#strings" },
        { "include": "#builtins" },
        // ...
    ],
    "repository": {
        "keywords": {
            "name": "keyword.control.jex",
            "match": "%(let|set|if|then|else|...)"
        }
    }
}
```

### Snippets

Snippets are defined in `snippets/jex.json`:

```json
{
    "JEX Let Statement": {
        "prefix": ["let", "%let"],
        "body": ["%let ${1:variableName} = ${2:expression};"],
        "description": "Declare a variable"
    }
}
```

---

## Adding New Features

### Adding a New LSP Feature

1. **Create Handler** in `Handlers/`:

```csharp
public class SignatureHelpHandler : SignatureHelpHandlerBase
{
    // Implement Handle method
}
```

2. **Register Handler** in `Program.cs`:

```csharp
options.WithHandler<SignatureHelpHandler>();
```

3. **Add Tests** in test project

4. **Update Documentation**

### Adding a New Snippet

Edit `KoreForge.Jex.VSCode/snippets/jex.json`:

```json
{
    "My New Snippet": {
        "prefix": ["mysnippet"],
        "body": [
            "// Line 1",
            "%let ${1:var} = ${2:value};"
        ],
        "description": "Description"
    }
}
```

### Adding Grammar Support

Edit `KoreForge.Jex.VSCode/syntaxes/jex.tmLanguage.json`:

```json
{
    "repository": {
        "my-new-pattern": {
            "name": "my.scope.name.jex",
            "match": "pattern-regex"
        }
    }
}
```

---

## Code Style and Conventions

### C# Conventions

- Use C# 12 features (file-scoped namespaces, primary constructors)
- Prefer records for immutable data structures
- Use nullable reference types
- Follow Microsoft naming conventions
- Document public APIs with XML comments

### TypeScript Conventions

- Use strict TypeScript settings
- Prefer `const` over `let`
- Use async/await over raw Promises
- Document with JSDoc comments

### Commit Messages

Follow conventional commits:

```
feat: add signature help support
fix: correct completion position calculation
docs: update installation guide
test: add hover handler tests
refactor: extract common completion logic
```

---

## Release Process

### Version Bump

1. Update version in `package.json`
2. Update version in `KoreForge.Jex.LanguageServer.csproj`
3. Update CHANGELOG.md

### Build Release

```powershell
.\scripts\clean.ps1
.\scripts\build.ps1 -Configuration Release
.\scripts\test.ps1
.\scripts\pack.ps1
```

### Verify Package

```powershell
# List package contents
vsce ls --tree

# Test install
code --install-extension .\artifacts\koreforge-jex.vsix
```

### Publish (if publishing to marketplace)

```powershell
vsce publish
```

---

## See Also

- [Installation Guide](Installation-Guide.md)
- [User Guide](User-Guide.md)
- [Language Server Protocol Specification](https://microsoft.github.io/language-server-protocol/)
- [VS Code Extension API](https://code.visualstudio.com/api)
- [OmniSharp Language Server](https://github.com/OmniSharp/csharp-language-server-protocol)
