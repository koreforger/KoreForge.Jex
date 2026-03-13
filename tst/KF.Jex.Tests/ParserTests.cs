using Xunit;
using KoreForge.Jex.Parser;

namespace KoreForge.Jex.Tests;

public class ParserTests
{
    [Fact]
    public void Parser_Should_Parse_Let_Statement()
    {
        var parser = new JexParser("%let x = 42;");
        var program = parser.Parse();
        
        Assert.Single(program.Statements);
        var let = Assert.IsType<LetStatement>(program.Statements[0]);
        Assert.Equal("x", let.VariableName);
        var num = Assert.IsType<NumberLiteral>(let.Value);
        Assert.Equal(42m, num.Value);
    }

    [Fact]
    public void Parser_Should_Parse_String_Literal()
    {
        var parser = new JexParser("%let msg = \"hello\";");
        var program = parser.Parse();
        
        var let = Assert.IsType<LetStatement>(program.Statements[0]);
        var str = Assert.IsType<StringLiteral>(let.Value);
        Assert.Equal("hello", str.Value);
    }

    [Fact]
    public void Parser_Should_Parse_Binary_Expression()
    {
        var parser = new JexParser("%let x = 1 + 2 * 3;");
        var program = parser.Parse();
        
        var let = Assert.IsType<LetStatement>(program.Statements[0]);
        var add = Assert.IsType<BinaryExpression>(let.Value);
        Assert.Equal("+", add.Operator);
        
        // 1 + (2 * 3) due to precedence
        var left = Assert.IsType<NumberLiteral>(add.Left);
        Assert.Equal(1m, left.Value);
        
        var mul = Assert.IsType<BinaryExpression>(add.Right);
        Assert.Equal("*", mul.Operator);
    }

    [Fact]
    public void Parser_Should_Parse_Function_Call()
    {
        var parser = new JexParser("%let x = myFunc(1, 2);");
        var program = parser.Parse();
        
        var let = Assert.IsType<LetStatement>(program.Statements[0]);
        var call = Assert.IsType<FunctionCall>(let.Value);
        Assert.Equal("myFunc", call.Name);
        Assert.Equal(2, call.Arguments.Count);
    }

    [Fact]
    public void Parser_Should_Parse_If_Statement()
    {
        var parser = new JexParser(@"
            %if (true) %then %do;
                %let x = 1;
            %end;
        ");
        var program = parser.Parse();
        
        var ifStmt = Assert.IsType<IfStatement>(program.Statements[0]);
        var cond = Assert.IsType<BooleanLiteral>(ifStmt.Condition);
        Assert.True(cond.Value);
        Assert.Single(ifStmt.ThenBlock);
        Assert.Null(ifStmt.ElseBlock);
    }

    [Fact]
    public void Parser_Should_Parse_If_Else_Statement()
    {
        var parser = new JexParser(@"
            %if (false) %then %do;
                %let x = 1;
            %end;
            %else %do;
                %let x = 2;
            %end;
        ");
        var program = parser.Parse();
        
        var ifStmt = Assert.IsType<IfStatement>(program.Statements[0]);
        Assert.NotNull(ifStmt.ElseBlock);
        Assert.Single(ifStmt.ElseBlock);
    }

    [Fact]
    public void Parser_Should_Parse_Foreach_Statement()
    {
        var parser = new JexParser(@"
            %foreach item %in arr() %do;
                %let x = &item;
            %end;
        ");
        var program = parser.Parse();
        
        var foreachStmt = Assert.IsType<ForeachStatement>(program.Statements[0]);
        Assert.Equal("item", foreachStmt.IteratorName);
        Assert.Single(foreachStmt.Body);
    }

    [Fact]
    public void Parser_Should_Parse_Variable_Reference()
    {
        var parser = new JexParser("%let x = &y;");
        var program = parser.Parse();
        
        var let = Assert.IsType<LetStatement>(program.Statements[0]);
        var varRef = Assert.IsType<VariableRef>(let.Value);
        Assert.Equal("y", varRef.Name);
    }

    [Fact]
    public void Parser_Should_Parse_BuiltIn_Variable()
    {
        var parser = new JexParser("%let x = $in;");
        var program = parser.Parse();
        
        var let = Assert.IsType<LetStatement>(program.Statements[0]);
        var builtIn = Assert.IsType<BuiltInVariable>(let.Value);
        Assert.Equal("$in", builtIn.Name);
    }

    [Fact]
    public void Parser_Should_Parse_Json_Object_Literal()
    {
        var parser = new JexParser("%let x = { \"a\": 1, \"b\": 2 };");
        var program = parser.Parse();
        
        var let = Assert.IsType<LetStatement>(program.Statements[0]);
        var obj = Assert.IsType<JsonObjectLiteral>(let.Value);
        Assert.Equal(2, obj.Properties.Count);
    }

    [Fact]
    public void Parser_Should_Parse_Json_Array_Literal()
    {
        var parser = new JexParser("%let x = [1, 2, 3];");
        var program = parser.Parse();
        
        var let = Assert.IsType<LetStatement>(program.Statements[0]);
        var arr = Assert.IsType<JsonArrayLiteral>(let.Value);
        Assert.Equal(3, arr.Elements.Count);
    }
}
