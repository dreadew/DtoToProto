using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DtoToProtoConverter.Models;

namespace DtoToProtoConverter.Services;

/// <summary>
/// DTO для описания одного файла, внутри которого могут быть несколько классов/enum.
/// </summary>
public class ParsedFileResult
{
    public string FileName { get; set; } = "";
    public string? NamespaceName { get; set; }
    public List<ParsedClass> Classes { get; set; } = new();
    public List<ParsedEnum> Enums { get; set; } = new();
}

public class ParsedClass
{
    public string ClassName { get; set; } = "";
    public List<ParsedProperty> Properties { get; set; } = new();
}

public class ParsedEnum
{
    public string EnumName { get; set; } = "";
    public List<ParsedEnumValue> Values { get; set; } = new();
}

public class ParsedEnumValue
{
    public string Name { get; set; } = "";
    public int? Value { get; set; }
}

public class ParsedProperty
{
    public string PropertyName { get; set; } = "";
    public string TypeName { get; set; } = "";
}

public class RoslynDtoParser
{
    private readonly ToolConfig _config;
    
    public RoslynDtoParser(ToolConfig config)
    {
        _config = config;
    }

    public List<ParsedFileResult> ParseDtos(string dtoPath)
    {
        var result = new List<ParsedFileResult>();
        var csFiles = Directory.GetFiles(dtoPath, "*.cs", SearchOption.AllDirectories);

        foreach (var csFile in csFiles)
        {
            var content = File.ReadAllText(csFile);
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = tree.GetCompilationUnitRoot();

            var namespaceNode = root.DescendantNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault();
            
            var fileResult = new ParsedFileResult
            {
                FileName = Path.GetFileName(csFile),
                NamespaceName = namespaceNode?.Name.ToString()
            };

            var classNodes = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>();
            foreach (var classNode in classNodes)
            {
                var parsedClass = new ParsedClass
                {
                    ClassName = classNode.Identifier.Text
                };

                var propertyNodes = classNode.Members
                    .OfType<PropertyDeclarationSyntax>();
                
                foreach (var propertyNode in propertyNodes)
                {
                    var propType = propertyNode.Type.ToString();
                    parsedClass.Properties.Add(new ParsedProperty
                    {
                        PropertyName = propertyNode.Identifier.Text,
                        TypeName = propType
                    });
                }

                fileResult.Classes.Add(parsedClass);
            }

            var enumNodes = root.DescendantNodes()
                .OfType<EnumDeclarationSyntax>();
            foreach (var enumNode in enumNodes)
            {
                var parsedEnum = new ParsedEnum
                {
                    EnumName = enumNode.Identifier.Text
                };

                foreach (var member in enumNode.Members)
                {
                    int? val = null;
                    if (member.EqualsValue?.Value is LiteralExpressionSyntax literalSyntax
                        && literalSyntax.Token.Value is int intVal)
                    {
                        val = intVal;
                    }

                    parsedEnum.Values.Add(new ParsedEnumValue
                    {
                        Name = member.Identifier.Text,
                        Value = val
                    });
                }

                fileResult.Enums.Add(parsedEnum);
            }

            if (fileResult.Classes.Count > 0 || fileResult.Enums.Count > 0)
                result.Add(fileResult);
        }

        return result;
    }
}