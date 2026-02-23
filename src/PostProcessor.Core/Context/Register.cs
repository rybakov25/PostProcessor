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
    /// Установить значение без отметки об изменении (для инициализации)
    /// </summary>
    public void SetInitial(double value)
    {
        Value = value;
        _previousValue = value;
        _hasChanged = false;
    }

    /// <summary>
    /// Показать значение (принудительно отметить как изменённое)
    /// </summary>
    public void Show()
    {
        _hasChanged = true;
    }

    /// <summary>
    /// Показать значение если оно отличается
    /// </summary>
    public void Show(double value)
    {
        if (Math.Abs(value - Value) > 1e-6)
        {
            _hasChanged = true;
        }
    }

    /// <summary>
    /// Скрыть значение (отметить как неизменённое)
    /// </summary>
    public void Hide()
    {
        _hasChanged = false;
    }

    /// <summary>
    /// Скрыть значение если оно равно указанному
    /// </summary>
    public void Hide(double value)
    {
        if (Math.Abs(value - Value) < 1e-6)
        {
            _hasChanged = false;
        }
    }

    /// <summary>
    /// Сбросить к значению по умолчанию
    /// </summary>
    public void Reset(bool markChanged = true)
    {
        _previousValue = Value;
        Value = 0.0;
        _hasChanged = markChanged;
    }

    /// <summary>
    /// Сбросить к указанному значению
    /// </summary>
    public void Reset(double value, bool markChanged = true)
    {
        _previousValue = Value;
        Value = value;
        _hasChanged = markChanged;
    }

    /// <summary>
    /// Проверить, отличаются ли значения
    /// </summary>
    public bool ValuesDiffer => Math.Abs(Value - _previousValue) > 1e-6;

    /// <summary>
    /// Проверить, равно ли значение указанному
    /// </summary>
    public bool ValuesSame => Math.Abs(Value - _previousValue) < 1e-6;

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
