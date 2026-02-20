using PostProcessor.Core.Context;
using PostProcessor.Core.Models;

namespace PostProcessor.Macros.Interfaces;

public interface IMacroEngine : IAsyncDisposable
{
    /// <summary>
    /// Регистрация загрузчика макросов
    /// </summary>
    void RegisterLoader(IMacroLoader loader);

    /// <summary>
    /// Загрузка всех макросов из путей
    /// </summary>
    Task LoadAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Поиск макроса по имени команды
    /// Поддержка шаблонов: "goto/*" соответствует "goto"
    /// </summary>
    IEnumerable<IMacro> FindMacros(string commandName);

    /// <summary>
    /// Выполнение макросов для команды
    /// </summary>
    Task ExecuteAsync(PostContext context, APTCommand command, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Получить количество загруженных макросов
    /// </summary>
    int GetMacroCount();
}
