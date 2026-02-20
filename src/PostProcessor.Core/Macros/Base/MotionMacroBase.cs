using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostProcessor.Core.Context;
using PostProcessor.Core.Models;

namespace PostProcessor.Core.Macros.Base;

/// <summary>
/// Базовый класс для макросов движения (GOTO, RAPID, линейная и круговая интерполяция)
/// </summary>
public abstract class MotionMacroBase : MacroBase
{
    protected MotionMacroBase(PostContext context, APTCommand command)
        : base(context, command)
    {
    }

    /// <summary>
    /// Получить координаты X, Y, Z из команды
    /// </summary>
    protected (double x, double y, double z) GetCoordinates()
    {
        var coords = GetNumericParams();
        return (
            coords.Count > 0 ? coords[0] : Context.Registers.X.Value,
            coords.Count > 1 ? coords[1] : Context.Registers.Y.Value,
            coords.Count > 2 ? coords[2] : Context.Registers.Z.Value
        );
    }

    /// <summary>
    /// Получить координаты поворота A, B, C из команды
    /// </summary>
    protected (double a, double b, double c) GetAngularCoordinates()
    {
        var coords = GetNumericParams();
        return (
            coords.Count > 3 ? coords[3] : Context.Registers.A.Value,
            coords.Count > 4 ? coords[4] : Context.Registers.B.Value,
            coords.Count > 5 ? coords[5] : Context.Registers.C.Value
        );
    }

    /// <summary>
    /// Обновить регистры координат
    /// </summary>
    protected void UpdateRegisters(double x, double y, double z)
    {
        Context.Registers.X.SetValue(x);
        Context.Registers.Y.SetValue(y);
        Context.Registers.Z.SetValue(z);
    }

    /// <summary>
    /// Обновить регистры углов
    /// </summary>
    protected void UpdateAngularRegisters(double a, double b, double c)
    {
        Context.Registers.A.SetValue(a);
        Context.Registers.B.SetValue(b);
        Context.Registers.C.SetValue(c);
    }

    /// <summary>
    /// Сформировать блок движения с G-кодом
    /// </summary>
    protected async Task WriteMotionBlockAsync(string gCode, bool includeFeed = true)
    {
        var parts = new List<string> { gCode };

        // Координаты
        if (Context.Registers.X.HasChanged || !Context.Registers.X.IsModal)
            parts.Add($"X{FormatValue(Context.Registers.X.Value)}");
        if (Context.Registers.Y.HasChanged || !Context.Registers.Y.IsModal)
            parts.Add($"Y{FormatValue(Context.Registers.Y.Value)}");
        if (Context.Registers.Z.HasChanged || !Context.Registers.Z.IsModal)
            parts.Add($"Z{FormatValue(Context.Registers.Z.Value)}");

        // Углы для 5-осевой обработки
        if (Context.Config.MultiAxis?.EnableRtcp == true)
        {
            if (Context.Registers.A.HasChanged || !Context.Registers.A.IsModal)
                parts.Add($"A{FormatValue(Context.Registers.A.Value, "F3.2")}");
            if (Context.Registers.B.HasChanged || !Context.Registers.B.IsModal)
                parts.Add($"B{FormatValue(Context.Registers.B.Value, "F3.2")}");
        }

        // Подача
        if (includeFeed && Context.Registers.F.Value > 0)
            parts.Add($"F{FormatValue(Context.Registers.F.Value, "F3.1")}");

        Context.Registers.ResetChangeFlags();

        if (parts.Count > 1)
            await WriteLineAsync(string.Join(" ", parts));
    }

    /// <summary>
    /// Проверить безопасность перемещения
    /// </summary>
    protected bool IsSafeMove(double z, out string? errorMessage)
    {
        errorMessage = null;
        var safety = Context.Config.Safety;

        if (z < safety.RetractPlane)
        {
            errorMessage = $"Z={z:F3} ниже плоскости втягивания Z={safety.RetractPlane:F3}";
            return false;
        }

        if (Math.Abs(z) > safety.ClearancePlane)
        {
            errorMessage = $"Z={z:F3} превышает безопасную высоту Z={safety.ClearancePlane:F3}";
            return false;
        }

        return true;
    }
}
