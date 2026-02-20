using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostProcessor.Core.Context;
using PostProcessor.Core.Models;

namespace PostProcessor.Core.Macros.Base;

/// <summary>
/// Базовый класс для макросов вспомогательных функций (M-коды, шпиндель, охлаждение)
/// </summary>
public abstract class AuxiliaryMacroBase : MacroBase
{
    protected AuxiliaryMacroBase(PostContext context, APTCommand command)
        : base(context, command)
    {
    }

    /// <summary>
    /// Получить состояние из команды
    /// </summary>
    protected string GetState()
    {
        var minorWords = GetMinorWords();
        if (minorWords.Count == 0)
            return "off";

        var state = minorWords[0].ToLowerInvariant();
        return state switch
        {
            "on" => "on",
            "off" => "off",
            "flood" => "flood",
            "mist" => "mist",
            "cw" or "clockwise" or "clw" => "cw",
            "ccw" or "counter-clockwise" or "cclw" => "ccw",
            _ => state
        };
    }

    /// <summary>
    /// Записать M-код с проверкой модальности
    /// </summary>
    protected async Task WriteMCodeAsync(string mCode, bool isModal = true)
    {
        if (isModal && Context.Machine.LastMCode == mCode)
            return;

        await WriteLineAsync(mCode);
        Context.Machine.LastMCode = mCode;
    }

    /// <summary>
    /// Записать S-код (шпиндель)
    /// </summary>
    protected async Task WriteSCodeAsync(double rpm)
    {
        var safety = Context.Config.Safety;

        if (rpm > safety.MaxSpindleSpeed)
        {
            await WriteLineAsync($"(WARNING: Spindle speed {rpm:F0} exceeds max {safety.MaxSpindleSpeed:F0})");
            rpm = safety.MaxSpindleSpeed;
        }

        if (rpm > 0)
            await WriteLineAsync($"S{rpm:F0}");
    }
}
