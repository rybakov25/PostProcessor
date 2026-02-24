using System.Collections.Concurrent;
using System.Globalization;

namespace PostProcessor.Core.Context;

/// <summary>
/// Кэш параметров циклов (CYCLE800, CYCLE81, CYCLE83...)
/// Автоматически выбирает: полное определение или только вызов
/// </summary>
public class CycleCache
{
    private readonly ConcurrentDictionary<string, object> _cachedParams = new();
    private string? _lastCycleName;
    private int _callCount;
    private int _fullDefinitionCount;

    /// <summary>
    /// Создать кэш для указанного цикла
    /// </summary>
    /// <param name="cycleName">Имя цикла (например, "CYCLE800")</param>
    public CycleCache(string cycleName)
    {
        CycleName = cycleName;
        _callCount = 0;
        _fullDefinitionCount = 0;
    }

    /// <summary>
    /// Имя цикла
    /// </summary>
    public string CycleName { get; }

    /// <summary>
    /// Записать цикл, если параметры отличаются от закэшированных
    /// </summary>
    /// <param name="writer">BlockWriter для вывода</param>
    /// <param name="parameters">Параметры цикла</param>
    /// <returns>true если записано полное определение, false если только вызов</returns>
    public bool WriteIfDifferent(BlockWriter writer, Dictionary<string, object> parameters)
    {
        _callCount++;

        if (ParametersEqual(_cachedParams, parameters))
        {
            // Те же параметры - записываем только вызов
            writer.WriteLine($"{CycleName}()");
            return false;
        }

        // Новые параметры - записываем полное определение
        var formatted = FormatParams(parameters);
        writer.WriteLine($"{CycleName}({formatted})");

        // Обновляем кэш
        _cachedParams.Clear();
        foreach (var kvp in parameters)
            _cachedParams[kvp.Key] = kvp.Value;

        _lastCycleName = CycleName;
        _fullDefinitionCount++;
        return true;
    }

    /// <summary>
    /// Записать цикл с форматированием через BlockWriter
    /// </summary>
    /// <param name="writer">BlockWriter для вывода</param>
    /// <param name="parameters">Параметры цикла</param>
    /// <param name="includeBlockNumber">Включить номер блока</param>
    /// <returns>true если записано полное определение</returns>
    public bool WriteBlockIfDifferent(BlockWriter writer, Dictionary<string, object> parameters, bool includeBlockNumber = true)
    {
        _callCount++;

        if (ParametersEqual(_cachedParams, parameters))
        {
            // Только вызов
            writer.WriteLine($"{CycleName}()");
            return false;
        }

        // Полное определение
        var formatted = FormatParams(parameters);
        writer.WriteLine($"{CycleName}({formatted})");

        // Обновляем кэш
        _cachedParams.Clear();
        foreach (var kvp in parameters)
            _cachedParams[kvp.Key] = kvp.Value;

        _lastCycleName = CycleName;
        _fullDefinitionCount++;
        return true;
    }

    /// <summary>
    /// Сбросить кэш
    /// </summary>
    public void Reset()
    {
        _cachedParams.Clear();
        _lastCycleName = null;
    }

    /// <summary>
    /// Получить статистику использования кэша
    /// </summary>
    /// <returns>Словарь со статистикой</returns>
    public Dictionary<string, object> GetStats()
    {
        return new Dictionary<string, object>
        {
            { "cycle_name", CycleName },
            { "call_count", _callCount },
            { "full_definition_count", _fullDefinitionCount },
            { "is_cached", _cachedParams.Count > 0 },
            { "cached_params_count", _cachedParams.Count }
        };
    }

    /// <summary>
    /// Проверить равенство параметров
    /// </summary>
    private static bool ParametersEqual(ConcurrentDictionary<string, object> cached, Dictionary<string, object> current)
    {
        if (cached.Count != current.Count)
            return false;

        foreach (var kvp in current)
        {
            if (!cached.TryGetValue(kvp.Key, out var cachedValue))
                return false;

            if (!Equals(cachedValue, kvp.Value))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Форматировать параметры для вывода
    /// </summary>
    private static string FormatParams(Dictionary<string, object> parameters)
    {
        var parts = new List<string>();

        foreach (var kvp in parameters)
        {
            string formattedValue = kvp.Value switch
            {
                double doubleValue => doubleValue.ToString("F3", CultureInfo.InvariantCulture),
                int intValue => intValue.ToString(CultureInfo.InvariantCulture),
                string stringValue => $"\"{stringValue}\"",
                bool boolValue => boolValue ? "1" : "0",
                null => "",
                _ => kvp.Value.ToString() ?? ""
            };

            parts.Add($"{kvp.Key}={formattedValue}");
        }

        return string.Join(", ", parts);
    }
}

/// <summary>
/// Вспомогательный класс для создания и работы с кэшем циклов
/// </summary>
public static class CycleCacheHelper
{
    private static readonly ConcurrentDictionary<string, CycleCache> _cacheRegistry = new();

    /// <summary>
    /// Получить или создать кэш для цикла
    /// </summary>
    /// <param name="context">PostContext</param>
    /// <param name="cycleName">Имя цикла</param>
    /// <returns>CycleCache</returns>
    public static CycleCache GetOrCreate(PostContext context, string cycleName)
    {
        var key = $"{cycleName}_CACHE";

        var cache = context.GetSystemVariable<CycleCache?>(key, null);
        if (cache != null)
            return cache;

        cache = new CycleCache(cycleName);
        context.SetSystemVariable(key, cache);
        return cache;
    }

    /// <summary>
    /// Записать цикл, если параметры отличаются
    /// </summary>
    /// <param name="context">PostContext</param>
    /// <param name="cycleName">Имя цикла</param>
    /// <param name="parameters">Параметры цикла</param>
    /// <returns>true если записано полное определение</returns>
    public static bool WriteIfDifferent(PostContext context, string cycleName, Dictionary<string, object> parameters)
    {
        var cache = GetOrCreate(context, cycleName);
        return cache.WriteIfDifferent(context.BlockWriter, parameters);
    }

    /// <summary>
    /// Сбросить кэш цикла
    /// </summary>
    /// <param name="context">PostContext</param>
    /// <param name="cycleName">Имя цикла</param>
    public static void Reset(PostContext context, string cycleName)
    {
        var key = $"{cycleName}_CACHE";
        var cache = context.GetSystemVariable<CycleCache?>(key, null);
        cache?.Reset();
    }
}
