# JEX VS Code Extension Documentation

Welcome to the documentation for the JEX VS Code Extension. This extension provides rich language support for the JEX (JSON Expression) transformation DSL.

## Features

- **Syntax Highlighting** - Full TextMate grammar for all JEX constructs
- **IntelliSense** - Completions for keywords, functions, variables, and more
- **Hover Information** - Documentation on hover for all language elements
- **Error Diagnostics** - Real-time syntax error detection
- **Code Snippets** - 20+ snippets for common patterns
- **Custom Function Support** - Manifest files for C# registered functions

## Documentation

| Document | Description |
|----------|-------------|
| [Installation Guide](Installation-Guide.md) | How to install, configure, and troubleshoot |
| [User Guide](User-Guide.md) | How to use the extension effectively |
| [Developer Guide](Developer-Guide.md) | Contributing and extending the extension |

## Quick Start

### 1. Install the Extension

```powershell
code --install-extension path/to/koreforge-jex.vsix
```

### 2. Create a JEX File

Create a file with `.jex` extension and start writing:

```jex
// Transform customer data
%let name = jp1($in, "$.customer.name");
%set $.greeting = concat("Hello, ", name, "!");
```

### 3. Explore Features

- Type `%` to see keyword completions
- Type `$` to see built-in variables
- Hover over functions to see documentation
- Type `foreach` + Tab to expand a snippet

## Architecture

```
┌─────────────────────┐     LSP      ┌─────────────────────┐
│   VS Code Client    │ ◄──────────► │   Language Server   │
│   (TypeScript)      │   (stdio)    │   (C# / .NET 10)    │
├─────────────────────┤              ├─────────────────────┤
│ • LSP Client        │              │ • Completion        │
│ • TextMate Grammar  │              │ • Hover             │
│ • Snippets          │              │ • Diagnostics       │
│ • Configuration     │              │ • Document Sync     │
└─────────────────────┘              └─────────────────────┘
                                              │
                                              ▼
                                     ┌─────────────────────┐
                                     │    JEX Parser       │
                                     │   (KoreForge.Jex)       │
                                     └─────────────────────┘
```

## Project Structure

```
KoreForge.Jex/
├── src/KoreForge.Jex.LanguageServer/   # C# Language Server
├── tests/KoreForge.Jex.LanguageServer.Tests/
├── KoreForge.Jex.VSCode/               # VS Code Extension
│   ├── src/                        # TypeScript source
│   ├── syntaxes/                   # TextMate grammar
│   ├── snippets/                   # Code snippets
│   └── schemas/                    # JSON schemas
├── scripts/                        # Build scripts
├── docs/                           # Documentation
└── artifacts/                      # Build outputs
```

## Building

```powershell
# Build everything
.\scripts\build.ps1

# Run tests
.\scripts\test.ps1

# Create VSIX package
.\scripts\pack.ps1

# Clean artifacts
.\scripts\clean.ps1
```

## Requirements

- Visual Studio Code 1.85.0+
- .NET 10.0 Runtime
- Node.js 18+ (for building)

## Contributing

See the [Developer Guide](Developer-Guide.md) for information on:
- Setting up the development environment
- Building and testing
- Adding new features
- Code style conventions

## License

MIT License - See [LICENSE.md](../LICENSE.md)
