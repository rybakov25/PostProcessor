namespace PostProcessor.Core.Context;

public enum SpindleDirection
{
    Off = 5,
    Clockwise = 3,
    CounterClockwise = 4
}

public enum CoolantMode
{
    Off = 9,
    Flood = 8,       // M08
    Mist = 7,        // M07
    ThroughSpindle = 81 // M81 (для глубокого сверления)
}

public class MachineState
{
    public ToolInfo? CurrentTool { get; set; }
    public SpindleDirection SpindleState { get; set; } = SpindleDirection.Off;
    public CoolantMode CoolantState { get; set; } = CoolantMode.Off;
    public int ActiveCoordinateSystem { get; set; } = 1; // G54 по умолчанию
    public CoordinateSystem[] WorkOffsets { get; } = new CoordinateSystem[6]; // G54-G59
    public string? LastMCode { get; set; } // Последний выведенный M-код для проверки модальности

    public MachineState()
    {
        // Инициализация стандартных систем координат
        for (int i = 0; i < 6; i++)
            WorkOffsets[i] = new CoordinateSystem(i + 1, 0.0, 0.0, 0.0);
    }

    public string ActiveGCodeSystem => WorkOffsets[ActiveCoordinateSystem - 1].GCode;
}
