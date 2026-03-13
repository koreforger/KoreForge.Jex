# JEX VS Code Extension - Installation Guide

This guide covers how to install and configure the JEX VS Code extension for syntax highlighting, autocompletion, and Language Server features.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation Methods](#installation-methods)
  - [Install from VSIX](#install-from-vsix)
  - [Install from Source](#install-from-source)
- [Configuration](#configuration)
- [Verifying Installation](#verifying-installation)
- [Troubleshooting](#troubleshooting)
- [Uninstallation](#uninstallation)

---

## Prerequisites

### For Using the Extension

- **Visual Studio Code** 1.85.0 or later
- **.NET 10.0 Runtime** (required for the Language Server)

### For Building from Source

- **Node.js** 18.x or later
- **npm** 9.x or later
- **.NET 10.0 SDK**
- **PowerShell** 7.x or later (for build scripts)

---

## Installation Methods

### Install from VSIX

The easiest way to install the extension is from a pre-built `.vsix` file.

#### Option 1: Command Line

```powershell
code --install-extension path\to\koreforge-jex.vsix
```

#### Option 2: VS Code UI

1. Open VS Code
2. Press `Ctrl+Shift+P` to open the Command Palette
3. Type "Extensions: Install from VSIX..."
4. Navigate to and select the `koreforge-jex.vsix` file
5. Reload VS Code when prompted

### Install from Source

If you want to build and install from source:

```powershell
# Clone or navigate to the repository
cd C:\My\KoreForge\KoreForge.Jex

# Build everything
.\scripts\build.ps1

# Create the VSIX package
.\scripts\pack.ps1

# Install the extension
code --install-extension .\artifacts\koreforge-jex.vsix
```

---

## Configuration

The extension can be configured through VS Code settings. Access settings via:
- `File > Preferences > Settings` (Windows/Linux)
- `Code > Preferences > Settings` (macOS)
- Or press `Ctrl+,`

Search for "JEX" to see all available settings.

### Available Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `jex.languageServer.enabled` | boolean | `true` | Enable/disable the Language Server for advanced features |
| `jex.languageServer.path` | string | `""` | Custom path to Language Server executable (uses bundled if empty) |
| `jex.functionManifest.paths` | array | `[]` | Additional paths to search for function manifest files |

### Example settings.json

```json
{
    "jex.languageServer.enabled": true,
    "jex.functionManifest.paths": [
        "C:/MyProject/functions",
        "${workspaceFolder}/jex-functions"
    ]
}
```

### Function Manifests

If your application registers custom C# functions with the JEX runtime, you can provide a manifest file so the extension knows about them:

1. Create a file named `*.jex.functions.json` in your project
2. Add it to the `jex.functionManifest.paths` setting or place it in your workspace

Example manifest:

```json
{
    "$schema": "https://raw.githubusercontent.com/koreforger/KoreForge.Jex/main/KoreForge.Jex.VSCode/schemas/jex.functions.schema.json",
    "functions": [
        {
            "name": "myCustomFunction",
            "description": "Does something custom",
            "signature": "myCustomFunction(input, options?)",
            "parameters": [
                { "name": "input", "type": "any", "description": "The input value" },
                { "name": "options", "type": "object", "optional": true }
            ],
            "returnType": "any"
        }
    ]
}
```

---

## Verifying Installation

After installation, verify everything is working:

### 1. Check Extension is Active

1. Create a new file with `.jex` extension
2. You should see "JEX" in the VS Code status bar (bottom right)
3. Syntax highlighting should be visible

### 2. Check Language Server

1. Open a `.jex` file
2. Type `%` - you should see completion suggestions
3. Hover over a keyword like `%let` - you should see documentation

### 3. Check Output Logs

1. Open the Output panel (`View > Output` or `Ctrl+Shift+U`)
2. Select "JEX Language Server" from the dropdown
3. Look for "JEX Language Server started" message

---

## Troubleshooting

### Extension Not Activating

**Symptoms**: No syntax highlighting, no completions

**Solutions**:
1. Ensure the file has `.jex` extension
2. Check that the extension is enabled in Extensions view
3. Reload VS Code window (`Ctrl+Shift+P` > "Reload Window")

### Language Server Not Starting

**Symptoms**: Syntax highlighting works but no completions/hover

**Solutions**:
1. Ensure .NET 10.0 runtime is installed: `dotnet --list-runtimes`
2. Check `jex.languageServer.enabled` is `true`
3. Check Output panel for error messages
4. Try specifying explicit path in `jex.languageServer.path`

### Custom Functions Not Appearing

**Symptoms**: Your C# registered functions don't show in completions

**Solutions**:
1. Verify manifest file is valid JSON
2. Check file name ends with `.jex.functions.json`
3. Ensure path is in `jex.functionManifest.paths` or in workspace
4. Reload window after adding new manifests

### Performance Issues

**Symptoms**: Slow completions, high CPU usage

**Solutions**:
1. Disable Language Server if not needed: `jex.languageServer.enabled: false`
2. Close unused JEX files
3. Check for very large JEX files (>10,000 lines)

### .NET Runtime Not Found

**Error**: "Could not find .NET runtime"

**Solution**:
```powershell
# Install .NET 10.0 runtime
winget install Microsoft.DotNet.Runtime.10
```

Or download from: https://dotnet.microsoft.com/download/dotnet/10.0

---

## Uninstallation

### Via Command Line

```powershell
code --uninstall-extension KoreForge.jex
```

### Via VS Code UI

1. Open Extensions view (`Ctrl+Shift+X`)
2. Find "KoreForge JEX" in installed extensions
3. Click the gear icon and select "Uninstall"
4. Reload VS Code

### Clean Uninstall

To remove all extension data:

```powershell
# Windows
Remove-Item -Recurse "$env:USERPROFILE\.vscode\extensions\KoreForge.koreforge-jex-*"

# Also remove settings if desired
# Edit settings.json and remove all "jex.*" entries
```

---

## Next Steps

- Read the [User Guide](User-Guide.md) to learn how to use JEX effectively
- Check the [Developer Guide](Developer-Guide.md) if you want to contribute
