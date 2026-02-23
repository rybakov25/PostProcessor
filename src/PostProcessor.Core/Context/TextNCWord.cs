namespace PostProcessor.Core.Context;

/// <summary>
/// Текстовое NC-слово для комментариев и строковых значений
/// Аналог TextNCWord из SPRUT SDK
/// </summary>
public class TextNCWord : NCWord
{
    private string _text = "";
    private string _prefix = "(";
    private string _suffix = ")";
    private bool _transliterate = false;
    private int? _maxLength = null;

    /// <summary>
    /// Создать текстовое NC-слово
    /// </summary>
    /// <param name="text">Текст</param>
    /// <param name="prefix">Префикс (по умолчанию "(")</param>
    /// <param name="suffix">Суффикс (по умолчанию ")")</param>
    /// <param name="transliterate">Транслитерировать кириллицу</param>
    public TextNCWord(string text = "", string? prefix = "(", string? suffix = ")", bool transliterate = false)
    {
        _text = text;
        _prefix = prefix ?? "";
        _suffix = suffix ?? "";
        _transliterate = transliterate;
        IsModal = false; // Текст всегда выводится
        _hasChanged = true;
    }

    /// <summary>
    /// Создать текстовое NC-слово из настроек конфига
    /// </summary>
    /// <param name="config">Конфигурация контроллера</param>
    /// <param name="text">Текст комментария</param>
    public TextNCWord(Config.Models.ControllerConfig config, string text)
    {
        _text = text;
        var commentStyle = config.Formatting.Comments;
        
        // Установка стиля в зависимости от типа
        switch (commentStyle.Type.ToLowerInvariant())
        {
            case "semicolon":
                _prefix = commentStyle.SemicolonPrefix;
                _suffix = "";
                break;
            case "both":
                // Для типа "both" используем parentheses + semicolon
                _prefix = commentStyle.Prefix;
                _suffix = commentStyle.Suffix + " " + commentStyle.SemicolonPrefix + " ";
                break;
            default: // parentheses
                _prefix = commentStyle.Prefix;
                _suffix = commentStyle.Suffix;
                break;
        }
        
        _transliterate = commentStyle.Transliterate;
        _maxLength = commentStyle.MaxLength > 0 ? commentStyle.MaxLength : null;
        IsModal = false;
        _hasChanged = true;
    }

    /// <summary>
    /// Текст комментария
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            _hasChanged = value != _text;
            _text = value;
        }
    }

    /// <summary>
    /// Префикс (например, "(" для комментариев)
    /// </summary>
    public string Prefix
    {
        get => _prefix;
        set => _prefix = value ?? "";
    }

    /// <summary>
    /// Суффикс (например, ")" для комментариев)
    /// </summary>
    public string Suffix
    {
        get => _suffix;
        set => _suffix = value ?? "";
    }

    /// <summary>
    /// Транслитерировать кириллицу в латиницу
    /// </summary>
    public bool Transliterate
    {
        get => _transliterate;
        set => _transliterate = value;
    }

    /// <summary>
    /// Максимальная длина текста (null = без ограничений)
    /// </summary>
    public int? MaxLength
    {
        get => _maxLength;
        set => _maxLength = value;
    }

    /// <summary>
    /// Установить текст и вернуть это же слово (для fluent interface)
    /// </summary>
    /// <param name="text">Текст</param>
    /// <returns>Это же слово</returns>
    public TextNCWord SetText(string text)
    {
        Text = text;
        return this;
    }

    /// <summary>
    /// Сформировать строку для вывода в NC-файл
    /// </summary>
    public override string ToNCString()
    {
        if (!HasChanged && IsModal)
            return "";

        var text = _transliterate ? TransliterateText(_text) : _text;

        // Ограничение длины
        if (_maxLength.HasValue && text.Length > _maxLength.Value)
            text = text.Substring(0, _maxLength.Value);

        return $"{_prefix}{text}{_suffix}";
    }

    /// <summary>
    /// Переопределение для совместимости
    /// </summary>
    public override string ToString()
    {
        return ToNCString();
    }

    /// <summary>
    /// Простая транслитерация кириллицы в латиницу
    /// </summary>
    /// <param name="text">Текст на русском</param>
    /// <returns>Транслитерированный текст</returns>
    private static string TransliterateText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var translit = text
            .Replace("А", "A").Replace("а", "a")
            .Replace("Б", "B").Replace("б", "b")
            .Replace("В", "V").Replace("в", "v")
            .Replace("Г", "G").Replace("г", "g")
            .Replace("Д", "D").Replace("д", "d")
            .Replace("Е", "E").Replace("е", "e")
            .Replace("Ё", "Yo").Replace("ё", "yo")
            .Replace("Ж", "Zh").Replace("ж", "zh")
            .Replace("З", "Z").Replace("з", "z")
            .Replace("И", "I").Replace("и", "i")
            .Replace("Й", "Y").Replace("й", "y")
            .Replace("К", "K").Replace("к", "k")
            .Replace("Л", "L").Replace("л", "l")
            .Replace("М", "M").Replace("м", "m")
            .Replace("Н", "N").Replace("н", "n")
            .Replace("О", "O").Replace("о", "o")
            .Replace("П", "P").Replace("п", "p")
            .Replace("Р", "R").Replace("р", "r")
            .Replace("С", "S").Replace("с", "s")
            .Replace("Т", "T").Replace("т", "t")
            .Replace("У", "U").Replace("у", "u")
            .Replace("Ф", "F").Replace("ф", "f")
            .Replace("Х", "Kh").Replace("х", "kh")
            .Replace("Ц", "Ts").Replace("ц", "ts")
            .Replace("Ч", "Ch").Replace("ч", "ch")
            .Replace("Ш", "Sh").Replace("ш", "sh")
            .Replace("Щ", "Sch").Replace("щ", "sch")
            .Replace("Ъ", "").Replace("ъ", "")
            .Replace("Ы", "Y").Replace("ы", "y")
            .Replace("Ь", "").Replace("ь", "")
            .Replace("Э", "E").Replace("э", "e")
            .Replace("Ю", "Yu").Replace("ю", "yu")
            .Replace("Я", "Ya").Replace("я", "ya");

        return translit;
    }
}
