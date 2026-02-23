using PostProcessor.Core.Config.Models;
using PostProcessor.Core.Models;
using System.Collections.Concurrent;

namespace PostProcessor.Core.Context;

public class PostContext : IAsyncDisposable
{
    public RegisterSet Registers { get; } = new();
    public MachineState Machine { get; } = new();
    public CatiaContext Catia { get; } = new();
    public StreamWriter Output { get; }
    
    /// <summary>
    /// Умный формирователь NC-блоков с модальной проверкой
    /// </summary>
    public BlockWriter BlockWriter { get; }

    /// <summary>
    /// Кэш состояний для отслеживания изменений переменных (IMSPost-style LAST_* variables)
    /// </summary>
    public StateCache StateCache { get; } = new();

    /// <summary>
    /// Параметры безопасности станка (ограничения хода, максимальные скорости)
    /// </summary>
    public SafetyParameters SafetyParameters { get; set; } = new SafetyParameters();

    /// <summary>
    /// Системные переменные (аналог системных переменных в IMSpost: SYSTEM.*, GLOBAL.*)
    /// Хранит пользовательские и служебные переменные времени выполнения
    /// </summary>
    private readonly ConcurrentDictionary<string, object> _systemVariables = new();

    /// <summary>
    /// Кэш геометрических примитивов (точки, линии, окружности)
    /// Соответствует переменным системы в IMSpost (раздел 3.5 "Geometry Handling")
    /// </summary>
    public ConcurrentDictionary<string, GeometryDefinition> GeometryCache { get; } = new();

    /// <summary>
    /// Кэш инструментов (отдельно от геометрии)
    /// </summary>
    public ConcurrentDictionary<string, ToolInfo> ToolCache { get; } = new();

    /// <summary>
    /// Буфер для аппроксимации дуг (накопление точек для расчёта)
    /// </summary>
    public ConcurrentBag<(double X, double Y, double Z)> ArcFitBuffer { get; } = new();

    /// <summary>
    /// Конфигурация контроллера с параметрами безопасности
    /// </summary>
    public ControllerConfig Config { get; set; } = new();

    private bool _disposed = false;

    private int _commandCount;
    private int _motionCount;
    private int _toolChanges;

    public (int CommandCount, int MotionCount, int ToolChanges) GetStatistics() => (_commandCount, _motionCount, _toolChanges);

    public PostContext(StreamWriter output)
    {
        Output = output;
        BlockWriter = new BlockWriter(output);
        
        // Регистрация регистров в BlockWriter для автоматического отслеживания
        BlockWriter.AddWords(
            Registers.X, Registers.Y, Registers.Z,
            Registers.A, Registers.B, Registers.C,
            Registers.F, Registers.S, Registers.T
        );
    }

    /// <summary>
    /// Установка системной переменной
    /// </summary>
    public void SetSystemVariable(string name, object value)
    {
        _systemVariables[name] = value;
    }

    /// <summary>
    /// Получение системной переменной
    /// </summary>
    public T GetSystemVariable<T>(string name, T defaultValue = default!)
    {
        return _systemVariables.TryGetValue(name, out var value) && value is T typedValue
            ? typedValue
            : defaultValue;
    }

    // === StateCache методы (IMSPost-style LAST_* variables) ===

    /// <summary>
    /// Проверить, изменилось ли значение по сравнению с кэшем
    /// </summary>
    public bool HasStateChanged<T>(string key, T currentValue)
    {
        return StateCache.HasChanged(key, currentValue);
    }

    /// <summary>
    /// Обновить значение в кэше состояний
    /// </summary>
    public void UpdateState<T>(string key, T value)
    {
        StateCache.Update(key, value);
    }

    /// <summary>
    /// Получить значение из кэша состояний
    /// </summary>
    public T GetState<T>(string key, T defaultValue = default!)
    {
        return StateCache.Get(key, defaultValue);
    }

    /// <summary>
    /// Получить или установить значение в кэше состояний
    /// </summary>
    public T GetOrSetState<T>(string key, T defaultValue = default!)
    {
        return StateCache.GetOrSet(key, defaultValue);
    }

    /// <summary>
    /// Сбросить значение из кэша состояний
    /// </summary>
    public void ResetState(string key)
    {
        StateCache.Remove(key);
    }

    /// <summary>
    /// Сбросить весь кэш состояний
    /// </summary>
    public void ResetAllStates()
    {
        StateCache.Clear();
    }

    // === CycleCache методы ===

    /// <summary>
    /// Записать цикл, если параметры отличаются от закэшированных
    /// </summary>
    /// <param name="cycleName">Имя цикла (например, "CYCLE800")</param>
    /// <param name="parameters">Параметры цикла</param>
    /// <returns>true если записано полное определение</returns>
    public bool WriteCycleIfDifferent(string cycleName, Dictionary<string, object> parameters)
    {
        return CycleCacheHelper.WriteIfDifferent(this, cycleName, parameters);
    }

    /// <summary>
    /// Сбросить кэш цикла
    /// </summary>
    public void ResetCycleCache(string cycleName)
    {
        CycleCacheHelper.Reset(this, cycleName);
    }

    public async IAsyncEnumerable<PostEvent> ProcessCommandAsync(APTCommand command)
    {
        _commandCount++;
        if (command.MajorWord is "goto" or "rapid") _motionCount++;
        if (command.MajorWord is "toolno" or "loadtl") _toolChanges++;

        // CATIA-специфичные команды
        switch (command.MajorWord)
        {
            case "catprocess":
                yield return HandleCatProcess(command);
                break;

            case "catproduct":
                yield return HandleCatProduct(command);
                break;

            case "toolpath_type":
                yield return HandleToolpathType(command);
                break;

            case "multax":
                yield return HandleMultiaxis(command);
                break;

            case "tlcomp":
                yield return HandleToolCompensation(command);
                break;

            case "loadtl":
                yield return HandleLoadTool(command);
                break;

            case "toolinf":
                yield return HandleToolInfo(command);
                break;

            case "op_name":
                yield return HandleOperationName(command);
                break;

            case "start_op":
                yield return HandleStartOperation(command);
                break;

            case "opdata":
                yield return HandleOperationData(command);
                break;

            // Стандартные APT-команды
            case "goto":
                yield return await HandleGotoAsync(command);
                break;

            case "rapid":
                yield return await HandleRapidAsync(command);
                break;

            case "fedrat":
                yield return await HandleFeedRateAsync(command);
                break;

            case "spindl":
                yield return await HandleSpindleAsync(command);
                break;

            case "coolnt":
                yield return await HandleCoolantAsync(command);
                break;

            case "tlon":
                yield return await HandleToolOnAsync(command);
                break;

            case "tloff":
                yield return await HandleToolOffAsync(command);
                break;

            // Геометрические определения
            case "point":
                yield return await HandlePointDefinitionAsync(command);
                break;

            case "line":
                yield return await HandleLineDefinitionAsync(command);
                break;

            case "circle":
                yield return await HandleCircleDefinitionAsync(command);
                break;

            // Служебные команды (игнорируем, но не считаем ошибкой)
            case "channel":
            case "catmat":
            case "pptable":
            case "part_open":
            case "partno":
            case "machin":
            case "program":
            case "fini":
                // Не генерируем событий — просто пропускаем
                break;

            // Продолжение многострочной команды
            case "continuation":
                // Обработка зависит от предыдущей команды — реализуется в макросах
                yield return new PostEvent(PostEventType.Custom, command, new() { ["type"] = "continuation" });
                break;

            default:
                // Неизвестная команда — передаём макросам
                yield return new PostEvent(
                    PostEventType.Custom,
                    command,
                    new() { ["warning"] = $"Unknown CATIA command: {command.MajorWord}" }
                );
                break;
        }
    }

    private PostEvent HandleCatProcess(APTCommand cmd)
    {
        if (cmd.MinorWords.Count > 0)
            Catia.CurrentProcessName = cmd.MinorWords[0];

        return new PostEvent(PostEventType.Custom, cmd, new() { ["process"] = Catia.CurrentProcessName });
    }

    private PostEvent HandleCatProduct(APTCommand cmd)
    {
        if (cmd.MinorWords.Count > 0)
            Catia.CurrentProductName = cmd.MinorWords[0];

        return new PostEvent(PostEventType.Custom, cmd, new() { ["product"] = Catia.CurrentProductName });
    }

    private PostEvent HandleToolpathType(APTCommand cmd)
    {
        if (cmd.MinorWords.Contains("3axis"))
            Catia.ToolpathType = CatiaToolpathType.Axis3;
        else if (cmd.MinorWords.Contains("5axis"))
            Catia.ToolpathType = CatiaToolpathType.Axis5;

        return new PostEvent(PostEventType.Custom, cmd, new() { ["toolpath_type"] = Catia.ToolpathType.ToString() });
    }

    private PostEvent HandleMultiaxis(APTCommand cmd)
    {
        Catia.IsMultiaxisEnabled = cmd.MinorWords.Contains("on");
        return new PostEvent(PostEventType.Custom, cmd, new() { ["multiaxis"] = Catia.IsMultiaxisEnabled });
    }

    private PostEvent HandleToolCompensation(APTCommand cmd)
    {
        if (cmd.MinorWords.Contains("left"))
            Catia.CompensationMode = ToolCompensationMode.Left;
        else if (cmd.MinorWords.Contains("right"))
            Catia.CompensationMode = ToolCompensationMode.Right;
        else if (cmd.MinorWords.Contains("adjust"))
            Catia.CompensationMode = ToolCompensationMode.Adjust;
        else
            Catia.CompensationMode = ToolCompensationMode.Off;

        return new PostEvent(PostEventType.Custom, cmd, new() { ["compensation"] = Catia.CompensationMode.ToString() });
    }

    private PostEvent HandleLoadTool(APTCommand cmd)
    {
        if (cmd.NumericValues.Count > 0)
        {
            int toolNumber = (int)Math.Round(cmd.NumericValues[0]);
            double? diameter = cmd.NumericValues.Count > 1 ? cmd.NumericValues[1] : null;

            // Поиск в кэше инструментов
            var toolKey = $"tool_{toolNumber}";
            if (ToolCache.TryGetValue(toolKey, out var tool))
            {
                Machine.CurrentTool = tool;
            }
            else
            {
                // Создание нового инструмента с разумными значениями по умолчанию
                tool = new ToolInfo(
                    Number: toolNumber,
                    Diameter: diameter ?? 10.0,
                    Length: 50.0,
                    Comment: $"Tool {toolNumber}",
                    Type: cmd.MinorWords.FirstOrDefault()
                );
                Machine.CurrentTool = tool;
                ToolCache[toolKey] = tool;
            }
        }

        return new PostEvent(PostEventType.ToolChange, cmd, new()
        {
            ["tool_number"] = Machine.CurrentTool?.Number ?? 0
        });
    }

    private PostEvent HandleToolInfo(APTCommand cmd)
    {
        // Сохранение информации об инструменте в отдельный кэш
        if (cmd.MinorWords.Count > 0)
        {
            var toolKey = cmd.MinorWords[0];
            var tool = new ToolInfo(
                Number: 0, // номер будет установлен при LOADTL
                Diameter: cmd.NumericValues.Count > 0 ? cmd.NumericValues[0] : 0,
                Length: cmd.NumericValues.Count > 1 ? cmd.NumericValues[1] : 0,
                Comment: toolKey,
                Type: cmd.MinorWords.Count > 1 ? cmd.MinorWords[1] : null
            );
            ToolCache[toolKey] = tool;
        }

        return new PostEvent(PostEventType.Custom, cmd, new() { ["tool_info"] = cmd.MinorWords.FirstOrDefault() });
    }

    private PostEvent HandleOperationName(APTCommand cmd)
    {
        if (cmd.MinorWords.Count > 0)
            Catia.CurrentOperationName = cmd.MinorWords[0];

        return new PostEvent(PostEventType.Custom, cmd, new() { ["operation"] = Catia.CurrentOperationName });
    }

    private PostEvent HandleStartOperation(APTCommand cmd)
    {
        // Начало новой операции — сброс состояния
        Registers.ResetChangeFlags();
        return new PostEvent(PostEventType.Custom, cmd, new() { ["operation_start"] = true });
    }

    private PostEvent HandleOperationData(APTCommand cmd)
    {
        // Сохранение параметров операции
        for (int i = 0; i < cmd.MinorWords.Count && i * 2 + 1 < cmd.NumericValues.Count; i++)
        {
            Catia.OperationParameters[cmd.MinorWords[i]] = cmd.NumericValues[i * 2 + 1];
        }

        return new PostEvent(PostEventType.Custom, cmd, new() { ["operation_data"] = Catia.OperationParameters.Count });
    }

    private async Task<PostEvent> HandleGotoAsync(APTCommand cmd)
    {
        // CATIA-специфика: поддержка неполных координат
        double x = cmd.NumericValues.Count > 0 ? cmd.NumericValues[0] : Registers.X.Value;
        double y = cmd.NumericValues.Count > 1 ? cmd.NumericValues[1] : Registers.Y.Value;
        double z = cmd.NumericValues.Count > 2 ? cmd.NumericValues[2] : Registers.Z.Value;

        Registers.X.SetValue(x);
        Registers.Y.SetValue(y);
        Registers.Z.SetValue(z);

        // CATIA 5-axis: обработка вектора направления инструмента (I,J,K)
        if (Catia.IsMultiaxisEnabled && cmd.NumericValues.Count >= 6)
        {
            // Вектор направления (нормализованный)
            double i = cmd.NumericValues[3];
            double j = cmd.NumericValues[4];
            double k = cmd.NumericValues[5];

            // Расчёт углов поворота из вектора направления (упрощённая версия)
            // Для полноценной кинематики потребуется расширение в макросах
            double a = Math.Atan2(j, k) * (180.0 / Math.PI);
            double b = Math.Atan2(i, Math.Sqrt(j * j + k * k)) * (180.0 / Math.PI);

            Registers.A.SetValue(a);
            Registers.B.SetValue(b);
        }

        var payload = new Dictionary<string, object>
        {
            ["motion_type"] = "linear",
            ["x"] = x,
            ["y"] = y,
            ["z"] = z,
            ["is_multiaxis"] = Catia.IsMultiaxisEnabled
        };

        if (Catia.IsMultiaxisEnabled && cmd.NumericValues.Count >= 6)
        {
            payload["i"] = cmd.NumericValues[3];
            payload["j"] = cmd.NumericValues[4];
            payload["k"] = cmd.NumericValues[5];
            payload["a"] = Registers.A.Value;
            payload["b"] = Registers.B.Value;
        }

        return new PostEvent(PostEventType.Motion, cmd, payload);
    }

    private async Task<PostEvent> HandleRapidAsync(APTCommand cmd) => await HandleGotoAsync(cmd);

    private async Task<PostEvent> HandleFeedRateAsync(APTCommand cmd)
    {
        if (cmd.NumericValues.Count > 0)
        {
            double feed = cmd.NumericValues[0];

            // CATIA использует MMPM (метры/минуту) по умолчанию
            if (cmd.MinorWords.Contains("mps")) // meters per second
                feed *= 60000;
            else if (!cmd.MinorWords.Contains("mmpm")) // если не указано явно — мм/мин
                ; // уже в мм/мин

            Registers.F.SetValue(feed);
        }

        return new PostEvent(PostEventType.FeedChange, cmd, new() { ["feed"] = Registers.F.Value });
    }

    private async Task<PostEvent> HandleSpindleAsync(APTCommand cmd)
    {
        if (cmd.NumericValues.Count > 0)
            Registers.S.SetValue(cmd.NumericValues[0]);

        var direction = SpindleDirection.Clockwise;
        if (cmd.MinorWords.Contains("cclw"))
            direction = SpindleDirection.CounterClockwise;
        else if (cmd.MinorWords.Contains("off"))
            direction = SpindleDirection.Off;

        Machine.SpindleState = direction;

        return new PostEvent(PostEventType.SpindleChange, cmd, new()
        {
            ["rpm"] = Registers.S.Value,
            ["direction"] = direction.ToString()
        });
    }

    private async Task<PostEvent> HandleCoolantAsync(APTCommand cmd)
    {
        var state = cmd.MinorWords.Contains("off") ? CoolantMode.Off : CoolantMode.Flood;
        Machine.CoolantState = state;

        return new PostEvent(PostEventType.CoolantChange, cmd, new() { ["mode"] = state.ToString() });
    }

    private async Task<PostEvent> HandleToolOnAsync(APTCommand cmd)
    {
        // Включение компенсации радиуса инструмента
        if (cmd.MinorWords.Contains("left"))
            Catia.CompensationMode = ToolCompensationMode.Left;
        else if (cmd.MinorWords.Contains("right"))
            Catia.CompensationMode = ToolCompensationMode.Right;
        else
            Catia.CompensationMode = ToolCompensationMode.Adjust;

        return new PostEvent(PostEventType.Custom, cmd, new()
        {
            ["compensation"] = "on",
            ["side"] = Catia.CompensationMode.ToString()
        });
    }

    private async Task<PostEvent> HandleToolOffAsync(APTCommand cmd)
    {
        Catia.CompensationMode = ToolCompensationMode.Off;
        return new PostEvent(PostEventType.Custom, cmd, new() { ["compensation"] = "off" });
    }

    // Геометрические определения (упрощённая реализация)
    private async Task<PostEvent> HandlePointDefinitionAsync(APTCommand cmd) =>
        new PostEvent(PostEventType.GeometryDefined, cmd, new() { ["type"] = "point" });

    private async Task<PostEvent> HandleLineDefinitionAsync(APTCommand cmd) =>
        new PostEvent(PostEventType.GeometryDefined, cmd, new() { ["type"] = "line" });

    private async Task<PostEvent> HandleCircleDefinitionAsync(APTCommand cmd) =>
        new PostEvent(PostEventType.GeometryDefined, cmd, new() { ["type"] = "circle" });

    // Вспомогательные методы для макросов
    /// <summary>
    /// Записать NC-блок через BlockWriter с автоматической модальностью
    /// </summary>
    public void WriteBlock(bool includeBlockNumber = true)
    {
        BlockWriter.WriteBlock(includeBlockNumber);
    }
    
    /// <summary>
    /// Записать строку напрямую (для комментариев, заголовков)
    /// </summary>
    public void Write(string text)
    {
        BlockWriter.WriteLine(text);
    }
    
    /// <summary>
    /// Записать комментарий в формате станка
    /// </summary>
    public void Comment(string text)
    {
        BlockWriter.WriteComment(text);
    }
    
    /// <summary>
    /// Скрыть регистры (не выводить до изменения)
    /// </summary>
    public void HideRegisters(params Register[] registers)
    {
        BlockWriter.Hide(registers);
    }
    
    /// <summary>
    /// Показать регистры (вывести обязательно)
    /// </summary>
    public void ShowRegisters(params Register[] registers)
    {
        BlockWriter.Show(registers);
    }
    
    /// <summary>
    /// Форматировать движение в блок (устаревший метод, использовать BlockWriter)
    /// </summary>
    public string FormatMotionBlock(bool isRapid = false)
    {
        var changed = Registers.ChangedRegisters().ToList();
        if (!changed.Any())
            return string.Empty;

        var gcode = isRapid ? "G0" : "G1";
        var parts = new List<string> { gcode };

        foreach (var reg in changed.Where(r => "XYZAB".Contains(r.Name)))
            parts.Add($"{reg.Name}{reg.FormatValue()}");

        if (!isRapid && (Registers.F.HasChanged || !Registers.F.IsModal))
            parts.Add($"F{Registers.F.FormatValue()}");

        Registers.ResetChangeFlags();
        return string.Join(" ", parts);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await Output.DisposeAsync();
            _disposed = true;
        }
    }

    // Вложенные типы для геометрии
    public abstract class GeometryDefinition { }

    public class Point : GeometryDefinition
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public Point(double x, double y, double z) => (X, Y, Z) = (x, y, z);

        public override string ToString() => $"POINT({X:F3},{Y:F3},{Z:F3})";
    }

    public class Line : GeometryDefinition
    {
        public Point Start { get; }
        public Point End { get; }
        public Line(Point start, Point end) => (Start, End) = (start, end);

        public override string ToString() => $"LINE({Start},{End})";
    }

    public class Circle : GeometryDefinition
    {
        public Point Center { get; }
        public double Radius { get; }
        public Circle(Point center, double radius) => (Center, Radius) = (center, radius);

        public override string ToString() => $"CIRCLE({Center.X:F3},{Center.Y:F3},{Center.Z:F3},R{Radius:F3})";
    }
}
