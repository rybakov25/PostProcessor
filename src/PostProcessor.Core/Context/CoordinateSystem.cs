namespace PostProcessor.Core.Context;

public record CoordinateSystem(
    int Number, // 1 = G54, 2 = G55, ...
    double XOffset,
    double YOffset,
    double ZOffset,
    double AOffset = 0.0,
    double BOffset = 0.0,
    double COffset = 0.0
)
{
    public string GCode => $"G5{Number + 3}"; // G54 для номера 1
}
