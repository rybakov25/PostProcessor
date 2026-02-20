namespace PostProcessor.Macros.Models;

/// <summary>
/// Стандартные приоритеты макросов (соответствует IMSpost)
/// </summary>
public static class MacroPriority
{
    public const int System = 0;      // Системные макросы (встроенные)
    public const int Controller = 10; // Макросы контроллера (Fanuc, Heidenhain)
    public const int Machine = 20;    // Макросы станка (5-осевой, токарный)
    public const int Process = 50;    // Макросы операции (сверление, фрезерование)
    public const int User = 100;      // Пользовательские макросы
}
