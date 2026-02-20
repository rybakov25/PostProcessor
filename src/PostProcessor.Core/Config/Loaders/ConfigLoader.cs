using PostProcessor.Core.Config.Models;

namespace PostProcessor.Core.Config.Loaders;

/// <summary>
/// Базовый абстрактный загрузчик конфигураций
/// Предоставляет единую точку доступа к различным форматам конфигураций
/// </summary>
public abstract class ConfigLoader
{
    /// <summary>
    /// Загрузка конфигурации контроллера
    /// </summary>
    public abstract ControllerConfig LoadController(string filePath);

    /// <summary>
    /// Загрузка конфигурации станка
    /// </summary>
    public abstract MachineConfig LoadMachine(string filePath);

    /// <summary>
    /// Загрузка библиотеки инструментов
    /// </summary>
    public abstract ToolLibraryConfig LoadToolLibrary(string filePath);

    /// <summary>
    /// Поиск файла конфигурации по имени в указанном базовом пути
    /// </summary>
    public virtual string FindConfigFile(string configName, string basePath, string subDirectory, string extension = ".json")
    {
        var searchPaths = new[]
        {
            Path.Combine(basePath, subDirectory, $"{configName}{extension}"),
            Path.Combine(basePath, subDirectory, configName, $"{configName}{extension}"),
            Path.Combine(basePath, subDirectory, configName.ToLowerInvariant(), $"{configName.ToLowerInvariant()}{extension}")
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException(
            $"Configuration file not found for '{configName}' in {basePath}/{subDirectory}");
    }
}
