using PostProcessor.Core.Models;

namespace PostProcessor.Core.Context;

public enum CatiaToolpathType
{
    Unknown,
    Axis3,    // 3-axis machining
    Axis5,    // 5-axis machining
    Turn      // Turning operations
}

public enum ToolCompensationMode
{
    Off,
    Left,
    Right,
    Adjust    // CATIA-specific adjustment mode
}

public class CatiaContext
{
    public CatiaToolpathType ToolpathType { get; set; } = CatiaToolpathType.Unknown;
    public bool IsMultiaxisEnabled { get; set; } = false;
    public ToolCompensationMode CompensationMode { get; set; } = ToolCompensationMode.Off;
    public string? CurrentOperationName { get; set; }
    public string? CurrentProcessName { get; set; }
    public string? CurrentProductName { get; set; }
    public Dictionary<string, object> OperationParameters { get; } = new();
    public Dictionary<string, ToolInfo> ToolDatabase { get; } = new();
}
