using PostProcessor.Core.Config.Models;
using PostProcessor.Core.Context;

namespace PostProcessor.Core.Config.Extensions;

/// <summary>
/// Расширения для интеграции конфигурации с контекстом выполнения
/// </summary>
public static class ConfigExtensions
{
    /// <summary>
    /// Применение конфигурации контроллера к контексту выполнения
    /// </summary>
    public static void ApplyToContext(this ControllerConfig config, PostContext context)
    {
        // Применение форматов регистров
        foreach (var (address, format) in config.RegisterFormats)
        {
            var reg = context.Registers.GetOrAdd(address, 0.0, format.IsModal, format.Format);
            // Валидация диапазона значений (если заданы ограничения)
            if (format.MinValue.HasValue && reg.Value < format.MinValue.Value)
                reg.SetValue(format.MinValue.Value);
            if (format.MaxValue.HasValue && reg.Value > format.MaxValue.Value)
                reg.SetValue(format.MaxValue.Value);
        }

        // Применение параметров безопасности
        context.SafetyParameters = config.Safety;

        // Применение параметров 5-осевой обработки
        if (config.MultiAxis != null)
        {
            context.Catia.IsMultiaxisEnabled = true;
            context.SetSystemVariable("MULTIAXIS_MAX_A", config.MultiAxis.MaxA);
            context.SetSystemVariable("MULTIAXIS_MIN_A", config.MultiAxis.MinA);
            context.SetSystemVariable("MULTIAXIS_MAX_B", config.MultiAxis.MaxB);
            context.SetSystemVariable("MULTIAXIS_MIN_B", config.MultiAxis.MinB);
            context.SetSystemVariable("MULTIAXIS_STRATEGY", config.MultiAxis.Strategy);
            context.SetSystemVariable("MULTIAXIS_RTCP", config.MultiAxis.EnableRtcp);
        }

        // Инициализация систем координат
        InitializeWorkCoordinateSystems(context, config.WorkCoordinateSystems);
    }

    private static void InitializeWorkCoordinateSystems(PostContext context, WorkCoordinateSystem[] systems)
    {
        // Инициализация стандартных систем координат G54-G59
        for (int i = 0; i < 6; i++)
        {
            var sys = systems.FirstOrDefault(s => s.Number == i + 1) ??
                      new WorkCoordinateSystem
                      {
                          Number = i + 1,
                          Code = $"G5{i + 4}",
                          XOffset = 0.0,
                          YOffset = 0.0,
                          ZOffset = 0.0
                      };

            context.Machine.WorkOffsets[i] = new CoordinateSystem(
                sys.Number,
                sys.XOffset,
                sys.YOffset,
                sys.ZOffset
            );
        }
    }

    /// <summary>
    /// Форматирование движения с учётом конфигурации контроллера
    /// </summary>
    public static string FormatMotionBlock(this ControllerConfig config, PostContext context, bool isRapid = false)
    {
        var parts = new List<string>();

        // Добавление кода движения (G00/G01)
        var motionFunc = isRapid
            ? config.FunctionCodes.GetValueOrDefault("rapid")?.Code ?? "G00"
            : config.FunctionCodes.GetValueOrDefault("linear")?.Code ?? "G01";
        parts.Add(motionFunc);

        // Добавление координат
        foreach (var axis in new[] { "X", "Y", "Z", "A", "B", "C" })
        {
            var reg = context.Registers.GetOrAdd(axis, 0.0, true, "F4.3");
            if (reg.HasChanged || !reg.IsModal || Math.Abs(reg.Value) > 1e-6)
            {
                var format = config.GetRegisterFormat(axis);
                parts.Add($"{axis}{format.FormatValue(reg.Value)}");
            }
        }

        // Добавление подачи для рабочих перемещений
        if (!isRapid)
        {
            var fReg = context.Registers.F;
            if (fReg.HasChanged || !fReg.IsModal)
            {
                var format = config.GetRegisterFormat("F");
                parts.Add($"F{format.FormatValue(fReg.Value)}");
            }
        }

        context.Registers.ResetChangeFlags();
        return parts.Count > 1 ? string.Join(" ", parts) : string.Empty;
    }

    /// <summary>
    /// Форматирование блока смены инструмента с учётом безопасности
    /// </summary>
    public static async Task FormatToolChangeBlockAsync(this ControllerConfig config, PostContext context, int toolNumber)
    {
        // Безопасный подъём по оси Z
        if (config.Safety.AutoToolChangeRetract)
        {
            var currentZ = context.Registers.Z.Value;
            var safeZ = Math.Max(currentZ, config.Safety.ClearancePlane);

            context.Registers.Z.SetValue(safeZ);
            var zFormat = config.GetRegisterFormat("Z");
            await context.Output.WriteLineAsync($"G0 G90 Z{zFormat.FormatValue(safeZ)}");
            context.Registers.ResetChangeFlags();
        }

        // Смена инструмента
        var tFormat = config.GetRegisterFormat("T");
        var m06 = config.FunctionCodes.GetValueOrDefault("tool_change")?.Code ?? "M06";
        await context.Output.WriteLineAsync($"T{tFormat.FormatValue(toolNumber)} {m06}");

        // Восстановление предыдущей высоты Z (если была в рабочей зоне)
        if (config.Safety.AutoToolChangeRetract && context.Registers.Z.Value < config.Safety.ClearancePlane - 10.0)
        {
            var currentZ = context.Registers.Z.Value;
            context.Registers.Z.SetValue(currentZ);
            var zFormat = config.GetRegisterFormat("Z");
            await context.Output.WriteLineAsync($"G0 Z{zFormat.FormatValue(currentZ)}");
            context.Registers.ResetChangeFlags();
        }
    }

    /// <summary>
    /// Валидация координат на выход за пределы рабочей зоны
    /// </summary>
    public static bool ValidateTravelLimits(
        this ControllerConfig config,
        PostContext context,
        MachineConfig machineConfig,
        double x,
        double y,
        double z)
    {
        if (!config.Safety.EnableTravelLimitsCheck)
            return true;

        // 1. Проверка защищённых зон
        if (machineConfig.IsInProtectedZone(x, y, z))
        {
            context.SetSystemVariable("TRAVEL_LIMIT_EXCEEDED", true);
            context.SetSystemVariable("TRAVEL_LIMIT_REASON", "protected_zone");
            context.SetSystemVariable("PROTECTED_ZONE_NUMBER", machineConfig.ProtectedZones.First(zn => zn.Contains(x, y, z)).Number);
            return false;
        }

        // 2. Проверка линейных лимитов станка (аналог программных ограничителей хода)
        if (x < machineConfig.MinX || x > machineConfig.MaxX ||
            y < machineConfig.MinY || y > machineConfig.MaxY ||
            z < machineConfig.MinZ || z > machineConfig.MaxZ)
        {
            context.SetSystemVariable("TRAVEL_LIMIT_EXCEEDED", true);
            context.SetSystemVariable("TRAVEL_LIMIT_REASON", "machine_limits");

            // Определение оси с нарушением
            if (x < machineConfig.MinX || x > machineConfig.MaxX)
                context.SetSystemVariable("TRAVEL_LIMIT_AXIS", "X");
            else if (y < machineConfig.MinY || y > machineConfig.MaxY)
                context.SetSystemVariable("TRAVEL_LIMIT_AXIS", "Y");
            else
                context.SetSystemVariable("TRAVEL_LIMIT_AXIS", "Z");

            return false;
        }

        // 3. Проверка скорости подачи (если задана в параметрах безопасности)
        if (context.Registers.F.Value > config.Safety.MaxFeedRate)
        {
            context.SetSystemVariable("FEED_LIMIT_EXCEEDED", true);
            context.SetSystemVariable("FEED_LIMIT_MAX", config.Safety.MaxFeedRate);
            return false;
        }

        // 4. Проверка оборотов шпинделя
        if (context.Registers.S.Value > config.Safety.MaxSpindleSpeed)
        {
            context.SetSystemVariable("SPINDLE_LIMIT_EXCEEDED", true);
            context.SetSystemVariable("SPINDLE_LIMIT_MAX", config.Safety.MaxSpindleSpeed);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Форматирование предупреждения о выходе за лимиты
    /// </summary>
    public static string FormatTravelLimitWarning(PostContext context)
    {
        var reason = context.GetSystemVariable<string>("TRAVEL_LIMIT_REASON", "unknown");
        var axis = context.GetSystemVariable<string>("TRAVEL_LIMIT_AXIS", "");
        var zone = context.GetSystemVariable<int>("PROTECTED_ZONE_NUMBER", 0);

        return reason switch
        {
            "protected_zone" => $"WARNING: Tool path enters protected zone {zone}",
            "machine_limits" => $"WARNING: Position exceeds machine limits on axis {axis}",
            _ => "WARNING: Travel limit violation"
        };
    }
}
