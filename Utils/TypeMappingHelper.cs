using System.Text;
using System.Text.RegularExpressions;

namespace DtoToProtoConverter.Utils;

public static class TypeMappingHelper
{
    /// <summary>
    /// Возвращает (protoType, isRepeated)
    /// Пример: (int32, false) или (int32, true) для List<int>
    /// </summary>
    public static (string ProtoType, bool IsRepeated, string? referencedFile) GetProtoType(
        string csharpType,
        Dictionary<string, (string FileName, bool IsEnum)> fullClassIndex,
        Dictionary<string, string>? typeMappings,
        string? currentFile = null
    )
    {
        if (typeMappings == null)
            typeMappings = new Dictionary<string, string>();

        bool isRepeated = false;

        csharpType = RemoveNamespaces(csharpType);
        
        // Обработка массива
        if (csharpType.EndsWith("[]"))
        {
            isRepeated = true;
            var elementType = csharpType[..^2];
            var (pt, _, rf) = GetProtoType(elementType, fullClassIndex, typeMappings, currentFile);
            return (pt, true, rf);
        }
        
        // Обработка generic
        if (csharpType.Contains('<') && csharpType.Contains('>'))
        {
            var mainType = csharpType.Substring(0, csharpType.IndexOf('<'));
            var innerType = csharpType[(csharpType.IndexOf('<') + 1) .. csharpType.LastIndexOf('>')];

            if (typeMappings.ContainsKey(mainType + "`1"))
            {
                isRepeated = true;
            }

            var (innerProto, _, innerRf) = GetProtoType(innerType, fullClassIndex, typeMappings, currentFile);
            return (innerProto, true, innerRf);
        }

        // Обработка nullable
        bool isNullable = false;
        if (csharpType.EndsWith("?"))
        {
            isNullable = true;
            csharpType = csharpType.TrimEnd('?');
        }

        // Обработка DateTime
        if (csharpType == "DateTime")
        {
            return ("google.protobuf.Timestamp", false, "google/protobuf/timestamp.proto");
        }

        if (typeMappings.TryGetValue(csharpType, out var mappedProto))
        {
            if (isNullable)
            {
                const string wrappersPkg = "google/protobuf/wrappers.proto";
                var propType = new StringBuilder("google.protobuf");
                
                switch (csharpType)
                {
                    case "string":
                        propType.Append(".StringValue");
                        break;
                    case "int":
                        propType.Append(".Int32Value");
                        break;
                    case "long":
                        propType.Append(".Int64Value");
                        break;
                    case "bool":
                        propType.Append(".BoolValue");
                        break;
                    default:
                        propType.Append(".StringValue");
                        break;
                }

                return (propType.ToString(), false, wrappersPkg);
            }

            return (mappedProto, isRepeated, null);
        }

        // Проверка на класс/enum
        var match = fullClassIndex
            .Where(kvp => kvp.Key.EndsWith("." + csharpType) || kvp.Key == csharpType)
            .Select(kvp => (FullName: kvp.Key, FileName: kvp.Value.FileName, IsEnum: kvp.Value.IsEnum))
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(match.FullName))
        {
            string? rf = (currentFile != null && match.FileName.Equals(currentFile, StringComparison.OrdinalIgnoreCase))
                ? null
                : match.FileName;
            
            return (csharpType, isRepeated, rf);
        }

        return ("string", isRepeated, null);
    }

    private static string RemoveNamespaces(string typeName)
    {
        var lessIdx = typeName.IndexOf('<');
        if (lessIdx > 0)
        {
            var mainPart = typeName.Substring(0, lessIdx);
            var genericPart = typeName.Substring(lessIdx);

            mainPart = mainPart.Substring(mainPart.LastIndexOf('.') + 1);
            genericPart = genericPart
                .Replace("System.", "")
                .Replace("Collections.Generic.", "")
                .Replace("MyProject.DTO.", "")
                .Replace("MyProject.Domain.Dto.", "");
            
            return mainPart + genericPart;
        }
        
        var lastDot = typeName.LastIndexOf('.');
        if (lastDot >= 0)
        {
            return typeName.Substring(lastDot + 1);
        }
        return typeName;
    }
}