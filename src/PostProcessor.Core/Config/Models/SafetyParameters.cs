namespace PostProcessor.Core.Config.Models;

/// <summary>
/// Параметры безопасности станка
/// </summary>
public record SafetyParameters
{
    /// <summary>
    /// Безопасная высота подъёма по оси Z перед перемещением в плоскости XY
    /// </summary>
    public double ClearancePlane { get; init; } = 100.0;

    /// <summary>
    /// Высота ретракта над деталью (для смены инструмента)
    /// </summary>
    public double RetractPlane { get; init; } = 5.0;

    /// <summary>
    /// Максимальная подача (мм/мин)
    /// </summary>
    public double MaxFeedRate { get; init; } = 10000.0;

    /// <summary>
    /// Максимальные обороты шпинделя (об/мин)
    /// </summary>
    public double MaxSpindleSpeed { get; init; } = 12000.0;

    /// <summary>
    /// Минимальная безопасная подача для резьбонарезания
    /// </summary>
    public double MinThreadFeed { get; init; } = 0.1;

    /// <summary>
    /// Включить автоматический подъём по Z при смене инструмента
    /// </summary>
    public bool AutoToolChangeRetract { get; init; } = true;

    /// <summary>
    /// Включить проверку выхода за пределы рабочей зоны
    /// </summary>
    public bool EnableTravelLimitsCheck { get; init; } = true;

    /// <summary>
    /// Защищённые зоны.
    /// Формат: (номер_зоны, мин_X, макс_X, мин_Y, макс_Y, мин_Z, макс_Z)
    /// </summary>
    public List<(int ZoneNumber, double MinX, double MaxX, double MinY, double MaxY, double MinZ, double MaxZ)> ProtectedZones { get; init; } = new();
}
