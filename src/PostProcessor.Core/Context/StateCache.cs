using System.Collections.Concurrent;

namespace PostProcessor.Core.Context;

/// <summary>
/// Кэш состояний для отслеживания изменений переменных (IMSPost-style LAST_* variables)
/// Используется для модального вывода только изменённых значений
/// </summary>
public class StateCache
{
    private readonly ConcurrentDictionary<string, object> _lastValues = new();

    /// <summary>
    /// Проверить, изменилось ли значение по сравнению с последним закэшированным
    /// </summary>
    /// <typeparam name="T">Тип значения</typeparam>
    /// <param name="key">Ключ переменной (например, "LAST_FEED", "LAST_TOOL")</param>
    /// <param name="currentValue">Текущее значение</param>
    /// <returns>true если значение изменилось или отсутствует в кэше</returns>
    public bool HasChanged<T>(string key, T currentValue)
    {
        if (!_lastValues.TryGetValue(key, out var last))
            return true;

        if (last is T typedLast)
            return !EqualityComparer<T>.Default.Equals(typedLast, currentValue);

        return true;
    }

    /// <summary>
    /// Обновить значение в кэше
    /// </summary>
    /// <typeparam name="T">Тип значения</typeparam>
    /// <param name="key">Ключ переменной</param>
    /// <param name="value">Новое значение</param>
    public void Update<T>(string key, T value)
    {
        _lastValues[key] = value!;
    }

    /// <summary>
    /// Получить значение из кэша
    /// </summary>
    /// <typeparam name="T">Тип значения</typeparam>
    /// <param name="key">Ключ переменной</param>
    /// <param name="defaultValue">Значение по умолчанию</param>
    /// <returns>Закэшированное значение или default</returns>
    public T Get<T>(string key, T defaultValue = default!)
    {
        return _lastValues.TryGetValue(key, out var v) && v is T typed
            ? typed
            : defaultValue;
    }

    /// <summary>
    /// Получить или установить значение (если отсутствует)
    /// </summary>
    /// <typeparam name="T">Тип значения</typeparam>
    /// <param name="key">Ключ переменной</param>
    /// <param name="defaultValue">Значение по умолчанию</param>
    /// <returns>Существующее или установленное значение</returns>
    public T GetOrSet<T>(string key, T defaultValue = default!)
    {
        if (_lastValues.TryGetValue(key, out var v) && v is T typed)
            return typed;

        _lastValues[key] = defaultValue!;
        return defaultValue;
    }

    /// <summary>
    /// Сбросить значение из кэша
    /// </summary>
    /// <param name="key">Ключ переменной</param>
    public void Remove(string key)
    {
        _lastValues.TryRemove(key, out _);
    }

    /// <summary>
    /// Сбросить весь кэш
    /// </summary>
    public void Clear()
    {
        _lastValues.Clear();
    }

    /// <summary>
    /// Получить количество закэшированных значений
    /// </summary>
    public int Count => _lastValues.Count;

    /// <summary>
    /// Получить все ключи в кэше
    /// </summary>
    public IEnumerable<string> Keys => _lastValues.Keys;

    /// <summary>
    /// Проверить наличие ключа в кэше
    /// </summary>
    /// <param name="key">Ключ переменной</param>
    /// <returns>true если ключ присутствует</returns>
    public bool Contains(string key)
    {
        return _lastValues.ContainsKey(key);
    }

    /// <summary>
    /// Установить значение без проверки изменений (для инициализации)
    /// </summary>
    /// <typeparam name="T">Тип значения</typeparam>
    /// <param name="key">Ключ переменной</param>
    /// <param name="value">Значение</param>
    public void SetInitial<T>(string key, T value)
    {
        _lastValues.AddOrUpdate(key, value!, (_, _) => value!);
    }
}
