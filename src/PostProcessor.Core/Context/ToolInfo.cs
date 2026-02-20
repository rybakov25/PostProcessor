namespace PostProcessor.Core.Context;

/// <summary>
/// Информация об инструменте с параметрами для ЧПУ
/// </summary>
public record ToolInfo(
    int Number,
    double Diameter,
    double Length,
    string? Comment = null,
    string? Type = null,        // "endmill", "drill", "ballnose", "face_mill"
    int? Flutes = null,         // Количество зубьев
    double? CornerRadius = null // Радиус скругления (для концевых фрез)
)
{
    /// <summary>
    /// Расчёт эффективного диаметра с учётом радиуса скругления
    /// </summary>
    public double EffectiveDiameter =>
        CornerRadius.HasValue && CornerRadius > 0
            ? Diameter - 2 * CornerRadius.Value
            : Diameter;
}
