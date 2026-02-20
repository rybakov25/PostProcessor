using System.Globalization;

namespace PostProcessor.Core.Context;

public class Register
{
    public string Name { get; }           // "X", "Y", "F", "S"...
    public double Value { get; private set; }
    public bool IsModal { get; }          // Сохранять значение между блоками
    public string Format { get; }         // "F4.3", "D5" для вывода
    public bool HasChanged { get; private set; }

    private double _previousValue;

    public Register(string name, double initialValue = 0.0, bool isModal = true, string format = "F4.3")
    {
        Name = name;
        Value = initialValue;
        _previousValue = initialValue;
        IsModal = isModal;
        Format = format;
        HasChanged = false;
    }

    public void SetValue(double newValue)
    {
        HasChanged = Math.Abs(newValue - Value) > 1e-6;
        _previousValue = Value;
        Value = newValue;
    }

    public void ResetChangeFlag() => HasChanged = false;

    public string FormatValue() => Value.ToString(Format, CultureInfo.InvariantCulture);

    public override string ToString() => $"{Name}={FormatValue()}";
}
