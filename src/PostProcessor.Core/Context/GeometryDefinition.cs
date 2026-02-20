namespace PostProcessor.Core.Context;

/// <summary>
/// Базовый класс для геометрических примитивов
/// </summary>
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
