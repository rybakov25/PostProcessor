using System.Text.Json.Serialization;

namespace PostProcessor.Core.Config.Models;

/// <summary>
/// Библиотека инструментов станка
/// </summary>
public record ToolLibraryConfig
{
    /// <summary>
    /// Имя библиотеки инструментов
    /// </summary>
    public string Name { get; init; } = "DefaultToolLibrary";

    /// <summary>
    /// Список инструментов в библиотеке
    /// </summary>
    public List<ToolDefinition> Tools { get; init; } = new();

    /// <summary>
    /// Поиск инструмента по номеру
    /// </summary>
    public ToolDefinition? FindTool(int toolNumber)
    {
        return Tools.FirstOrDefault(t => t.Number == toolNumber);
    }

    /// <summary>
    /// Поиск инструмента по имени/комментарию
    /// </summary>
    public ToolDefinition? FindToolByName(string name)
    {
        return Tools.FirstOrDefault(t =>
            t.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true ||
            t.Comment?.Contains(name, StringComparison.OrdinalIgnoreCase) == true);
    }
}

/// <summary>
/// Определение инструмента с полными параметрами
/// номер коррекции, Z.., K.., c.., m..
/// где c.. = текущее значение, m.. = максимальное значение
/// </summary>
public record ToolDefinition
{
    /// <summary>
    /// Номер инструмента (1..9999)
    /// </summary>
    public int Number { get; init; }

    /// <summary>
    /// Имя инструмента (например, "концевая фреза с радиусом")
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Тип инструмента (endmill, drill, ballnose, face_mill, tap)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ToolType Type { get; init; } = ToolType.EndMill;

    /// <summary>
    /// Диаметр инструмента в мм (для фрез — наружный диаметр)
    /// </summary>
    public double Diameter { get; init; }

    /// <summary>
    /// Эффективный диаметр с учётом радиуса скругления (для концевых фрез)
    /// </summary>
    public double EffectiveDiameter =>
        CornerRadius > 0 ? Diameter - 2 * CornerRadius : Diameter;

    /// <summary>
    /// Длина инструмента (от базы до режущей кромки)
    /// </summary>
    public double Length { get; init; }

    /// <summary>
    /// Радиус скругления режущей кромки (для концевых фрез с радиусом)
    /// </summary>
    public double CornerRadius { get; init; } = 0.0;

    /// <summary>
    /// Количество зубьев
    /// </summary>
    public int Flutes { get; init; } = 2;

    /// <summary>
    /// Материал инструмента (HSS, carbide, ceramic)
    /// </summary>
    public string Material { get; init; } = "carbide";

    /// <summary>
    /// Комментарий/описание (например, "D20R0.8L70")
    /// </summary>
    public string? Comment { get; init; }

    /// <summary>
    /// Текущая коррекция длины (Z) — аналог "c.."
    /// </summary>
    public double LengthCorrection { get; init; } = 0.0;

    /// <summary>
    /// Максимально допустимая коррекция длины — аналог "m.."
    /// </summary>
    public double MaxLengthCorrection { get; init; } = 5.0;

    /// <summary>
    /// Текущая коррекция диаметра (для осей в диаметральном режиме) — аналог "c.."
    /// </summary>
    public double DiameterCorrection { get; init; } = 0.0;

    /// <summary>
    /// Максимально допустимая коррекция диаметра — аналог "m.."
    /// </summary>
    public double MaxDiameterCorrection { get; init; } = 2.0;

    /// <summary>
    /// Статус инструмента (активен/изношен/негоден)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ToolStatus Status { get; init; } = ToolStatus.Active;

    /// <summary>
    /// Оставшееся время службы в минутах (для систем отслеживания срока службы)
    /// </summary>
    public double RemainingLife { get; init; } = double.PositiveInfinity;

    /// <summary>
    /// Альтернативный инструмент (для автоматической замены при износе)
    /// </summary>
    public int? AlternateTool { get; init; }
}

public enum ToolType
{
    EndMill,
    BallNose,
    Drill,
    Tap,
    FaceMill,
    BoringBar,
    Reamer,
    ChamferMill
}

public enum ToolStatus
{
    Active,      // Инструмент готов к работе
    Worn,        // Инструмент изношен, требуется замена при следующей возможности
    Broken,      // Инструмент объявлен негодным
    Reserved     // Инструмент зарезервирован для будущих операций
}
