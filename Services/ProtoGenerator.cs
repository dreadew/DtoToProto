using System.Text;
using System.Text.RegularExpressions;
using DtoToProtoConverter.Models;
using DtoToProtoConverter.Utils;

namespace DtoToProtoConverter.Services;

public class ProtoGenerator
{
    private readonly ToolConfig _config;
    private readonly Dictionary<string, (string FileName, bool IsEnum)> _fullClassIndex;

    public ProtoGenerator(
        ToolConfig config,
        Dictionary<string, (string FileName, bool IsEnum)> fullClassIndex
    )
    {
        _config = config;
        _fullClassIndex = fullClassIndex;
    }

    public string GenerateProto(ParsedFileResult fileResult)
    {
        var sb = new StringBuilder();
        sb.AppendLine("syntax = \"proto3\";");
        sb.AppendLine();

        // Собираем все импорты
        var neededImports = CollectImports(fileResult);
        foreach (var importFile in neededImports)
        {
            sb.AppendLine($"import \"{importFile}\";");
        }
        if (neededImports.Count > 0)
            sb.AppendLine();
        
        // Выводим package
        if (_config.MapNamespaceToPackage && !string.IsNullOrEmpty(fileResult.NamespaceName))
        {
            var pkgName = NamespaceToPackage(fileResult.NamespaceName);
            sb.AppendLine($"package {pkgName};");
        }
        else
        {
            if (!string.IsNullOrEmpty(_config.ProtoPackage))
                sb.AppendLine($"package {_config.ProtoPackage};");
        }

        sb.AppendLine();

        // Генерируем enum
        foreach (var enm in fileResult.Enums)
        {
            sb.AppendLine($"enum {enm.EnumName} {{");
            for (int i = 0; i < enm.Values.Count; i++)
            {
                var ev = enm.Values[i];
                var enumVal = ev.Value ?? i;
                sb.AppendLine($"    {ev.Name} = {enumVal};");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Генерируем message
        foreach (var cls in fileResult.Classes)
        {
            sb.AppendLine($"message {cls.ClassName} {{");

            for (int i = 0; i < cls.Properties.Count; i++)
            {
                var prop = cls.Properties[i];
                var (protoType, isRepeated, _) = TypeMappingHelper.GetProtoType(
                    prop.TypeName,
                    _fullClassIndex,
                    _config.TypeMappings,
                    fileResult.FileName
                );

                var fieldNumber = i + 1;

                var repeatedPrefix = isRepeated ? "repeated " : "";
                sb.AppendLine($"    {repeatedPrefix}{protoType} {prop.PropertyName} = {fieldNumber};");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Находим все классы/enum, на которые ссылаются поля в данном файле,
    /// но которые объявлены в других .cs файлах и добавляем их импорты в Protobuf файл
    /// </summary>
    private HashSet<string> CollectImports(ParsedFileResult fileResult)
    {
        var imports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var cls in fileResult.Classes)
        {
            foreach (var prop in cls.Properties)
            {
                var (_, _, referencedFile) = TypeMappingHelper.GetProtoType(
                    prop.TypeName,
                    _fullClassIndex,
                    _config.TypeMappings,
                    fileResult.FileName
                );

                if (!string.IsNullOrEmpty(referencedFile) &&
                    !referencedFile.Equals(fileResult.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    var protoFile = Path.GetFileNameWithoutExtension(referencedFile) + ".proto";
                    imports.Add(protoFile);
                }
            }
        }
        
        return imports;
    }

    private string NamespaceToPackage(string csharpNamespace)
    {
        return Regex.Replace(csharpNamespace
            .Replace('/', '_')
            .Replace("domain", "infrastructure", StringComparison.OrdinalIgnoreCase)
            .Replace("dtos", "", StringComparison.OrdinalIgnoreCase)
            .Replace("dto", "", StringComparison.OrdinalIgnoreCase)
            .ToLower(), @"\.{2,}", ".").TrimEnd('.');
    }
}