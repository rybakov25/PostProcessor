using System.Globalization;

namespace PostProcessor.Core.Context;

/// <summary>
/// Регистр ЧПУ для числовых значений (X, Y, Z, F, S...)
/// Наследуется от NCWord для совместимости с BlockWriter
/// </summary>
public class Register : NCWord
{
    public string Name { get; }           // "X", "Y", "F", "S"...
    public double Value { get; private set; }
    public string Format { get; }         // "F4.3", "D5" для вывода
    
    private double _previousValue;

    public Register(string name, double initialValue = 0.0, bool isModal = true, string format = "F4.3")
    {
        Name = name;
        Address = name;  // Адрес по умолчанию равен имени
        Value = initialValue;
        _previousValue = initialValue;
        IsModal = isModal;
        Format = format;
        _hasChanged = false;
    }

    /// <summary>
    /// Установить новое значение
    /// </summary>
    public void SetValue(double newValue)
    {
        _hasChanged = Math.Abs(newValue - Value) > 1e-6;
        _previousValue = Value;
        Value = newValue;
    }

    /// <summary>
    /// Форматировать значение согласно формату
    /// </summary>
    public string FormatValue() => Value.ToString(Format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Сформировать строку для вывода в NC-файл
    /// </summary>
    public override string ToNCString()
    {
        if (!HasChanged && IsModal)
            return "";
        
        return $"{Address}{FormatValue()}";
    }

    public override string ToString() => $"{Name}={FormatValue()}";
}
