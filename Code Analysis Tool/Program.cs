using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class Program
{
    private static bool _problemFound = false;

    static void Main()
    {
        Console.WriteLine(".cs File path: ");
        string filepath = Console.ReadLine();

        try
        {
            if (string.IsNullOrEmpty(filepath))
            {
                Console.WriteLine("Invalid filepath");
                return;
            }

            string codeToAnalyze = File.ReadAllText(filepath);
            AnalyzeCode(codeToAnalyze);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    static void Log(string problem, int line, string code)
    {
        Console.WriteLine($"{problem} found on line {line}: {code}");
        _problemFound = true;
    }

    static void AnalyzeCode(string code)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();

        // Public fields
        var publicFields = root.DescendantNodes()
                              .OfType<FieldDeclarationSyntax>()
                              .Where(field => field.Modifiers.Any(SyntaxKind.PublicKeyword));

        foreach (var publicField in publicFields)
        {
            Log("Public field", syntaxTree.GetLineSpan(publicField.Span).StartLinePosition.Line + 1, publicField.ToString());
        }

        // Non readonly public fields
        var nonReadonlyFields = root.DescendantNodes()
                            .OfType<FieldDeclarationSyntax>()
                            .Where(field => field.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                                           !field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword));

        foreach (var field in nonReadonlyFields)
        {
            Log("Public non-readonly field", syntaxTree.GetLineSpan(field.Span).StartLinePosition.Line + 1, field.ToString());
        }


        // Usage of "var" with primitive types
        var varUsages = root.DescendantNodes()
                            .OfType<VariableDeclarationSyntax>()
                            .Where(declaration => declaration.Type.Kind() == SyntaxKind.VarKeyword &&
                                                 declaration.Type is IdentifierNameSyntax identifierType &&
                                                 identifierType.Identifier.Text.IsPrimitiveType());

        foreach (var varUsage in varUsages)
        {
            Log("var used with primitive type", syntaxTree.GetLineSpan(varUsage.Span).StartLinePosition.Line + 1, varUsage.ToString());
        }

        // Methods with long parameter lists
        var methodsWithLongParameters = root.DescendantNodes()
                                            .OfType<MethodDeclarationSyntax>()
                                            .Where(method => method.ParameterList.Parameters.Count > 5);

        foreach (var method in methodsWithLongParameters)
        {
            Log("Method with long parameter list", syntaxTree.GetLineSpan(method.Span).StartLinePosition.Line + 1, method.ToString());
        }

        // Nested if statements that can be simplified
        var nestedIfStatements = root.DescendantNodes()
                                     .OfType<IfStatementSyntax>()
                                     .Where(ifStatement => ifStatement.Statement is BlockSyntax block &&
                                                          block.Statements.Count == 1 &&
                                                          block.Statements.Single() is IfStatementSyntax nestedIf &&
                                                          nestedIf.Else == null);

        foreach (var nestedIf in nestedIfStatements)
        {
            Log("Nested if statement that can be simplified", syntaxTree.GetLineSpan(nestedIf.Span).StartLinePosition.Line + 1, nestedIf.ToString());
        }

        if (!_problemFound)
            Console.WriteLine("No bad practice found");
    }
}

static class Extensions
{
    public static bool IsPrimitiveType(this string typeName)
    {
        return typeName switch
        {
            "int" => true,
            "uint" => true,
            "short" => true,
            "ushort" => true,
            "long" => true,
            "ulong" => true,
            "byte" => true,
            "sbyte" => true,
            "char" => true,
            "float" => true,
            "double" => true,
            "decimal" => true,
            _ => false,
        };
    }
}