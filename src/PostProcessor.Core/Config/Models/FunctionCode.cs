namespace PostProcessor.Core.Config.Models;

/// <summary>
/// Код функции контроллера (G/M код) с группировкой и модальностью
/// Соответствует IMSpost
/// </summary>
public record FunctionCode
{
    /// <summary>
    /// Код функции (например, "G00", "M03", "G41")
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Группа функции (например, "MOTION", "SPINDLE", "COOLANT")
    /// Функции одной группы взаимоисключаемы (модальность на уровне группы)
    /// </summary>
    public string Group { get; init; } = string.Empty;

    /// <summary>
    /// Является ли функция модальной (сохраняется до отмены)
    /// </summary>
    public bool IsModal { get; init; } = true;

    /// <summary>
    /// Дополнительные регистры, выводимые с функцией (например, "G43 H1" для длины инструмента)
    /// </summary>
    public string[] AssociatedRegisters { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Описание функции для документации
    /// </summary>
    public string Description { get; init; } = string.Empty;
}
