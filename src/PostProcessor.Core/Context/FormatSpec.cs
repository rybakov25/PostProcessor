using System.Globalization;
using System.Text.RegularExpressions;

namespace PostProcessor.Core.Context;

/// <summary>
/// Спецификация формата для NC-слов (вдохновлено СПРУТ SDK)
/// Поддерживает форматы наподобие "X{-####!0##}"
/// </summary>
public class FormatSpec
{
    /// <summary>
    /// Адрес (X, Y, Z, F, S...)
    /// </summary>
    public string Address { get; set; } = "";
    
    /// <summary>
    /// Знак: No, MinusOnly, PlusAndMinus
    /// </summary>
    public NCWordSign SignMode { get; set; } = NCWordSign.MinusOnly;
    
    /// <summary>
    /// Десятичная точка: Never, Optional, Always
    /// </summary>
    public NCWordDecPoint PointMode { get; set; } = NCWordDecPoint.Always;
    
    /// <summary>
    /// Количество цифр перед точкой
    /// </summary>
    public int DigitsBefore { get; set; } = 4;
    
    /// <summary>
    /// Количество цифр после точки
    /// </summary>
    public int DigitsAfter { get; set; } = 3;
    
    /// <summary>
    /// Выводить ли ведущие нули
    /// </summary>
    public bool LeadingZeroes { get; set; } = true;
    
    /// <summary>
    /// Режим хвостовых нулей
    /// </summary>
    public TrailingZeroesMode TrailingZeroes { get; set; } = TrailingZeroesMode.OneOnly;
    
    /// <summary>
    /// Разделитель десятичной дроби
    /// </summary>
    public static string DecimalSeparator => ".";
    
    /// <summary>
    /// Сформировать строку формата из спецификации
    /// </summary>
    public string ToFormatString()
    {
        var result = Address;
        
        // Знак
        if (SignMode == NCWordSign.PlusAndMinus)
            result += "+";
        else if (SignMode == NCWordSign.MinusOnly)
            result += "-";
        
        // Цифры
        result += "{";
        
        // Ведущие нули
        if (LeadingZeroes)
        {
            for (int i = 0; i < DigitsBefore; i++)
                result += "0";
        }
        else
        {
            for (int i = 0; i < DigitsBefore; i++)
                result += "#";
        }
        
        // Точка
        if (PointMode == NCWordDecPoint.Always)
            result += "!";
        else if (PointMode == NCWordDecPoint.Optional)
            result += ".";
        // Never - ничего не добавляем
        
        // Цифры после точки
        for (int i = 0; i < DigitsAfter; i++)
            result += "#";
        
        result += "}";
        
        return result;
    }
    
    /// <summary>
    /// Форматировать значение согласно спецификации
    /// </summary>
    public string FormatValue(double value)
    {
        // Округление
        var rounded = Math.Round(value, DigitsAfter);
        
        // Форматирование
        var format = new string('0', DigitsBefore) + "." + new string('0', DigitsAfter);
        var formatted = rounded.ToString(format, CultureInfo.InvariantCulture);
        
        // Обработка знака
        if (SignMode == NCWordSign.No && formatted.StartsWith("-"))
        {
            formatted = formatted.Substring(1);
        }
        else if (SignMode == NCWordSign.PlusAndMinus && !formatted.StartsWith("-"))
        {
            formatted = "+" + formatted;
        }
        
        // Обработка хвостовых нулей
        if (TrailingZeroes == TrailingZeroesMode.No)
        {
            formatted = formatted.TrimEnd('0').TrimEnd('.');
        }
        else if (TrailingZeroes == TrailingZeroesMode.OneOnly)
        {
            if (formatted.Contains("."))
            {
                formatted = formatted.TrimEnd('0');
                if (formatted.EndsWith("."))
                    formatted += "0";
            }
        }
        
        return Address + formatted;
    }
    
    /// <summary>
    /// Парсить формат-строку наподобие "X{-####!0##}"
    /// </summary>
    public static FormatSpec Parse(string formatString)
    {
        var spec = new FormatSpec();

        if (string.IsNullOrEmpty(formatString))
            return spec;

        // Извлечение адреса (первый символ или символы до {)
        var braceIndex = formatString.IndexOf('{');
        if (braceIndex > 0)
        {
            spec.Address = formatString.Substring(0, braceIndex);
            formatString = formatString.Substring(braceIndex);
        }
        else if (formatString.Length > 0 && char.IsLetter(formatString[0]))
        {
            spec.Address = formatString[0].ToString();
            formatString = formatString.Substring(1);
        }

        // Парсинг содержимого {}
        if (formatString.StartsWith("{") && formatString.EndsWith("}"))
        {
            var content = formatString.Substring(1, formatString.Length - 2);
            ParseContent(spec, content);
        }

        return spec;
    }

    /// <summary>
    /// Попытаться распарсить формат-строку
    /// </summary>
    /// <param name="formatString">Формат-строка</param>
    /// <returns>FormatSpec или null если не удалось</returns>
    public static FormatSpec? TryParse(string formatString)
    {
        try
        {
            return Parse(formatString);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Форматировать значение (алиас для FormatValue)
    /// </summary>
    public string Format(double value)
    {
        return FormatValue(value);
    }
    
    private static void ParseContent(FormatSpec spec, string content)
    {
        int i = 0;
        
        // Знак
        if (i < content.Length && (content[i] == '-' || content[i] == '+'))
        {
            spec.SignMode = content[i] == '-' ? NCWordSign.MinusOnly : NCWordSign.PlusAndMinus;
            i++;
        }
        
        // Подсчёт цифр до точки
        int digitsBefore = 0;
        while (i < content.Length && (content[i] == '0' || content[i] == '#'))
        {
            if (content[i] == '0')
                spec.LeadingZeroes = true;
            digitsBefore++;
            i++;
        }
        spec.DigitsBefore = digitsBefore;
        
        // Точка
        if (i < content.Length)
        {
            if (content[i] == '!')
                spec.PointMode = NCWordDecPoint.Always;
            else if (content[i] == '.')
                spec.PointMode = NCWordDecPoint.Optional;
            // Иначе - Never
            
            if (spec.PointMode != NCWordDecPoint.Never)
                i++;
        }
        
        // Подсчёт цифр после точки
        int digitsAfter = 0;
        while (i < content.Length && (content[i] == '0' || content[i] == '#'))
        {
            digitsAfter++;
            i++;
        }
        spec.DigitsAfter = digitsAfter > 0 ? digitsAfter : 0;
    }
}

/// <summary>
/// Режим вывода знака числа
/// </summary>
public enum NCWordSign
{
    No,          // Не выводить знак
    MinusOnly,   // Только минус
    PlusAndMinus // Всегда +/-
}

/// <summary>
/// Режим вывода десятичной точки
/// </summary>
public enum NCWordDecPoint
{
    Never,    // Никогда
    Optional, // Только для дробных
    Always    // Всегда
}

/// <summary>
/// Режим вывода хвостовых нулей
/// </summary>
public enum TrailingZeroesMode
{
    No,       // Не выводить
    OneOnly,  // Только один ноль
    Yes       // Всегда выводить все
}
