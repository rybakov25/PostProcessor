namespace PostProcessor.Macros.Attributes;

/// <summary>
/// Определяет приоритет выполнения макроса.
/// Низкий приоритет = выполняется раньше.
/// Стандартные значения:
///   System    = 0   — системные макросы (базовые команды)
///   Controller = 10  — макросы контроллера (Fanuc, Heidenhain)
///   Machine    = 20  — макросы станка (5-осевой, токарный)
///   Process    = 50  — макросы операции (сверление, фрезерование)
///   User       = 100 — пользовательские макросы
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class MacroPriorityAttribute : Attribute
{
    public int Priority { get; }

    public MacroPriorityAttribute(int priority)
    {
        if (priority < 0 || priority > 1000)
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be between 0 and 1000");

        Priority = priority;
    }

    // Стандартные приоритеты (для удобства использования)
    public const int System = 0;
    public const int Controller = 10;
    public const int Machine = 20;
    public const int Process = 50;
    public const int User = 100;
}
