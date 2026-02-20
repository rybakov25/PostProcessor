using System.Text.Json;
using System.Text.Json.Serialization;

namespace PostProcessor.Core.Config.Models;

/// <summary>
/// Модель конфигурации контроллера ЧПУ
/// Используется для настройки постпроцессора под конкретный станок
/// </summary>
public record ControllerConfig
{
    /// <summary>
    /// Имя контроллера (например, "Fanuc 31i")
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Тип станка: milling, turning, millturn, edm
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MachineType MachineType { get; init; } = MachineType.Milling;

    /// <summary>
    /// Версия контроллера
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Форматы регистров (X, Y, Z, F, S, T, A, B, C...)
    /// </summary>
    public Dictionary<string, RegisterFormat> RegisterFormats { get; init; } = new();

    /// <summary>
    /// Карта функций (G/M коды) и их значения
    /// </summary>
    public Dictionary<string, FunctionCode> FunctionCodes { get; init; } = new();

    /// <summary>
    /// Рабочие системы координат (G54-G59)
    /// </summary>
    public WorkCoordinateSystem[] WorkCoordinateSystems { get; init; } = Array.Empty<WorkCoordinateSystem>();

    /// <summary>
    /// Циклы сверления (G81, G83...)
    /// </summary>
    public DrillingCycles DrillingCycles { get; init; } = new();

    /// <summary>
    /// Параметры безопасности
    /// </summary>
    public SafetyParameters Safety { get; init; } = new();

    /// <summary>
    /// Параметры 5-осевой обработки
    /// </summary>
    public MultiAxisParameters? MultiAxis { get; init; }

    /// <summary>
    /// Шаблоны программы (заголовок, футер)
    /// </summary>
    public ProgramTemplates Templates { get; init; } = new();

    /// <summary>
    /// Профиль станка - имя конкретного станка на производстве
    /// Используется для применения специфичных настроек
    /// </summary>
    public string? MachineProfile { get; init; }

    /// <summary>
    /// Header программы (из Templates.Header)
    /// </summary>
    public string[] Header => Templates.Header;

    /// <summary>
    /// Footer программы (из Templates.Footer)
    /// </summary>
    public string[] Footer => Templates.Footer;

    /// <summary>
    /// Включены ли header/footer
    /// </summary>
    public bool HeaderFooterEnabled => Templates.Enabled;

    /// <summary>
    /// Пользовательские параметры для макросов
    /// Ключ-значение для гибкой настройки без изменения кода
    /// </summary>
    public Dictionary<string, object?> CustomParameters { get; init; } = new();

    /// <summary>
    /// Включить расширенные функции макросов
    /// </summary>
    public bool EnableAdvancedMacros { get; init; } = true;

    /// <summary>
    /// Путь к пользовательским макросам для этого станка
    /// </summary>
    public string? CustomMacrosPath { get; init; }

    /// <summary>
    /// Специфичные G-коды для данного станка
    /// Переопределяет стандартные значения из FunctionCodes
    /// </summary>
    public Dictionary<string, string>? CustomGCodes { get; init; }

    /// <summary>
    /// Специфичные M-коды для данного станка
    /// </summary>
    public Dictionary<string, string>? CustomMCodes { get; init; }

    /// <summary>
    /// Ограничения по осям для конкретного станка
    /// </summary>
    public AxisLimits? AxisLimits { get; init; }

    /// <summary>
    /// Получить формат регистра по адресу
    /// </summary>
    public RegisterFormat GetRegisterFormat(string address, RegisterFormat? defaultValue = null)
    {
        return RegisterFormats.TryGetValue(address.ToUpperInvariant(), out var format)
            ? format
            : defaultValue ?? new RegisterFormat
            {
                Address = address,
                Format = "F4.3",
                IsModal = true
            };
    }

    /// <summary>
    /// Получить код функции по имени группы и функции
    /// </summary>
    public FunctionCode? GetFunctionCode(string groupName, string functionName)
    {
        return FunctionCodes.Values.FirstOrDefault(f =>
            f.Group.Equals(groupName, StringComparison.OrdinalIgnoreCase) &&
            f.Code.Equals(functionName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Получить пользовательский G-код или стандартный
    /// </summary>
    public string GetCustomGCode(string standardCode, string? customKey = null)
    {
        if (customKey != null && CustomGCodes?.TryGetValue(customKey, out var custom) == true)
            return custom;
        return standardCode;
    }

    /// <summary>
    /// Получить пользовательский M-код или стандартный
    /// </summary>
    public string GetCustomMCode(string standardCode, string? customKey = null)
    {
        if (customKey != null && CustomMCodes?.TryGetValue(customKey, out var custom) == true)
            return custom;
        return standardCode;
    }

    /// <summary>
    /// Получить пользовательский параметр
    /// </summary>
    public T? GetCustomParameter<T>(string key, T? defaultValue = default)
    {
        if (CustomParameters.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }
}

public enum MachineType
{
    Milling,
    Turning,
    MillTurn,
    Edm,
    Laser,
    Router
}

public record WorkCoordinateSystem
{
    public int Number { get; init; } // 1 = G54, 2 = G55...
    public string Code { get; init; } = string.Empty; // "G54"
    public double XOffset { get; init; }
    public double YOffset { get; init; }
    public double ZOffset { get; init; }
}

public record DrillingCycles
{
    public string Drill { get; init; } = "G81";
    public string PeckDrill { get; init; } = "G83";
    public string Tapping { get; init; } = "G84";
    public string Boring { get; init; } = "G85";
    public string FineBoring { get; init; } = "G76";
}

public record MultiAxisParameters
{
    public bool EnableRtcp { get; init; } = false;
    public double MaxA { get; init; } = 120.0;
    public double MinA { get; init; } = -120.0;
    public double MaxB { get; init; } = 360.0;
    public double MinB { get; init; } = 0.0;
    public string Strategy { get; init; } = "cartesian";
}

public record ProgramTemplates
{
    public bool Enabled { get; init; } = true;
    public string[] Header { get; init; } = new[] { "(Generated by PostProcessor)" };
    public string[] Footer { get; init; } = new[] { "M30" };
    public bool IncludeTimestamp { get; init; } = true;
    public bool IncludeToolList { get; init; } = true;
}

/// <summary>
/// Ограничения по осям станка
/// </summary>
public record AxisLimits
{
    public double XMin { get; init; } = -1000.0;
    public double XMax { get; init; } = 1000.0;
    public double YMin { get; init; } = -500.0;
    public double YMax { get; init; } = 500.0;
    public double ZMin { get; init; } = -400.0;
    public double ZMax { get; init; } = 100.0;
    public double AMin { get; init; } = -120.0;
    public double AMax { get; init; } = 120.0;
    public double BMin { get; init; } = 0.0;
    public double BMax { get; init; } = 360.0;
    public double CMin { get; init; } = 0.0;
    public double CMax { get; init; } = 360.0;
}

public static class ConfigLoader
{
    public static ControllerConfig Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Config file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ControllerConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        }) ?? throw new InvalidOperationException($"Failed to deserialize config: {filePath}");
    }
}
