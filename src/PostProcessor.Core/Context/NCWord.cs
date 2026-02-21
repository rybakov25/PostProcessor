namespace PostProcessor.Core.Context;

/// <summary>
/// Базовый класс для NC-слова (регистра ЧПУ)
/// Преобразует значения в текст для G-кода с поддержкой модальности
/// </summary>
public abstract class NCWord
{
    /// <summary>
    /// Адрес слова (X, Y, Z, G, M, F, S...)
    /// </summary>
    public string Address { get; set; } = "";
    
    /// <summary>
    /// Режим модальности: если true, не выводится при неизменном значении
    /// </summary>
    public bool IsModal { get; set; } = true;
    
    /// <summary>
    /// Флаг изменения: true если значение изменилось и требует вывода
    /// </summary>
    protected bool _hasChanged;
    
    /// <summary>
    /// Возвращает true если слово требует вывода
    /// </summary>
    public bool HasChanged => _hasChanged;
    
    /// <summary>
    /// Принудительно установить флаг изменения
    /// </summary>
    public void ForceChanged() => _hasChanged = true;
    
    /// <summary>
    /// Принудительно сбросить флаг изменения (скрыть слово)
    /// </summary>
    public void ForceUnchanged() => _hasChanged = false;
    
    /// <summary>
    /// Сбросить флаг изменения после вывода
    /// </summary>
    public void ResetChangeFlag() => _hasChanged = false;
    
    /// <summary>
    /// Формирует строку для вывода в NC-файл
    /// </summary>
    /// <returns>NC-слово в формате "A123.456" или пустая строка если не изменено</returns>
    public abstract string ToNCString();
    
    /// <summary>
    /// Проверяет, требует ли слово вывода с учётом модальности
    /// </summary>
    public bool ShouldOutput()
    {
        return !IsModal || _hasChanged;
    }
}
