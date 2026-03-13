using System.Collections.Generic;
using KoreForge.Jex.Parser;

namespace KoreForge.Jex.Compiler;

/// <summary>
/// Compiled JEX program that can be executed.
/// This serves as both the compilation result and execution model.
/// The AST is walked directly during interpretation (tree-walking interpreter).
/// </summary>
public sealed class CompiledProgram
{
    public Program Ast { get; }
    public Dictionary<string, FunctionDeclaration> UserFunctions { get; }

    internal CompiledProgram(Program ast, Dictionary<string, FunctionDeclaration> userFunctions)
    {
        Ast = ast;
        UserFunctions = userFunctions;
    }
}

/// <summary>
/// Compiles JEX source code into a compiled program.
/// </summary>
public sealed class JexCompiler
{
    private readonly JexCompileOptions _options;

    public JexCompiler(JexCompileOptions? options = null)
    {
        _options = options ?? JexCompileOptions.Default;
    }

    /// <summary>
    /// Compiles the given JEX script into an executable program.
    /// </summary>
    public CompiledProgram Compile(string script)
    {
        var parser = new JexParser(script);
        var ast = parser.Parse();

        // Extract user-defined functions
        var userFunctions = new Dictionary<string, FunctionDeclaration>();
        foreach (var stmt in ast.Statements)
        {
            if (stmt is FunctionDeclaration funcDecl)
            {
                if (!_options.AllowUserFunctions)
                {
                    throw new JexCompileException("User-defined functions are not allowed", funcDecl.Span);
                }
                if (userFunctions.ContainsKey(funcDecl.Name))
                {
                    throw new JexCompileException($"Function '{funcDecl.Name}' is already defined", funcDecl.Span);
                }
                userFunctions[funcDecl.Name] = funcDecl;
            }
        }

        // Perform semantic analysis if in strict mode
        if (_options.Strict)
        {
            PerformSemanticAnalysis(ast, userFunctions);
        }

        return new CompiledProgram(ast, userFunctions);
    }

    /// <summary>
    /// Compiles a library source file, extracting only function definitions.
    /// </summary>
    public Dictionary<string, FunctionDeclaration> CompileLibrary(string source)
    {
        var parser = new JexParser(source);
        var ast = parser.Parse();

        var functions = new Dictionary<string, FunctionDeclaration>();
        foreach (var stmt in ast.Statements)
        {
            if (stmt is FunctionDeclaration funcDecl)
            {
                if (functions.ContainsKey(funcDecl.Name))
                {
                    throw new JexCompileException($"Function '{funcDecl.Name}' is already defined in library", funcDecl.Span);
                }
                functions[funcDecl.Name] = funcDecl;
            }
            else
            {
                // Libraries can only contain function definitions
                throw new JexCompileException(
                    "Libraries can only contain function definitions. Use %func ... %endfunc to define functions.",
                    stmt.Span);
            }
        }

        if (functions.Count == 0)
        {
            throw new JexCompileException("Library contains no function definitions", ast.Span);
        }

        return functions;
    }

    private void PerformSemanticAnalysis(Program ast, Dictionary<string, FunctionDeclaration> userFunctions)
    {
        // Basic semantic checks can be added here:
        // - Check for undefined variables (if we want to be strict)
        // - Check function arity
        // For now, we keep it simple and do runtime checks
    }
}
