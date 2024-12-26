Это консольное приложение (и .NET Tool), конвертирующее C# DTO-классы в Protobuf (*.proto*)-файлы.

## Возможности

- Парсит DTO классы и enum с помощью [Roslyn](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk).
- Учитывает списки/массивы (`List<T>` / `T[]` \-> `repeated`).
- Добавляет `import` при ссылках на DTO из других файлов.
- Поддерживает `DateTime -> google.protobuf.Timestamp`.
- Поддерживает nullable примитивы (`string?`, `int?`, `bool?`, ...) \-> `google.protobuf.*Value`.
- Генерирует несколько сообщений (классов/enum) в одном \*.proto, если они в одном `.cs`-файле.
- Позволяет задавать или переопределять маппинги типов и прочие настройки через **JSON-конфиг**.

## Установка

### 1. Установка из NuGet (глобально)

1. Скачайте пакет `Dto2Proto` в **NuGet**.  
2. Установите инструмент глобально:

Команда будет доступна через:
```bash
dotnet tool install --global Dto2Proto --version 1.0
```

### 2. Установка локально

```bash
cd MySolutionFolder
dotnet new tool-manifest
dotnet tool install Dto2Proto --version 1.0
```

Команда будет доступна через:
```bash
dotnet tool run dto2proto -- --dtoPath ... --outputPath ... --configPath ...
```

или более короткий вариант со стандартным конфигом:
```bash
dotnet tool run dto2proto -- --dtoPath ... --outputPath ...
```

### 3. Пример запуска (глобально)

```bash
dto2proto \
    --dtoPath "C:/MyApp/src//Domain/DTO \
    --outputPath "C:/MyApp/src/Infrastructure/Protos
```

### 4. Пример запуска (локально)

```bash
dotnet tool run dto2proto -- \
    --dtoPath "./src/Domain/Dto" \
    --outputPath "./src/Infrastructure/Protos"
```

### 5. Настройки дефолтного конфига (если --configPath не указан)

```json
{
  "ProtoPackage": "myproject.infrastructure",
  "MapNamespaceToPackage": true,
  "SingleFilePerCs": true,
  "TypeMappings": {
    "int": "int32",
    "long": "int64",
    "float": "float",
    "double": "double",
    "bool": "bool",
    "string": "string",
    // Списки/массивы
    "List`1": "repeated",
    "IEnumerable`1": "repeated",
    "IList`1": "repeated",
    "T[]": "repeated"
  }
}
```

Описание:

- ProtoPackage: пакет, который будет выведен в .proto при отсутствии namespace или если MapNamespaceToPackage = false.
- MapNamespaceToPackage: если true, namespace MyProject.Domain.DTO -> package myproject_domain_dto;. Если нет, используется ProtoPackage.
- SingleFilePerCs: генерировать один *.proto на каждый *.cs-файл.
- TypeMappings: словарь, указывающий, как конвертировать C#-тип в proto-тип.

Дополнительно в коде заложена логика:

- DateTime -> google.protobuf.Timestamp
- nullable примитивы (int?, string?, bool? и т.д.) -> соответствующие wrapper-типы (google.protobuf.Int32Value, google.protobuf.StringValue, ...)