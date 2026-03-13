using Xunit;
using KoreForge.Jex;
using KoreForge.Jex.Lexer;

using Newtonsoft.Json.Linq;

namespace KoreForge.Jex.Tests;

public class LexerTests
{
    [Fact]
    public void Lexer_Should_Tokenize_Keywords()
    {
        var lexer = new JexLexer("%let %set %if %then %else %do %end");
        
        Assert.Equal(TokenType.Let, lexer.NextToken().Type);
        Assert.Equal(TokenType.Set, lexer.NextToken().Type);
        Assert.Equal(TokenType.If, lexer.NextToken().Type);
        Assert.Equal(TokenType.Then, lexer.NextToken().Type);
        Assert.Equal(TokenType.Else, lexer.NextToken().Type);
        Assert.Equal(TokenType.Do, lexer.NextToken().Type);
        Assert.Equal(TokenType.End, lexer.NextToken().Type);
        Assert.Equal(TokenType.EOF, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Numbers()
    {
        var lexer = new JexLexer("123 45.67");
        
        var intToken = lexer.NextToken();
        Assert.Equal(TokenType.Integer, intToken.Type);
        Assert.Equal(123L, intToken.Value);
        
        var decToken = lexer.NextToken();
        Assert.Equal(TokenType.Decimal, decToken.Type);
        Assert.Equal(45.67m, decToken.Value);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Strings()
    {
        var lexer = new JexLexer("\"hello world\" \"line\\nbreak\"");
        
        var token1 = lexer.NextToken();
        Assert.Equal(TokenType.String, token1.Type);
        Assert.Equal("hello world", token1.Value);
        
        var token2 = lexer.NextToken();
        Assert.Equal(TokenType.String, token2.Type);
        Assert.Equal("line\nbreak", token2.Value);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Variable_Refs()
    {
        var lexer = new JexLexer("&myVar &another_var");
        
        var token1 = lexer.NextToken();
        Assert.Equal(TokenType.VariableRef, token1.Type);
        Assert.Equal("myVar", token1.Value);
        
        var token2 = lexer.NextToken();
        Assert.Equal(TokenType.VariableRef, token2.Type);
        Assert.Equal("another_var", token2.Value);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Operators()
    {
        var lexer = new JexLexer("== != <= >= && ||");
        
        Assert.Equal(TokenType.Equal, lexer.NextToken().Type);
        Assert.Equal(TokenType.NotEqual, lexer.NextToken().Type);
        Assert.Equal(TokenType.LessOrEqual, lexer.NextToken().Type);
        Assert.Equal(TokenType.GreaterOrEqual, lexer.NextToken().Type);
        Assert.Equal(TokenType.And, lexer.NextToken().Type);
        Assert.Equal(TokenType.Or, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_Should_Skip_Comments()
    {
        var lexer = new JexLexer("123 // this is a comment\n456 /* block comment */ 789");
        
        Assert.Equal(123L, lexer.NextToken().Value);
        Assert.Equal(456L, lexer.NextToken().Value);
        Assert.Equal(789L, lexer.NextToken().Value);
    }

    [Fact]
    public void Lexer_Should_Track_Line_Numbers()
    {
        var lexer = new JexLexer("abc\ndef\nghi");
        
        var t1 = lexer.NextToken();
        Assert.Equal(1, t1.Span.Start.Line);
        
        var t2 = lexer.NextToken();
        Assert.Equal(2, t2.Span.Start.Line);
        
        var t3 = lexer.NextToken();
        Assert.Equal(3, t3.Span.Start.Line);
    }
}
