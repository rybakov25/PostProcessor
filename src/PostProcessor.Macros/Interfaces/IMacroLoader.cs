namespace PostProcessor.Macros.Interfaces;

public interface IMacroLoader
{
    /// <summary>
    /// Загрузка макросов из указанного пути
    /// </summary>
    Task<IEnumerable<IMacro>> LoadMacrosAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Поддерживаемые расширения файлов макросов
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }
}
