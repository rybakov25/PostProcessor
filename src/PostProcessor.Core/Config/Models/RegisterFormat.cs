namespace PostProcessor.Core.Config.Models;

/// <summary>
/// ‘орматирование регистра контроллера (буквенный адрес + точность)
/// —оответствует IMSpost
/// </summary>
public record RegisterFormat
{
    /// <summary>
    /// Ѕуквенный адрес регистра (X, Y, Z, F, S, T и т.д.)
    /// </summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// ‘ормат вывода (например, "F4.3" дл€ 4 знака до точки, 3 после)
    /// </summary>
    public string Format { get; init; } = "F4.3";

    /// <summary>
    /// явл€етс€ ли регистр модальным (сохран€ет значение между кадрами)
    /// </summary>
    public bool IsModal { get; init; } = true;

    /// <summary>
    /// ћинимальное значение (дл€ валидации)
    /// </summary>
    public double? MinValue { get; init; }

    /// <summary>
    /// ћаксимальное значение (дл€ валидации)
    /// </summary>
    public double? MaxValue { get; init; }

    /// <summary>
    /// ‘орматирование значени€ регистра согласно спецификации контроллера
    /// </summary>
    public string FormatValue(double value)
    {
        return value.ToString(Format, System.Globalization.CultureInfo.InvariantCulture);
    }
}
