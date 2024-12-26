using System.Text.Json;
using DtoToProtoConverter.Models;

namespace DtoToProtoConverter.Services;

public static class ConfigService
{
    public static async Task<ToolConfig> LoadConfigAsync(FileInfo? configFile)
    {
        if (configFile == null || !configFile.Exists)
        {
            return GetDefaultConfig();
        }

        try
        {
            using var fs = configFile.OpenRead();
            var config = await JsonSerializer.DeserializeAsync<ToolConfig>(fs);
            return config ?? GetDefaultConfig();
        }
        catch
        {
            return GetDefaultConfig();
        }
    }

    private static ToolConfig GetDefaultConfig()
    {
        return new ToolConfig
        {
            ProtoPackage = "myproject.infrastructure",
            MapNamespaceToPackage = true,
            SingleFilePerCs = true,
            TypeMappings = new Dictionary<string, string>
            {
                ["int"] = "int32",
                ["long"] = "int64",
                ["string"] = "string",
                ["float"] = "float",
                ["double"] = "double",
                ["bool"] = "bool",
                ["List`1"] = "repeated",
                ["IList`1"] = "repeated",
                ["IEnumerable`1"] = "repeated",
                ["T[]"] = "repeated"
            }
        };
    }
}