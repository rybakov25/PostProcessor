using System.Text.Json.Serialization;

namespace PostProcessor.Core.Config.Models;

/// <summary>
/// Конфигурация станка (физические параметры и ограничения)
/// </summary>
public record MachineConfig
{
    public string Name { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MachineType Type { get; init; } = MachineType.Milling;

    /// <summary>
    /// Рабочая зона станка по линейным осям (мм)
    /// Минимальные и максимальные значения для проверки выхода за пределы
    /// </summary>
    public double MinX { get; init; } = -1000.0;
    public double MaxX { get; init; } = 1000.0;
    public double MinY { get; init; } = -500.0;
    public double MaxY { get; init; } = 500.0;
    public double MinZ { get; init; } = -400.0;
    public double MaxZ { get; init; } = 100.0;

    /// <summary>
    /// Ограничения для ротационных осей (градусы)
    /// </summary>
    public double MinA { get; init; } = -120.0;
    public double MaxA { get; init; } = 120.0;
    public double MinB { get; init; } = 0.0;
    public double MaxB { get; init; } = 360.0;
    public double MinC { get; init; } = -360.0;
    public double MaxC { get; init; } = 360.0;

    /// <summary>
    /// Максимальные скорости осей (мм/мин)
    /// </summary>
    public double MaxFeedX { get; init; } = 10000.0;
    public double MaxFeedY { get; init; } = 10000.0;
    public double MaxFeedZ { get; init; } = 8000.0;

    /// <summary>
    /// Наличие ротационных осей
    /// </summary>
    public bool HasAxisA { get; init; } = false;
    public bool HasAxisB { get; init; } = false;
    public bool HasAxisC { get; init; } = false;

    /// <summary>
    /// Тип кинематики для 5-осевых станков
    /// </summary>
    public string KinematicsType { get; init; } = "table-table"; // table-table, head-head, table-head

    /// <summary>
    /// Защищённые зоны. Каждая зона определяется как параллелепипед с координатами
    /// </summary>
    public List<ProtectedZone> ProtectedZones { get; init; } = new();

    /// <summary>
    /// Проверка попадания точки в защищённую зону
    /// </summary>
    public bool IsInProtectedZone(double x, double y, double z)
    {
        return ProtectedZones.Any(zone => zone.Contains(x, y, z));
    }
}

/// <summary>
/// Защищённая зона станка. Определяет область, куда запрещён вход инструмента
/// </summary>
public record ProtectedZone
{
    public int Number { get; init; }
    public double MinX { get; init; }
    public double MaxX { get; init; }
    public double MinY { get; init; }
    public double MaxY { get; init; }
    public double MinZ { get; init; }
    public double MaxZ { get; init; }
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Проверка попадания точки в зону
    /// </summary>
    public bool Contains(double x, double y, double z)
    {
        if (!IsActive)
            return false;

        return x >= MinX && x <= MaxX &&
               y >= MinY && y <= MaxY &&
               z >= MinZ && z <= MaxZ;
    }
}
