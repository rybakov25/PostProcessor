using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PostProcessor.Core.Context;
using PostProcessor.Core.Models;

namespace PostProcessor.Macros.Engine;

/// <summary>
/// Контекст выполнения макроса с доступом к данным и функциям постпроцессора
/// Поддерживает переменные: CLDATA, REGISTER, FUNCTION, MODE, MACHINE, SOLUTION
/// </summary>
public class MacroExecutionContext
{
    private readonly PostContext _postContext;
    private readonly APTCommand _command;
    private readonly StringWriter _outputBuffer = new();

    public MacroExecutionContext(PostContext postContext, APTCommand command)
    {
        _postContext = postContext;
        _command = command;
    }

    // === CLDATA переменные (командные данные) ===
    /// <summary>
    /// Все элементы команды (слова + значения)
    /// Аналог CLDATA в IMSpost
    /// </summary>
    public IReadOnlyList<object> CLDATA =>
        _command.MinorWords.Cast<object>()
            .Concat(_command.NumericValues.Cast<object>())
            .Concat(_command.StringValues.Cast<object>())
            .ToList();

    /// <summary>
    /// Числовые значения команды (только числа)
    /// Аналог CLDATAN в IMSpost
    /// Индексы начинаются с 0 (как в C#)
    /// </summary>
    public IReadOnlyList<double> CLDATAN => _command.NumericValues;

    /// <summary>
    /// Minor words команды
    /// Аналог CLDATAM в IMSpost
    /// </summary>
    public IReadOnlyList<string> CLDATAM => _command.MinorWords;

    // === Регистры и переменные ===
    /// <summary>
    /// Текущие значения регистров (X, Y, Z, F, S, T...)
    /// Аналог REGISTER в IMSpost
    /// </summary>
    public RegisterSet Registers => _postContext.Registers;

    /// <summary>
    /// Доступ к полному PostContext
    /// </summary>
    public PostContext PostContext => _postContext;

    // === Машина и состояние ===
    /// <summary>
    /// Состояние машины (охлаждение, вращение шпинделя)
    /// Аналог MACHINE в IMSpost
    /// </summary>
    public MachineState Machine => _postContext.Machine;

    // === CATIA-специфичные переменные ===
    /// <summary>
    /// Контекст специфичных переменных CATIA
    /// </summary>
    public CatiaContext Catia => _postContext.Catia;

    // === Вывод G-кода ===
    /// <summary>
    /// Запись строки G-кода в выходной поток
    /// </summary>
    public async Task WriteLineAsync(string line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            await _outputBuffer.WriteLineAsync(line);
            await _postContext.Output.WriteLineAsync(line);
        }
    }

    /// <summary>
    /// Форматирование движения с модальными регистрами
    /// </summary>
    public string FormatMotionBlock(bool isRapid = false) =>
        _postContext.FormatMotionBlock(isRapid);

    /// <summary>
    /// Форматирование значения регистра с названием
    /// </summary>
    public string FormatRegister(string name) =>
        Registers.GetOrAdd(name).FormatValue();

    // === Утилиты ===
    /// <summary>
    /// Логирование для отладки макросов
    /// </summary>
    public void DebugLog(string message)
    {
#if DEBUG
        Console.WriteLine($"[MACRO DEBUG] {message}");
#endif
    }
}
