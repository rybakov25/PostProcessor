namespace PostProcessor.Core.Context;

/// <summary>
/// NC-слово с автоматическим инкрементом (для нумерации блоков N1, N2, N3...)
/// Аналог CountingNCWord из SPRUT SDK
/// </summary>
public class SequenceNCWord : NCWord
{
    private int _value;
    private readonly int _step;
    private readonly string? _prefix;
    private readonly string? _suffix;

    /// <summary>
    /// Создать счётчик с автоинкрементом
    /// </summary>
    /// <param name="start">Начальное значение</param>
    /// <param name="step">Шаг инкремента</param>
    /// <param name="prefix">Префикс (например, "N")</param>
    /// <param name="suffix">Суффикс (например, "")</param>
    /// <param name="isModal">Режим модальности (по умолчанию false — всегда выводится)</param>
    public SequenceNCWord(int start = 1, int step = 10, string? prefix = "N", string? suffix = "", bool isModal = false)
    {
        Address = prefix ?? "";
        _prefix = prefix;
        _suffix = suffix;
        _value = start;
        _step = step;
        IsModal = isModal;
        _hasChanged = true;
    }

    /// <summary>
    /// Текущее значение счётчика
    /// </summary>
    public int Value => _value;

    /// <summary>
    /// Шаг инкремента
    /// </summary>
    public int Step => _step;

    /// <summary>
    /// Установить новое значение
    /// </summary>
    /// <param name="value">Новое значение</param>
    public void SetValue(int value)
    {
        _hasChanged = value != _value;
        _value = value;
    }

    /// <summary>
    /// Инкрементировать счётчик на шаг
    /// </summary>
    public void Increment()
    {
        _value += _step;
        _hasChanged = true;
    }

    /// <summary>
    /// Сбросить счётчик к начальному значению
    /// </summary>
    /// <param name="start">Новое начальное значение (или текущее если не указано)</param>
    public void Reset(int? start = null)
    {
        _value = start ?? _value;
        _hasChanged = true;
    }

    /// <summary>
    /// Сформировать строку для вывода в NC-файл
    /// Автоматически инкрементирует значение после вывода
    /// </summary>
    public override string ToNCString()
    {
        if (!HasChanged && IsModal)
            return "";

        var result = $"{_prefix}{_value}{_suffix}";
        
        // Автоматический инкремент после вывода
        Increment();
        
        return result;
    }

    /// <summary>
    /// Переопределение для совместимости с BlockWriter
    /// </summary>
    public override string ToString()
    {
        return $"{_prefix}{_value}{_suffix}";
    }
}
