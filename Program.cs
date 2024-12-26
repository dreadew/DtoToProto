using System.CommandLine;
using DtoToProtoConverter.Services;

namespace DtoToProtoConverter;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var dtoPathOption = new Option<DirectoryInfo>(
            "--dtoPath",
            "Путь к папке с DTO-классами"
        )
        {
            IsRequired = true
        };

        var outputPathOption = new Option<DirectoryInfo>(
            "--outputPath",
            "Путь, куда будут сохраняться protobuf файлы"
        )
        {
            IsRequired = true
        };

        var configPathOption = new Option<FileInfo>(
            "--configPath",
            "Путь к JSON-файлу конфигурации"
        )
        {
            IsRequired = false
        };

        var rootCommand = new RootCommand("DTO to Protobuf converter");
        rootCommand.AddOption(dtoPathOption);
        rootCommand.AddOption(outputPathOption);
        rootCommand.AddOption(configPathOption);

        rootCommand.SetHandler(
            async (dtoDir, outputDir, configFile) =>
            {
                var config = await ConfigService.LoadConfigAsync(configFile);

                var parser = new RoslynDtoParser(config);
                var parsedResults = parser.ParseDtos(dtoDir.FullName);

                var fileNameIndex = parsedResults.ToDictionary(
                    r => r.FileName,
                    r => r
                );

                var fullClassIndex = BuildFullClassIndex(parsedResults);

                var generator = new ProtoGenerator(config, fullClassIndex);
                
                if (!outputDir.Exists)
                    outputDir.Create();

                foreach (var fileResult in parsedResults)
                {
                    var protoText = generator.GenerateProto(fileResult);

                    var protoFileName = Path.GetFileNameWithoutExtension(fileResult.FileName) + ".proto";
                    var fullOutputPath = Path.Combine(outputDir.FullName, protoFileName);

                    await File.WriteAllTextAsync(fullOutputPath, protoText);
                    Console.WriteLine($"Generated: {fullOutputPath}");
                }
            },
            dtoPathOption,
            outputPathOption,
            configPathOption
        );

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Создаем словарь: "Полное имя класса (Namespace + Class)" -> ParsedFileResult (где объявлен класс)
    /// </summary>
    private static Dictionary<string, (string FileName, bool IsEnum)> BuildFullClassIndex(
        List<ParsedFileResult> allFiles
    )
    {
        var dict = new Dictionary<string, (string FileName, bool IsEnum)>(StringComparer.OrdinalIgnoreCase);

        foreach (var fileResult in allFiles)
        {
            // Для классов
            foreach (var cls in fileResult.Classes)
            {
                var fullName = $"{fileResult.NamespaceName}.{cls.ClassName}";
                fullName = fullName.TrimStart('.');
                if (!dict.ContainsKey(fullName))
                {
                    dict[fullName] = (fileResult.FileName, false);
                }
            }

            // Для enum
            foreach (var enm in fileResult.Enums)
            {
                var fullName = $"{fileResult.NamespaceName}.{enm.EnumName}".TrimStart('.');
                if (!dict.ContainsKey(fullName))
                {
                    dict[fullName] = (fileResult.FileName, true);
                }
            }
        }

        return dict;
    }
}