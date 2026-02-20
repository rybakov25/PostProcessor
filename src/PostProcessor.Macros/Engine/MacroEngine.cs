using System.Reflection;
using PostProcessor.Core.Context;
using PostProcessor.Core.Models;
using PostProcessor.Macros.Attributes;
using PostProcessor.Macros.Interfaces;

namespace PostProcessor.Macros.Engine;

public class MacroEngine : IMacroEngine
{
    private readonly List<IMacroLoader> _loaders = new();
    private readonly Dictionary<string, List<IMacro>> _macroRegistry = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool _isLoaded;

    public void RegisterLoader(IMacroLoader loader)
    {
        _loaders.Add(loader);
    }

    public int GetMacroCount() => _macroRegistry.Values.Sum(v => v.Count);

    public async Task LoadAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        await _loadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var allMacros = new List<IMacro>();

            // 1. Сначала загружаем встроенные макросы из текущего assembly
            var builtInMacros = LoadBuiltInMacros();
            allMacros.AddRange(builtInMacros);

            // 2. Затем загружаем макросы из файлов
            foreach (var path in paths)
            {
                foreach (var loader in _loaders)
                {
                    try
                    {
                        var macros = await loader.LoadMacrosAsync(path, cancellationToken).ConfigureAwait(false);
                        allMacros.AddRange(macros);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to load macros from {path}: {ex.Message}");
                    }
                }
            }

            // Регистрация макросов с сортировкой по приоритету
            foreach (var macro in allMacros.OrderBy(m => m.Priority))
            {
                var key = macro.Name.ToLowerInvariant();

                if (!_macroRegistry.ContainsKey(key))
                    _macroRegistry[key] = new List<IMacro>();

                _macroRegistry[key].Add(macro);
            }

            _isLoaded = true;
            Console.WriteLine($"Loaded {_macroRegistry.Values.Sum(v => v.Count)} macros from {paths.Count()} paths");
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// Загрузка встроенных макросов из текущего assembly
    /// </summary>
    private List<IMacro> LoadBuiltInMacros()
    {
        // Встроенные макросы отключены - требуют доработки архитектуры
        // Для использования макросов поместите .cs файлы в директорию macros/
        return new List<IMacro>();
    }

    public IEnumerable<IMacro> FindMacros(string commandName)
    {
        var name = commandName.ToLowerInvariant();

        // Поиск точного совпадения
        if (_macroRegistry.TryGetValue(name, out var macros))
            return macros;

        // Поиск по шаблону "имя/*"
        var patternKey = $"{name}/*";
        if (_macroRegistry.TryGetValue(patternKey, out macros))
            return macros;

        // Поиск универсального макроса "*"
        if (_macroRegistry.TryGetValue("*", out macros))
            return macros;

        return Enumerable.Empty<IMacro>();
    }

    public async Task ExecuteAsync(PostContext context, APTCommand command, CancellationToken cancellationToken = default)
    {
        if (!_isLoaded)
            throw new InvalidOperationException("Macros not loaded. Call LoadAsync first.");

        var macros = FindMacros(command.MajorWord).ToList();

        if (!macros.Any())
        {
            // Нет макроса — выводим предупреждение и пропускаем команду
            Console.WriteLine($"[WARNING] No macro found for command: {command.MajorWord} at line {command.LineNumber}");
            return;
        }

        var executionContext = new MacroExecutionContext(context, command);

        // Выполнение макросов в порядке приоритета
        foreach (var macro in macros)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await macro.ProcessAsync(executionContext, command).ConfigureAwait(false);
        }
    }

    public ValueTask DisposeAsync()
    {
        // Очистка изолированных контекстов загрузки
        foreach (var loader in _loaders.OfType<IDisposable>())
            loader.Dispose();

        _macroRegistry.Clear();
        GC.SuppressFinalize(this);
        _isLoaded = false;

        return ValueTask.CompletedTask;
    }
}
