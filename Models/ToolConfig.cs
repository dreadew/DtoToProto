namespace DtoToProtoConverter.Models;

public class ToolConfig
{
    /// <summary>
    /// Маппинг типов из C# в protobuf
    /// Ключи: "int", "System.String", "List`1" и т.п.
    /// Значения: "int32", "string", "repeated" и т.д.
    /// </summary>
    public Dictionary<string, string>? TypeMappings { get; set; }

    /// <summary>
    /// Дополнительные настройки package в protobuf
    /// </summary>
    public string? ProtoPackage { get; set; } = "default_package";

    /// <summary>
    /// Маппинг namespace в protobuf
    /// </summary>
    public bool MapNamespaceToPackage { get; set; } = true;

    /// <summary>
    /// True - генерация нескольких сообщений в одном protobuf файле (если в .cs несколько классов)
    /// False - разделение по классам
    /// </summary>
    public bool SingleFilePerCs { get; set; } = true;
}