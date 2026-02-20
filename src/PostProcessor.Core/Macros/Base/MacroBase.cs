using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostProcessor.Core.Context;
using PostProcessor.Core.Models;

namespace PostProcessor.Core.Macros.Base;

/// <summary>
/// Базовый класс для всех макросов постпроцессора
/// Предоставляет общие методы и свойства для обработки APT-команд
/// </summary>
public abstract class MacroBase
{
    protected PostContext Context { get; }
    protected APTCommand Command { get; }

    protected MacroBase(PostContext context, APTCommand command)
    {
        Context = context;
        Command = command;
    }

    /// <summary>
    /// Основной метод обработки команды
    /// </summary>
    public abstract Task ExecuteAsync();

    /// <summary>
    /// Получить числовые параметры команды
    /// </summary>
    protected IReadOnlyList<double> GetNumericParams() => Command.NumericValues;

    /// <summary>
    /// Получить строковые параметры команды
    /// </summary>
    protected IReadOnlyList<string> GetStringParams() => Command.StringValues;

    /// <summary>
    /// Получить младшие слова команды
    /// </summary>
    protected IReadOnlyList<string> GetMinorWords() => Command.MinorWords;

    /// <summary>
    /// Проверить наличие параметра в младших словах
    /// </summary>
    protected bool HasMinorWord(string word) =>
        GetMinorWords().Any(w => w.Equals(word, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Записать строку в выходной файл
    /// </summary>
    protected async Task WriteLineAsync(string line)
    {
        if (!string.IsNullOrWhiteSpace(line))
            await Context.Output.WriteLineAsync(line);
    }

    /// <summary>
    /// Форматировать значение с учётом формата регистра
    /// </summary>
    protected string FormatValue(double value, string format = "F4.3")
    {
        return format switch
        {
            "F4.3" => value.ToString("F3"),
            "F3.2" => value.ToString("F2"),
            "F3.1" => value.ToString("F1"),
            "F0" => value.ToString("F0"),
            _ => value.ToString("F3")
        };
    }
}
