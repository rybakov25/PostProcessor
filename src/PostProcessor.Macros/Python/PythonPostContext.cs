using System;
using System.Collections.Generic;
using System.IO;
using PostProcessor.Core.Config.Models;
using PostProcessor.Core.Context;

namespace PostProcessor.Macros.Python;

/// <summary>
/// Python-обёртка для контекста постпроцессора
/// Предоставляет API для макросов на Python
/// </summary>
public class PythonPostContext
{
    private readonly PostContext _context;

    public PythonPostContext(PostContext context)
    {
        _context = context;
        registers = new PythonRegisters(context.Registers);
        config = new PythonConfig(context.Config);
        machine = new PythonMachineState(context.Machine);
        system = new PythonSystemVariables(context);
        globalVars = new PythonGlobalVariables(context);
    }

    // === Регистры ===
    public PythonRegisters registers { get; }

    // === Конфигурация ===
    public PythonConfig config { get; }

    // === Состояние машины ===
    public PythonMachineState machine { get; }

    // === Системные переменные (SYSTEM.*) ===
    public PythonSystemVariables system { get; }

    // === Глобальные переменные (GLOBAL.*) ===
    public PythonGlobalVariables globalVars { get; }
    
    // === Переменные состояния ===
    public double? currentFeed { get; set; }
    public string currentMotionType { get; set; } = "LINEAR";

    // === Методы управления нумерацией ===
    public void setBlockNumbering(int start = 1, int increment = 2, bool enabled = true)
    {
        _context.SetSystemVariable("BLOCK_NUMBER", start);
        _context.SetSystemVariable("BLOCK_INCREMENT", increment);
        _context.SetSystemVariable("BLOCK_NUMBER_ENABLED", enabled);
    }

    public int getNextBlockNumber()
    {
        int num = _context.GetSystemVariable("BLOCK_NUMBER", 1);
        int increment = _context.GetSystemVariable("BLOCK_INCREMENT", 2);
        _context.SetSystemVariable("BLOCK_NUMBER", num + increment);
        return num;
    }

    // === StateCache методы (IMSPost-style LAST_* variables) ===

    /// <summary>
    /// Получить значение из кэша состояний
    /// </summary>
    public T cacheGet<T>(string key, T defaultValue = default!)
    {
        return _context.StateCache.Get(key, defaultValue);
    }

    /// <summary>
    /// Установить значение в кэш состояний
    /// </summary>
    public void cacheSet<T>(string key, T value)
    {
        _context.StateCache.Update(key, value);
    }

    /// <summary>
    /// Проверить, изменилось ли значение по сравнению с кэшем
    /// </summary>
    public bool cacheHasChanged<T>(string key, T value)
    {
        return _context.StateCache.HasChanged(key, value);
    }

    /// <summary>
    /// Получить или установить значение в кэше состояний
    /// </summary>
    public T cacheGetOrSet<T>(string key, T defaultValue = default!)
    {
        return _context.StateCache.GetOrSet(key, defaultValue);
    }

    /// <summary>
    /// Сбросить значение из кэша состояний
    /// </summary>
    public void cacheReset(string key)
    {
        _context.StateCache.Remove(key);
    }

    /// <summary>
    /// Сбросить весь кэш состояний
    /// </summary>
    public void cacheResetAll()
    {
        _context.StateCache.Clear();
    }

    // === CycleCache методы ===

    /// <summary>
    /// Записать цикл, если параметры отличаются от закэшированных
    /// </summary>
    /// <param name="cycleName">Имя цикла (например, "CYCLE800")</param>
    /// <param name="parameters">Параметры цикла</param>
    /// <returns>true если записано полное определение</returns>
    public bool cycleWriteIfDifferent(string cycleName, Dictionary<string, object> parameters)
    {
        return _context.WriteCycleIfDifferent(cycleName, parameters);
    }

    /// <summary>
    /// Сбросить кэш цикла
    /// </summary>
    public void cycleReset(string cycleName)
    {
        _context.ResetCycleCache(cycleName);
    }

    /// <summary>
    /// Получить или создать кэш цикла
    /// </summary>
    /// <param name="cycleName">Имя цикла</param>
    /// <returns>CycleCache</returns>
    public CycleCache cycleGetCache(string cycleName)
    {
        return CycleCacheHelper.GetOrCreate(_context, cycleName);
    }

    // === NumericNCWord методы ===

    /// <summary>
    /// Получить NumericNCWord по адресу
    /// </summary>
    /// <param name="address">Адрес (X, Y, Z, F, S...)</param>
    /// <returns>NumericNCWord</returns>
    public NumericNCWord getNumericWord(string address)
    {
        return _context.GetNumericWord(address);
    }

    /// <summary>
    /// Установить значение регистра через NumericNCWord (с форматированием из конфига)
    /// </summary>
    /// <param name="address">Адрес регистра</param>
    /// <param name="value">Значение</param>
    public void setNumericValue(string address, double value)
    {
        _context.SetNumericValue(address, value);
    }

    /// <summary>
    /// Получить отформатированное значение регистра
    /// </summary>
    /// <param name="address">Адрес регистра</param>
    /// <returns>Отформатированная строка</returns>
    public string getFormattedValue(string address)
    {
        var word = getNumericWord(address);
        return word.ToNCString();
    }

    /// <summary>
    /// Записать комментарий с использованием стиля из конфига
    /// </summary>
    /// <param name="text">Текст комментария</param>
    public void writeComment(string text)
    {
        comment(text);
    }

    // === Методы вывода ===
    /// <summary>
    /// Записать строку через BlockWriter с автоматической модальностью
    /// НЕ добавляет newline в конце - используйте writeBlock() для вывода блока
    /// </summary>
    public void write(string line, bool suppressBlock = false)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            _context.Output.Write(line);
            _context.Output.Flush();
        }
    }

    /// <summary>
    /// Записать строку и сразу вывести блок с модальной проверкой
    /// Добавляет newline после блока
    /// </summary>
    public void writeln(string line = "")
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            _context.Output.Write(line);
        }
        _context.BlockWriter.WriteBlock(true);
        _context.Output.Flush();
    }

    /// <summary>
    /// Записать комментарий в формате станка
    /// Использует стиль из конфига (parentheses/semicolon/both)
    /// </summary>
    public void comment(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            _context.Comment(text);
            _context.Output.Flush();
        }
    }

    public void warning(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            _context.Output.WriteLine($"(WARNING: {text})");
            _context.Output.Flush();
        }
    }
    
    /// <summary>
    /// Записать NC-блок через BlockWriter
    /// Добавляет newline в конце блока
    /// </summary>
    public void writeBlock(bool includeBlockNumber = true)
    {
        _context.BlockWriter.WriteBlock(includeBlockNumber);
        _context.Output.WriteLine();  // Add newline after block
        _context.Output.Flush();
    }
    
    /// <summary>
    /// Скрыть регистры (не выводить до изменения)
    /// </summary>
    public void hide(params string[] registerNames)
    {
        foreach (var name in registerNames)
        {
            var reg = getRegisterByName(name);
            if (reg != null)
                _context.BlockWriter.Hide(reg);
        }
    }
    
    /// <summary>
    /// Показать регистры (вывести обязательно)
    /// </summary>
    public void show(params string[] registerNames)
    {
        foreach (var name in registerNames)
        {
            var reg = getRegisterByName(name);
            if (reg != null)
                _context.BlockWriter.Show(reg);
        }
    }
    
    private Register? getRegisterByName(string name)
    {
        return name.ToUpper() switch
        {
            "X" => _context.Registers.X,
            "Y" => _context.Registers.Y,
            "Z" => _context.Registers.Z,
            "A" => _context.Registers.A,
            "B" => _context.Registers.B,
            "C" => _context.Registers.C,
            "F" => _context.Registers.F,
            "S" => _context.Registers.S,
            "T" => _context.Registers.T,
            _ => null
        };
    }

    // === Утилиты ===
    public double round(double value, int decimals = 3)
    {
        return Math.Round(value, decimals);
    }

    public string format(double value, string format = "F3")
    {
        return value.ToString(format);
    }
}

/// <summary>
/// Python-обёртка для регистров
/// </summary>
public class PythonRegisters
{
    private readonly RegisterSet _registers;
    
    public PythonRegisters(RegisterSet registers)
    {
        _registers = registers;
    }
    
    // Координаты
    public double x
    {
        get => _registers.X.Value;
        set => _registers.X.SetValue(value);
    }
    
    public double y
    {
        get => _registers.Y.Value;
        set => _registers.Y.SetValue(value);
    }
    
    public double z
    {
        get => _registers.Z.Value;
        set => _registers.Z.SetValue(value);
    }
    
    // Углы
    public double a
    {
        get => _registers.A.Value;
        set => _registers.A.SetValue(value);
    }
    
    public double b
    {
        get => _registers.B.Value;
        set => _registers.B.SetValue(value);
    }
    
    public double c
    {
        get => _registers.C.Value;
        set => _registers.C.SetValue(value);
    }
    
    // Подача и шпиндель
    public double f
    {
        get => _registers.F.Value;
        set => _registers.F.SetValue(value);
    }
    
    public double s
    {
        get => _registers.S.Value;
        set => _registers.S.SetValue(value);
    }
    
    public int t
    {
        get => (int)_registers.T.Value;
        set => _registers.T.SetValue(value);
    }
}

/// <summary>
/// Python-обёртка для конфигурации
/// </summary>
public class PythonConfig
{
    private readonly ControllerConfig _config;
    
    public PythonConfig(ControllerConfig config)
    {
        _config = config;
    }
    
    public string name => _config.Name;
    public string machineProfile => _config.MachineProfile ?? "";
    
    public PythonSafety safety { get; }
    
    public PythonMultiAxis multiAxis { get; }
    
    // Пользовательские параметры
    public object getParameter(string key, object defaultValue = null)
    {
        if (_config.CustomParameters.TryGetValue(key, out var value))
            return value ?? defaultValue;
        return defaultValue;
    }
    
    public bool getParameterBool(string key, bool defaultValue = false)
    {
        if (_config.CustomParameters.TryGetValue(key, out var value) && value is bool b)
            return b;
        return defaultValue;
    }
    
    public double getParameterDouble(string key, double defaultValue = 0.0)
    {
        if (_config.CustomParameters.TryGetValue(key, out var value))
        {
            if (value is double d) return d;
            if (value is int i) return i;
            if (value is string s && double.TryParse(s, out var parsed)) return parsed;
        }
        return defaultValue;
    }
    
    public string getParameterString(string key, string defaultValue = "")
    {
        if (_config.CustomParameters.TryGetValue(key, out var value) && value != null)
            return value.ToString();
        return defaultValue;
    }
    
    // M-code access
    public PythonMCode mcode => new PythonMCode(_config);
}

/// <summary>
/// Python wrapper for M-codes
/// </summary>
public class PythonMCode
{
    private readonly ControllerConfig _config;
    
    public PythonMCode(ControllerConfig config)
    {
        _config = config;
    }
    
    public string programEnd => _config.GetCustomMCode("M30", "programEnd");
    public string programStop => _config.GetCustomMCode("M00", "programStop");
    public string spindleCW => _config.GetCustomMCode("M3", "spindleCW");
    public string spindleCCW => _config.GetCustomMCode("M4", "spindleCCW");
    public string spindleStop => _config.GetCustomMCode("M5", "spindleStop");
    public string coolantOn => _config.GetCustomMCode("M8", "coolantOn");
    public string coolantOff => _config.GetCustomMCode("M9", "coolantOff");
    public string toolChange => _config.GetCustomMCode("M6", "toolChange");
}

/// <summary>
/// Python-обёртка для параметров безопасности
/// </summary>
public class PythonSafety
{
    private readonly SafetyParameters _safety;
    
    public PythonSafety(SafetyParameters safety)
    {
        _safety = safety;
    }
    
    public double clearancePlane => _safety.ClearancePlane;
    public double retractPlane => _safety.RetractPlane;
    public double maxFeedRate => _safety.MaxFeedRate;
    public double maxSpindleSpeed => _safety.MaxSpindleSpeed;
}

/// <summary>
/// Python-обёртка для параметров многоосевой обработки
/// </summary>
public class PythonMultiAxis
{
    private readonly MultiAxisParameters? _multiAxis;
    
    public PythonMultiAxis(MultiAxisParameters? multiAxis)
    {
        _multiAxis = multiAxis;
    }
    
    public bool enableRtcp => _multiAxis?.EnableRtcp ?? false;
    public double maxA => _multiAxis?.MaxA ?? 120.0;
    public double minA => _multiAxis?.MinA ?? -120.0;
    public double maxB => _multiAxis?.MaxB ?? 360.0;
    public double minB => _multiAxis?.MinB ?? 0.0;
    public string strategy => _multiAxis?.Strategy ?? "cartesian";
}

/// <summary>
/// Python-обёртка для состояния машины
/// </summary>
public class PythonMachineState
{
    private readonly MachineState _state;

    public PythonMachineState(MachineState state)
    {
        _state = state;
    }

    public int currentTool => _state.CurrentTool?.Number ?? 0;
    public string spindleState => _state.SpindleState.ToString();
    public string coolantState => _state.CoolantState.ToString();
    public int activeCoordinateSystem => _state.ActiveCoordinateSystem;
}

/// <summary>
/// Python-обёртка для системных переменных (SYSTEM.*)
/// </summary>
public class PythonSystemVariables
{
    private readonly PostContext _context;

    public PythonSystemVariables(PostContext context)
    {
        _context = context;
    }

    public string this[string name]
    {
        get => _context.GetSystemVariable(name, string.Empty);
        set => _context.SetSystemVariable(name, value);
    }

    public double GetDouble(string name, double defaultValue = 0.0)
        => _context.GetSystemVariable(name, defaultValue);

    public void SetDouble(string name, double value)
        => _context.SetSystemVariable(name, value);

    public int GetInt(string name, int defaultValue = 0)
        => _context.GetSystemVariable(name, defaultValue);

    public void SetInt(string name, int value)
        => _context.SetSystemVariable(name, value);

    public bool GetBool(string name, bool defaultValue = false)
        => _context.GetSystemVariable(name, defaultValue);

    public void SetBool(string name, bool value)
        => _context.SetSystemVariable(name, value);

    // Common system variables
    public string MOTION
    {
        get => _context.GetSystemVariable("MOTION", "LINEAR");
        set => _context.SetSystemVariable("MOTION", value);
    }

    public string SPINDLE_NAME
    {
        get => _context.GetSystemVariable("SPINDLE_NAME", "S");
        set => _context.SetSystemVariable("SPINDLE_NAME", value);
    }

    public string SPINDLE
    {
        get => _context.GetSystemVariable("SPINDLE", "");
        set => _context.SetSystemVariable("SPINDLE", value);
    }

    public string TECHNOLOGY_TYPE
    {
        get => _context.GetSystemVariable("TECHNOLOGY_TYPE", "ALL");
        set => _context.SetSystemVariable("TECHNOLOGY_TYPE", value);
    }

    public double TOOL_LENGTH
    {
        get => _context.GetSystemVariable("TOOL_LENGTH", 0.0);
        set => _context.SetSystemVariable("TOOL_LENGTH", value);
    }

    public int CIRCTYPE
    {
        get => _context.GetSystemVariable("CIRCTYPE", 0);
        set => _context.SetSystemVariable("CIRCTYPE", value);
    }

    public double MAX_CSS
    {
        get => _context.GetSystemVariable("MAX_CSS", 0.0);
        set => _context.SetSystemVariable("MAX_CSS", value);
    }

    public int LINTOL
    {
        get => _context.GetSystemVariable("LINTOL", 0);
        set => _context.SetSystemVariable("LINTOL", value);
    }

    public double LINTOL_LINEAR
    {
        get => _context.GetSystemVariable("LINTOL_LINEAR", 0.0);
        set => _context.SetSystemVariable("LINTOL_LINEAR", value);
    }

    public double LINTOL_ROTARY
    {
        get => _context.GetSystemVariable("LINTOL_ROTARY", 0.0);
        set => _context.SetSystemVariable("LINTOL_ROTARY", value);
    }

    public int SURFACE
    {
        get => _context.GetSystemVariable("SURFACE", 1);
        set => _context.SetSystemVariable("SURFACE", value);
    }

    public string CUT_TOOL_COMPONENT
    {
        get => _context.GetSystemVariable("CUT_TOOL_COMPONENT", "TOOL");
        set => _context.SetSystemVariable("CUT_TOOL_COMPONENT", value);
    }

    public double MTIME
    {
        get => _context.GetSystemVariable("MTIME", 0.0);
        set => _context.SetSystemVariable("MTIME", value);
    }

    public double MTOOL_TIME
    {
        get => _context.GetSystemVariable("MTOOL_TIME", 0.0);
        set => _context.SetSystemVariable("MTOOL_TIME", value);
    }

    public int TIME_CALCULATION
    {
        get => _context.GetSystemVariable("TIME_CALCULATION", 1);
        set => _context.SetSystemVariable("TIME_CALCULATION", value);
    }

    public int SEQUENCE_OUTPUT
    {
        get => _context.GetSystemVariable("SEQUENCE_OUTPUT", 0);
        set => _context.SetSystemVariable("SEQUENCE_OUTPUT", value);
    }

    public string AXIS
    {
        get => _context.GetSystemVariable("AXIS", "X,Y,Z,A,B,C");
        set => _context.SetSystemVariable("AXIS", value);
    }

    public string PLANE_X
    {
        get => _context.GetSystemVariable("PLANE_X", "X");
        set => _context.SetSystemVariable("PLANE_X", value);
    }

    public string PLANE_Y
    {
        get => _context.GetSystemVariable("PLANE_Y", "Y");
        set => _context.SetSystemVariable("PLANE_Y", value);
    }

    public string PLANE_Z
    {
        get => _context.GetSystemVariable("PLANE_Z", "Z");
        set => _context.SetSystemVariable("PLANE_Z", value);
    }

    public string CIRCLE_CENTER_X
    {
        get => _context.GetSystemVariable("CIRCLE_CENTER_X", "I");
        set => _context.SetSystemVariable("CIRCLE_CENTER_X", value);
    }

    public string CIRCLE_CENTER_Y
    {
        get => _context.GetSystemVariable("CIRCLE_CENTER_Y", "J");
        set => _context.SetSystemVariable("CIRCLE_CENTER_Y", value);
    }

    public string CIRCLE_CENTER_Z
    {
        get => _context.GetSystemVariable("CIRCLE_CENTER_Z", "K");
        set => _context.SetSystemVariable("CIRCLE_CENTER_Z", value);
    }

    public string CIRCLE_RADIUS
    {
        get => _context.GetSystemVariable("CIRCLE_RADIUS", "CR");
        set => _context.SetSystemVariable("CIRCLE_RADIUS", value);
    }

    public string CIRCLE_ANGLE
    {
        get => _context.GetSystemVariable("CIRCLE_ANGLE", "AR");
        set => _context.SetSystemVariable("CIRCLE_ANGLE", value);
    }

    public int CONTROLLER_RAPID_TYPE
    {
        get => _context.GetSystemVariable("CONTROLLER_RAPID_TYPE", 0);
        set => _context.SetSystemVariable("CONTROLLER_RAPID_TYPE", value);
    }

    public int MULTAX
    {
        get => _context.GetSystemVariable("MULTAX", 0);
        set => _context.SetSystemVariable("MULTAX", value);
    }

    public string FEEDRATE_NAME
    {
        get => _context.GetSystemVariable("FEEDRATE_NAME", "F");
        set => _context.SetSystemVariable("FEEDRATE_NAME", value);
    }
}

/// <summary>
/// Python-обёртка для глобальных переменных (GLOBAL.*)
/// </summary>
public class PythonGlobalVariables
{
    private readonly PostContext _context;

    public PythonGlobalVariables(PostContext context)
    {
        _context = context;
    }

    public string this[string name]
    {
        get => _context.GetSystemVariable(name, string.Empty);
        set => _context.SetSystemVariable(name, value);
    }

    /// <summary>
    /// Get value with default (generic)
    /// </summary>
    public object Get(string name, object defaultValue = null)
        => _context.GetSystemVariable(name, defaultValue);

    /// <summary>
    /// Set value (generic)
    /// </summary>
    public void Set(string name, object value)
        => _context.SetSystemVariable(name, value);

    public double GetDouble(string name, double defaultValue = 0.0)
        => _context.GetSystemVariable(name, defaultValue);

    public void SetDouble(string name, double value)
        => _context.SetSystemVariable(name, value);

    public int GetInt(string name, int defaultValue = 0)
        => _context.GetSystemVariable(name, defaultValue);

    public void SetInt(string name, int value)
        => _context.SetSystemVariable(name, value);

    public bool GetBool(string name, bool defaultValue = false)
        => _context.GetSystemVariable(name, defaultValue);

    public void SetBool(string name, bool value)
        => _context.SetSystemVariable(name, value);

    // Spindle variables
    public string SPINDLE_DEF
    {
        get => _context.GetSystemVariable("SPINDLE_DEF", "CLW");
        set => _context.SetSystemVariable("SPINDLE_DEF", value);
    }

    public double SPINDLE_RPM
    {
        get => _context.GetSystemVariable("SPINDLE_RPM", 100.0);
        set => _context.SetSystemVariable("SPINDLE_RPM", value);
    }

    public int SPINDLE_BLOCK
    {
        get => _context.GetSystemVariable("SPINDLE_BLOCK", 1);
        set => _context.SetSystemVariable("SPINDLE_BLOCK", value);
    }

    // Tool change variables
    public int TOOLCHNG
    {
        get => _context.GetSystemVariable("TOOLCHNG", 0);
        set => _context.SetSystemVariable("TOOLCHNG", value);
    }

    public int TOOLCHG_IGNORE_SAME
    {
        get => _context.GetSystemVariable("TOOLCHG_IGNORE_SAME", 1);
        set => _context.SetSystemVariable("TOOLCHG_IGNORE_SAME", value);
    }

    public int TOOLCHG_SPINOFF
    {
        get => _context.GetSystemVariable("TOOLCHG_SPINOFF", 0);
        set => _context.SetSystemVariable("TOOLCHG_SPINOFF", value);
    }

    public int TOOLCHG_COOLOFF
    {
        get => _context.GetSystemVariable("TOOLCHG_COOLOFF", 0);
        set => _context.SetSystemVariable("TOOLCHG_COOLOFF", value);
    }

    public string TOOLCHG_TREG
    {
        get => _context.GetSystemVariable("TOOLCHG_TREG", "T");
        set => _context.SetSystemVariable("TOOLCHG_TREG", value);
    }

    public string TOOLCHG_LREG
    {
        get => _context.GetSystemVariable("TOOLCHG_LREG", "D");
        set => _context.SetSystemVariable("TOOLCHG_LREG", value);
    }

    public int TOOL
    {
        get => _context.GetSystemVariable("TOOL", 0);
        set => _context.SetSystemVariable("TOOL", value);
    }

    public int FTOOL
    {
        get => _context.GetSystemVariable("FTOOL", -1);
        set => _context.SetSystemVariable("FTOOL", value);
    }

    public double HVAL
    {
        get => _context.GetSystemVariable("HVAL", 1.0);
        set => _context.SetSystemVariable("HVAL", value);
    }

    public string SPINDLE_NAME
    {
        get => _context.GetSystemVariable("SPINDLE_NAME", "S");
        set => _context.SetSystemVariable("SPINDLE_NAME", value);
    }

    public int TOOLCHG_SEQUENCE
    {
        get => _context.GetSystemVariable("TOOLCHG_SEQUENCE", 0);
        set => _context.SetSystemVariable("TOOLCHG_SEQUENCE", value);
    }

    public int TOOLCHG_SEQUENCE_START
    {
        get => _context.GetSystemVariable("TOOLCHG_SEQUENCE_START", 0);
        set => _context.SetSystemVariable("TOOLCHG_SEQUENCE_START", value);
    }

    public string CHANNEL_TOOL
    {
        get => _context.GetSystemVariable("CHANNEL_TOOL", "");
        set => _context.SetSystemVariable("CHANNEL_TOOL", value);
    }

    public string ORIGINAL_TOOL_DIRECTION
    {
        get => _context.GetSystemVariable("ORIGINAL_TOOL_DIRECTION", "");
        set => _context.SetSystemVariable("ORIGINAL_TOOL_DIRECTION", value);
    }

    public int OPSTOPS_CHECK_TOOL
    {
        get => _context.GetSystemVariable("OPSTOPS_CHECK_TOOL", 0);
        set => _context.SetSystemVariable("OPSTOPS_CHECK_TOOL", value);
    }

    public string OPSTOPS_CODE_TOOL
    {
        get => _context.GetSystemVariable("OPSTOPS_CODE_TOOL", "M01");
        set => _context.SetSystemVariable("OPSTOPS_CODE_TOOL", value);
    }

    // Motion variables
    public string LINEAR_TYPE
    {
        get => _context.GetSystemVariable("LINEAR_TYPE", "LINEAR");
        set => _context.SetSystemVariable("LINEAR_TYPE", value);
    }

    public string RAPID_TYPE
    {
        get => _context.GetSystemVariable("RAPID_TYPE", "RAPID_BREAK");
        set => _context.SetSystemVariable("RAPID_TYPE", value);
    }

    public int SURFACE
    {
        get => _context.GetSystemVariable("SURFACE", 1);
        set => _context.SetSystemVariable("SURFACE", value);
    }

    public string RAPID_SPECIAL
    {
        get => _context.GetSystemVariable("RAPID_SPECIAL", "");
        set => _context.SetSystemVariable("RAPID_SPECIAL", value);
    }

    public int RAPID_RESTORE_FEED
    {
        get => _context.GetSystemVariable("RAPID_RESTORE_FEED", 0);
        set => _context.SetSystemVariable("RAPID_RESTORE_FEED", value);
    }

    // Coolant variables
    public string COOLANT_DEF
    {
        get => _context.GetSystemVariable("COOLANT_DEF", "FLOOD");
        set => _context.SetSystemVariable("COOLANT_DEF", value);
    }

    public int COOLANT_BLOCK
    {
        get => _context.GetSystemVariable("COOLANT_BLOCK", 0);
        set => _context.SetSystemVariable("COOLANT_BLOCK", value);
    }

    // Cutcom variables
    public int CUTCOM_BLOCK
    {
        get => _context.GetSystemVariable("CUTCOM_BLOCK", 1);
        set => _context.SetSystemVariable("CUTCOM_BLOCK", value);
    }

    public string CUTCOM_REG
    {
        get => _context.GetSystemVariable("CUTCOM_REG", "D");
        set => _context.SetSystemVariable("CUTCOM_REG", value);
    }

    public double CUTCOM_RVAL
    {
        get => _context.GetSystemVariable("CUTCOM_RVAL", 1.0);
        set => _context.SetSystemVariable("CUTCOM_RVAL", value);
    }

    public int CUTCOM_DIAMVALUE
    {
        get => _context.GetSystemVariable("CUTCOM_DIAMVALUE", 0);
        set => _context.SetSystemVariable("CUTCOM_DIAMVALUE", value);
    }

    public string CUTCOM_DIR
    {
        get => _context.GetSystemVariable("CUTCOM_DIR", "RIGHT");
        set => _context.SetSystemVariable("CUTCOM_DIR", value);
    }

    // Feed variables
    public int FEED_BLOCK
    {
        get => _context.GetSystemVariable("FEED_BLOCK", 1);
        set => _context.SetSystemVariable("FEED_BLOCK", value);
    }

    public double FEED_PROG
    {
        get => _context.GetSystemVariable("FEED_PROG", 100.0);
        set => _context.SetSystemVariable("FEED_PROG", value);
    }

    public string FEED_INV
    {
        get => _context.GetSystemVariable("FEED_INV", "");
        set => _context.SetSystemVariable("FEED_INV", value);
    }

    public string FEEDMODE
    {
        get => _context.GetSystemVariable("FEEDMODE", "FPM");
        set => _context.SetSystemVariable("FEEDMODE", value);
    }

    // Circle variables
    public string PLANE
    {
        get => _context.GetSystemVariable("PLANE", "Z");
        set => _context.SetSystemVariable("PLANE", value);
    }

    public int CIRCLE_TYPE
    {
        get => _context.GetSystemVariable("CIRCLE_TYPE", 4);
        set => _context.SetSystemVariable("CIRCLE_TYPE", value);
    }

    public int CIRCLE_90
    {
        get => _context.GetSystemVariable("CIRCLE_90", 0);
        set => _context.SetSystemVariable("CIRCLE_90", value);
    }

    public double CIRCLE_MINRAD
    {
        get => _context.GetSystemVariable("CIRCLE_MINRAD", 0.0);
        set => _context.SetSystemVariable("CIRCLE_MINRAD", value);
    }

    public double CIRCLE_MAXRAD
    {
        get => _context.GetSystemVariable("CIRCLE_MAXRAD", 0.0);
        set => _context.SetSystemVariable("CIRCLE_MAXRAD", value);
    }

    public int CIRCLE_ALLPLANE
    {
        get => _context.GetSystemVariable("CIRCLE_ALLPLANE", 0);
        set => _context.SetSystemVariable("CIRCLE_ALLPLANE", value);
    }

    // Strategy variables
    public int STRATEGY_RTCP
    {
        get => _context.GetSystemVariable("STRATEGY_RTCP", 1);
        set => _context.SetSystemVariable("STRATEGY_RTCP", value);
    }

    public int STRATEGY_3X_MILLING
    {
        get => _context.GetSystemVariable("STRATEGY_3X_MILLING", 2);
        set => _context.SetSystemVariable("STRATEGY_3X_MILLING", value);
    }

    public int STRATEGY_3X_CYCLE
    {
        get => _context.GetSystemVariable("STRATEGY_3X_CYCLE", 2);
        set => _context.SetSystemVariable("STRATEGY_3X_CYCLE", value);
    }

    public int STRATEGY_AXIAL_MILLING
    {
        get => _context.GetSystemVariable("STRATEGY_AXIAL_MILLING", 0);
        set => _context.SetSystemVariable("STRATEGY_AXIAL_MILLING", value);
    }

    public int STRATEGY_AXIAL_CYCLE
    {
        get => _context.GetSystemVariable("STRATEGY_AXIAL_CYCLE", 0);
        set => _context.SetSystemVariable("STRATEGY_AXIAL_CYCLE", value);
    }

    // Safety variables
    public int CFG_SAFETY_TLCHNG
    {
        get => _context.GetSystemVariable("CFG_SAFETY_TLCHNG", 1);
        set => _context.SetSystemVariable("CFG_SAFETY_TLCHNG", value);
    }

    public int CFG_SAFETY_WPL_SAFE
    {
        get => _context.GetSystemVariable("CFG_SAFETY_WPL_SAFE", 0);
        set => _context.SetSystemVariable("CFG_SAFETY_WPL_SAFE", value);
    }

    public int CFG_SAFETY_TABLE_ROT
    {
        get => _context.GetSystemVariable("CFG_SAFETY_TABLE_ROT", 1);
        set => _context.SetSystemVariable("CFG_SAFETY_TABLE_ROT", value);
    }

    public int CFG_SAFETY_HEAD_ROT
    {
        get => _context.GetSystemVariable("CFG_SAFETY_HEAD_ROT", 1);
        set => _context.SetSystemVariable("CFG_SAFETY_HEAD_ROT", value);
    }

    // Transition variables
    public int TRANSITION
    {
        get => _context.GetSystemVariable("TRANSITION", 0);
        set => _context.SetSystemVariable("TRANSITION", value);
    }

    public int CFG_TRANSITION_5AXIS
    {
        get => _context.GetSystemVariable("CFG_TRANSITION_5AXIS", 3);
        set => _context.SetSystemVariable("CFG_TRANSITION_5AXIS", value);
    }

    public int CFG_TRANSITION_WPLANE
    {
        get => _context.GetSystemVariable("CFG_TRANSITION_WPLANE", 3);
        set => _context.SetSystemVariable("CFG_TRANSITION_WPLANE", value);
    }

    public int CFG_TRANSITION_3AXIS
    {
        get => _context.GetSystemVariable("CFG_TRANSITION_3AXIS", 3);
        set => _context.SetSystemVariable("CFG_TRANSITION_3AXIS", value);
    }

    // Home variables
    public double HOMEX
    {
        get => _context.GetSystemVariable("HOMEX", 0.0);
        set => _context.SetSystemVariable("HOMEX", value);
    }

    public double HOMEY
    {
        get => _context.GetSystemVariable("HOMEY", 0.0);
        set => _context.SetSystemVariable("HOMEY", value);
    }

    public double HOMEZ
    {
        get => _context.GetSystemVariable("HOMEZ", 0.0);
        set => _context.SetSystemVariable("HOMEZ", value);
    }

    // Force way
    public string FORCE_WAY
    {
        get => _context.GetSystemVariable("FORCE_WAY", "");
        set => _context.SetSystemVariable("FORCE_WAY", value);
    }

    public int FORCE
    {
        get => _context.GetSystemVariable("FORCE", 0);
        set => _context.SetSystemVariable("FORCE", value);
    }

    // WPLANE variables
    public int WPLANE_ONOFF
    {
        get => _context.GetSystemVariable("WPLANE_ONOFF", 1);
        set => _context.SetSystemVariable("WPLANE_ONOFF", value);
    }

    public int WPLANE_NULL
    {
        get => _context.GetSystemVariable("WPLANE_NULL", 1);
        set => _context.SetSystemVariable("WPLANE_NULL", value);
    }

    // Toolpath variables
    string TOOLPATH_TYPE
    {
        get => _context.GetSystemVariable("TOOLPATH_TYPE", "");
        set => _context.SetSystemVariable("TOOLPATH_TYPE", value);
    }

    string WORK_TYPE
    {
        get => _context.GetSystemVariable("WORK_TYPE", "");
        set => _context.SetSystemVariable("WORK_TYPE", value);
    }

    // Cycle variables
    public string LASTCYCLE
    {
        get => _context.GetSystemVariable("LASTCYCLE", "DRILL");
        set => _context.SetSystemVariable("LASTCYCLE", value);
    }

    public int FCYCLE
    {
        get => _context.GetSystemVariable("FCYCLE", 1);
        set => _context.SetSystemVariable("FCYCLE", value);
    }

    // Comment variables
    public int COMMENT_ONOFF
    {
        get => _context.GetSystemVariable("COMMENT_ONOFF", 1);
        set => _context.SetSystemVariable("COMMENT_ONOFF", value);
    }

    public string COMMENT_PREFIX
    {
        get => _context.GetSystemVariable("COMMENT_PREFIX", ";");
        set => _context.SetSystemVariable("COMMENT_PREFIX", value);
    }

    public int COMMENT_UPERCASE
    {
        get => _context.GetSystemVariable("COMMENT_UPERCASE", 1);
        set => _context.SetSystemVariable("COMMENT_UPERCASE", value);
    }

    // Subroutine variables
    public int SUB_EXIST
    {
        get => _context.GetSystemVariable("SUB_EXIST", 1);
        set => _context.SetSystemVariable("SUB_EXIST", value);
    }

    public int SUB_ACTIVE
    {
        get => _context.GetSystemVariable("SUB_ACTIVE", 0);
        set => _context.SetSystemVariable("SUB_ACTIVE", value);
    }

    // Partno variables
    public int PARTNO_GET
    {
        get => _context.GetSystemVariable("PARTNO_GET", 1);
        set => _context.SetSystemVariable("PARTNO_GET", value);
    }

    public int PARTNO_PROG
    {
        get => _context.GetSystemVariable("PARTNO_PROG", 1000);
        set => _context.SetSystemVariable("PARTNO_PROG", value);
    }

    public int PARTNO_SEQNO
    {
        get => _context.GetSystemVariable("PARTNO_SEQNO", 1);
        set => _context.SetSystemVariable("PARTNO_SEQNO", value);
    }

    public string PARTNO_REG
    {
        get => _context.GetSystemVariable("PARTNO_REG", "");
        set => _context.SetSystemVariable("PARTNO_REG", value);
    }

    // Turning/Milling
    public int TURNING_DIAMETER
    {
        get => _context.GetSystemVariable("TURNING_DIAMETER", 1);
        set => _context.SetSystemVariable("TURNING_DIAMETER", value);
    }

    public int MILLING_DIAMETER
    {
        get => _context.GetSystemVariable("MILLING_DIAMETER", 0);
        set => _context.SetSystemVariable("MILLING_DIAMETER", value);
    }
}
