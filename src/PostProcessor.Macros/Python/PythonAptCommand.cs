using System.Collections.Generic;
using PostProcessor.Core.Models;

namespace PostProcessor.Macros.Python;

/// <summary>
/// Python-обёртка для APT-команды
/// Предоставляет доступ к параметрам команды из Python-макроса
/// </summary>
public class PythonAptCommand
{
    private readonly APTCommand _command;

    public PythonAptCommand(APTCommand command)
    {
        _command = command;
        numeric = _command.NumericValues ?? new List<double>();
        strings = _command.StringValues ?? new List<string>();
        minorWords = _command.MinorWords ?? new List<string>();
    }

    /// <summary>
    /// Основное слово команды (например, "goto", "spindl")
    /// </summary>
    public string majorWord => _command.MajorWord?.ToLowerInvariant() ?? "";

    /// <summary>
    /// Номер строки в исходном файле
    /// </summary>
    public int lineNumber => _command.LineNumber;

    /// <summary>
    /// Числовые параметры команды (список double)
    /// Пример: GOTO/100, 50, 10 -> [100.0, 50.0, 10.0]
    /// </summary>
    public List<double> numeric { get; }

    /// <summary>
    /// Строковые параметры команды (список string)
    /// Пример: PARTNO/NAME, 123 -> ["NAME"]
    /// </summary>
    public List<string> strings { get; }

    /// <summary>
    /// Младшие слова команды (ключевые слова)
    /// Пример: SPINDL/ON, CLW -> ["on", "clw"]
    /// </summary>
    public List<string> minorWords { get; }
    
    /// <summary>
    /// Проверить наличие ключевого слова
    /// </summary>
    public bool hasMinorWord(string word)
    {
        var lowerWord = word.ToLowerInvariant();
        foreach (var minor in minorWords)
        {
            if (minor.ToLowerInvariant() == lowerWord)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Получить первое числовое значение или значение по умолчанию
    /// </summary>
    public double getNumeric(int index = 0, double defaultValue = 0.0)
    {
        if (index >= 0 && index < numeric.Count)
            return numeric[index];
        return defaultValue;
    }
    
    /// <summary>
    /// Получить первое строковое значение или значение по умолчанию
    /// </summary>
    public string getString(int index = 0, string defaultValue = "")
    {
        if (index >= 0 && index < strings.Count)
            return strings[index];
        return defaultValue;
    }
}
