using System;
using System.Collections.Generic;
using System.IO;
using KoreForge.Jex.Compiler;
using KoreForge.Jex.Parser;

namespace KoreForge.Jex.Library;

/// <summary>
/// Represents a compiled JEX library containing reusable functions.
/// </summary>
public sealed class JexLibrary
{
    private readonly Dictionary<string, FunctionDeclaration> _functions;

    /// <summary>
    /// Gets the name of the library.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the names of all functions in the library.
    /// </summary>
    public IEnumerable<string> FunctionNames => _functions.Keys;

    internal JexLibrary(string name, Dictionary<string, FunctionDeclaration> functions)
    {
        Name = name;
        _functions = functions;
    }

    internal bool TryGetFunction(string name, out FunctionDeclaration? function)
    {
        return _functions.TryGetValue(name, out function);
    }

    /// <summary>
    /// Compiles a JEX library from source code.
    /// </summary>
    /// <param name="name">The name of the library.</param>
    /// <param name="source">The JEX source code containing function definitions.</param>
    /// <returns>A compiled library.</returns>
    public static JexLibrary Compile(string name, string source)
    {
        var compiler = new JexCompiler(null);
        var compiled = compiler.CompileLibrary(source);
        return new JexLibrary(name, compiled);
    }

    /// <summary>
    /// Loads a JEX library from a file.
    /// </summary>
    /// <param name="filePath">Path to the .jex file.</param>
    /// <returns>A compiled library.</returns>
    public static JexLibrary LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Library file not found: {filePath}", filePath);

        var name = Path.GetFileNameWithoutExtension(filePath);
        var source = File.ReadAllText(filePath);
        return Compile(name, source);
    }

    /// <summary>
    /// Loads a JEX library from a stream.
    /// </summary>
    /// <param name="name">The name to assign to the library.</param>
    /// <param name="stream">A stream containing the JEX source code.</param>
    /// <returns>A compiled library.</returns>
    public static JexLibrary LoadFromStream(string name, Stream stream)
    {
        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd();
        return Compile(name, source);
    }

    /// <summary>
    /// Loads a JEX library from a TextReader.
    /// </summary>
    /// <param name="name">The name to assign to the library.</param>
    /// <param name="reader">A TextReader containing the JEX source code.</param>
    /// <returns>A compiled library.</returns>
    public static JexLibrary LoadFromReader(string name, TextReader reader)
    {
        var source = reader.ReadToEnd();
        return Compile(name, source);
    }
}

/// <summary>
/// Manages loaded libraries and provides library resolution.
/// </summary>
public sealed class JexLibraryManager
{
    private readonly Dictionary<string, JexLibrary> _libraries = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a library with this manager.
    /// </summary>
    public void Register(JexLibrary library)
    {
        _libraries[library.Name] = library;
    }

    /// <summary>
    /// Loads and registers a library from a file.
    /// </summary>
    public JexLibrary LoadFromFile(string filePath)
    {
        var library = JexLibrary.LoadFromFile(filePath);
        Register(library);
        return library;
    }

    /// <summary>
    /// Loads and registers a library from a stream.
    /// </summary>
    public JexLibrary LoadFromStream(string name, Stream stream)
    {
        var library = JexLibrary.LoadFromStream(name, stream);
        Register(library);
        return library;
    }

    /// <summary>
    /// Loads and registers a library from source code.
    /// </summary>
    public JexLibrary LoadFromSource(string name, string source)
    {
        var library = JexLibrary.Compile(name, source);
        Register(library);
        return library;
    }

    /// <summary>
    /// Gets all registered libraries.
    /// </summary>
    public IEnumerable<JexLibrary> Libraries => _libraries.Values;

    /// <summary>
    /// Tries to get a library by name.
    /// </summary>
    public bool TryGetLibrary(string name, out JexLibrary? library)
    {
        return _libraries.TryGetValue(name, out library);
    }

    /// <summary>
    /// Tries to find a function by name across all registered libraries.
    /// </summary>
    internal bool TryGetFunction(string name, out FunctionDeclaration? function, out JexLibrary? library)
    {
        foreach (var lib in _libraries.Values)
        {
            if (lib.TryGetFunction(name, out function))
            {
                library = lib;
                return true;
            }
        }
        function = null;
        library = null;
        return false;
    }
}
