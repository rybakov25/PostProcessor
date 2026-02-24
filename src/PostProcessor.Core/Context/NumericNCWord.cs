using System.Globalization;
using PostProcessor.Core.Config.Models;

namespace PostProcessor.Core.Context;

/// <summary>
/// Числовое NC-слово с форматированием по паттерну из конфига
/// Аналог NumericNCWord из SPRUT SDK
/// </summary>
public class NumericNCWord : NCWord
{
    private double _value;
    private readonly double _defaultValue;
    private readonly string _formatPattern;
    private readonly FormatSpec? _formatSpec;
    private readonly int _decimals;
    private readonly bool _leadingZeros;
    private readonly bool _decimalPoint;
    private readonly bool _trailingZeros;

    /// <summary>
    /// Создать числовое NC-слово с форматированием
    /// </summary>
    /// <param name="address">Адрес (X, Y, Z, F, S...)</param>
    /// <param name="defaultValue">Значение по умолчанию</param>
    /// <param name="formatPattern">Паттерн формата (например, "X{-#####!###}")</param>
    /// <param name="isModal">Режим модальности</param>
    public NumericNCWord(string address, double defaultValue = 0.0, string? formatPattern = null, bool isModal = true)
    {
        Address = address;
        _defaultValue = defaultValue;
        _value = defaultValue;
        _formatPattern = formatPattern ?? "";
        _formatSpec = FormatSpec.TryParse(formatPattern ?? "");
        IsModal = isModal;
        _hasChanged = true;
    }

    /// <summary>
    /// Создать числовое NC-слово из настроек конфига
    /// </summary>
    /// <param name="config">Конфигурация контроллера</param>
    /// <param name="address">Адрес регистра (X, Y, Z, F, S...)</param>
    /// <param name="defaultValue">Значение по умолчанию</param>
    /// <param name="isModal">Режим модальности</param>
    public NumericNCWord(ControllerConfig config, string address, double defaultValue = 0.0, bool isModal = true)
    {
        Address = address;
        _defaultValue = defaultValue;
        _value = defaultValue;

        // Получаем настройки форматирования для данного адреса
        var coordFormatting = config.Formatting.Coordinates;
        
        // Для F и S используем специальные настройки
        if (address == "F")
        {
            _decimals = config.Formatting.Feedrate.Decimals;
        }
        else if (address == "S")
        {
            _decimals = config.Formatting.SpindleSpeed.Decimals;
        }
        else
        {
            _decimals = coordFormatting.Decimals;
        }

        _leadingZeros = coordFormatting.LeadingZeros;
        _decimalPoint = coordFormatting.DecimalPoint;
        _trailingZeros = coordFormatting.TrailingZeros;
        
        IsModal = isModal;
        _hasChanged = true;
    }

    /// <summary>
    /// Текущее значение
    /// </summary>
    public double v
    {
        get => _value;
        set
        {
            v0 = _value;
            _value = value;
            _hasChanged = Math.Abs(value - v0) > 1e-6;
        }
    }

    /// <summary>
    /// Предыдущее значение (для сравнения модальности)
    /// </summary>
    public double v0 { get; private set; }

    /// <summary>
    /// Установить новое значение
    /// </summary>
    /// <param name="value">Новое значение</param>
    public void Set(double value)
    {
        v0 = _value;
        v = value;
    }

    /// <summary>
    /// Установить значение без отметки об изменении (для инициализации)
    /// </summary>
    /// <param name="value">Значение</param>
    public void SetInitial(double value)
    {
        _value = value;
        v0 = value;
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
    /// <param name="value">Значение для проверки</param>
    public void Show(double value)
    {
        if (Math.Abs(value - _value) > 1e-6)
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
    /// <param name="value">Значение для проверки</param>
    public void Hide(double value)
    {
        if (Math.Abs(value - _value) < 1e-6)
        {
            _hasChanged = false;
        }
    }

    /// <summary>
    /// Сбросить к значению по умолчанию
    /// </summary>
    /// <param name="markChanged">Отметить как изменённое</param>
    public void Reset(bool markChanged = true)
    {
        v0 = _value;
        _value = _defaultValue;
        _hasChanged = markChanged;
    }

    /// <summary>
    /// Сбросить к указанному значению
    /// </summary>
    /// <param name="value">Новое значение</param>
    /// <param name="markChanged">Отметить как изменённое</param>
    public void Reset(double value, bool markChanged = true)
    {
        v0 = _value;
        _value = value;
        _hasChanged = markChanged;
    }

    /// <summary>
    /// Проверить, отличаются ли значения
    /// </summary>
    public bool ValuesDiffer => Math.Abs(_value - v0) > 1e-6;

    /// <summary>
    /// Проверить, равно ли значение указанному
    /// </summary>
    public bool ValuesSame => Math.Abs(_value - v0) < 1e-6;

    /// <summary>
    /// Сформировать строку для вывода в NC-файл
    /// </summary>
    public override string ToNCString()
    {
        if (!HasChanged && IsModal)
            return "";

        var formatted = FormatValue(_value);
        var result = $"{Address}{formatted}";
        
        // Сброс флага после вывода (для модальных слов)
        if (IsModal)
            _hasChanged = false;
        
        return result;
    }

    /// <summary>
    /// Форматировать значение
    /// </summary>
    /// <param name="value">Значение</param>
    /// <returns>Отформатированная строка</returns>
    private string FormatValue(double value)
    {
        // Если есть паттерн формата (SPRUT-style), используем его
        if (_formatSpec != null)
        {
            return _formatSpec.Format(value);
        }

        // Иначе используем стандартное форматирование с нужным количеством знаков
        var formatted = value.ToString($"F{_decimals}", CultureInfo.InvariantCulture);

        // Обработка ведущих нулей
        if (!_leadingZeros && value >= 0)
        {
            var parts = formatted.Split('.');
            if (parts.Length > 0)
            {
                parts[0] = parts[0].TrimStart('0');
                if (string.IsNullOrEmpty(parts[0]))
                    parts[0] = "0";
                formatted = string.Join(".", parts);
            }
        }
        else if (!_leadingZeros && value < 0)
        {
            var parts = formatted.Split('.');
            if (parts.Length > 0)
            {
                parts[0] = "-" + parts[0].TrimStart('-').TrimStart('0');
                if (parts[0] == "-")
                    parts[0] = "-0";
                formatted = string.Join(".", parts);
            }
        }

        // Обработка десятичной точки
        if (!_decimalPoint && formatted.Contains("."))
        {
            var parts = formatted.Split('.');
            if (parts.Length == 2 && parts[1] == "0")
                formatted = parts[0];
        }

        // Обработка хвостовых нулей
        if (!_trailingZeros && formatted.Contains("."))
        {
            formatted = formatted.TrimEnd('0').TrimEnd('.');
        }

        return formatted;
    }

    /// <summary>
    /// Переопределение для совместимости
    /// </summary>
    public override string ToString()
    {
        return $"{Address}{FormatValue(_value)}";
    }
}
