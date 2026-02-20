using PostProcessor.Core.Models;
using PostProcessor.Macros.Engine;

namespace PostProcessor.Macros.Interfaces;

/// <summary>
/// Интерфейс макроса для обработки APT-команд.
/// Макросы должны быть помечены атрибутами [MacroName] и [MacroPriority].
/// </summary>
public interface IMacro
{
    /// <summary>
    /// Имя макроса (соответствует имени команды в APT).
    /// Должно совпадать со значением атрибута [MacroName].
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Приоритет выполнения макроса.
    /// Должен совпадать со значением атрибута [MacroPriority].
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Обработка команды в контексте выполнения.
    /// </summary>
    Task ProcessAsync(MacroExecutionContext context, APTCommand command);
}
