using PostProcessor.Core.Config.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PostProcessor.Core.Config.Loaders;

/// <summary>
/// Загрузчик конфигураций из JSON файлов
/// Поддерживает валидацию и дефолтные значения согласно стандартам контроллеров
/// </summary>
public static class JsonConfigLoader
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Загрузка конфигурации контроллера из JSON файла
    /// </summary>
    public static ControllerConfig LoadController(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Controller config not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<ControllerConfig>(json, _options);

        if (config == null)
            throw new InvalidOperationException($"Failed to deserialize controller config: {filePath}");

        // Валидация обязательных полей
        ValidateControllerConfig(config, filePath);

        // Применение дефолтных значений для отсутствующих регистров
        ApplyDefaultRegisterFormats(config);

        return config;
    }

    /// <summary>
    /// Загрузка конфигурации станка из JSON файла
    /// </summary>
    public static MachineConfig LoadMachine(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Machine config not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<MachineConfig>(json, _options);

        if (config == null)
            throw new InvalidOperationException($"Failed to deserialize machine config: {filePath}");

        return config;
    }

    private static void ValidateControllerConfig(ControllerConfig config, string filePath)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
            throw new InvalidOperationException($"Controller name is required in {filePath}");

        // Минимальный набор регистров для фрезерного станка
        var requiredRegisters = new[] { "X", "Y", "Z", "F", "S" };
        foreach (var reg in requiredRegisters)
        {
            if (!config.RegisterFormats.ContainsKey(reg))
                throw new InvalidOperationException(
                    $"Required register '{reg}' missing in controller config: {filePath}");
        }

        // Минимальный набор функций
        var requiredFunctions = new[] { "rapid", "linear", "spindle_cw", "coolant_flood", "program_end" };
        foreach (var func in requiredFunctions)
        {
            if (!config.FunctionCodes.ContainsKey(func))
                throw new InvalidOperationException(
                    $"Required function '{func}' missing in controller config: {filePath}");
        }
    }

    private static void ApplyDefaultRegisterFormats(ControllerConfig config)
    {
        // Стандартные форматы для фрезерных станков (соответствуют спецификации Fanuc)
        var defaults = new Dictionary<string, RegisterFormat>
        {
            ["X"] = new RegisterFormat { Address = "X", Format = "F4.3", IsModal = true },
            ["Y"] = new RegisterFormat { Address = "Y", Format = "F4.3", IsModal = true },
            ["Z"] = new RegisterFormat { Address = "Z", Format = "F4.3", IsModal = true },
            ["A"] = new RegisterFormat { Address = "A", Format = "F3.2", IsModal = true },
            ["B"] = new RegisterFormat { Address = "B", Format = "F3.2", IsModal = true },
            ["C"] = new RegisterFormat { Address = "C", Format = "F3.2", IsModal = true },
            ["F"] = new RegisterFormat { Address = "F", Format = "F3.1", IsModal = false },
            ["S"] = new RegisterFormat { Address = "S", Format = "F0", IsModal = false },
            ["T"] = new RegisterFormat { Address = "T", Format = "F0", IsModal = false }
        };

        foreach (var (key, def) in defaults)
        {
            if (!config.RegisterFormats.ContainsKey(key))
                config.RegisterFormats[key] = def;
        }
    }

    /// <summary>
    /// Поиск конфигурации контроллера по имени в каталоге
    /// </summary>
    public static string FindControllerConfig(string controllerName, string basePath)
    {
        var searchPaths = new[]
        {
            Path.Combine(basePath, "controllers", controllerName.ToLowerInvariant(), $"{controllerName.ToLowerInvariant()}.json"),
            Path.Combine(basePath, "controllers", $"{controllerName.ToLowerInvariant()}.json"),
            Path.Combine(basePath, "controllers", "fanuc", $"{controllerName.ToLowerInvariant()}.json"),
            Path.Combine(basePath, "controllers", "heidenhain", $"{controllerName.ToLowerInvariant()}.json"),
            Path.Combine(basePath, "controllers", "siemens", $"{controllerName.ToLowerInvariant()}.json")
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException(
            $"Controller config not found for '{controllerName}'. Searched in: {basePath}/controllers/");
    }
}
