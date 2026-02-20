namespace PostProcessor.Macros.Attributes;

/// <summary>
/// указывает имя APT-команды, с которой связан макрос.
/// ѕоддерживает шаблоны:
///   "goto"      Ч точное совпадение
///   "goto/*"    Ч все варианты команды goto
///   "*"         Ч универсальный макрос для всех команд
/// соответствует механизму именования макросов в IMSpost
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class MacroNameAttribute : Attribute
{
    public string Name { get; }

    public MacroNameAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Macro name cannot be null or empty", nameof(name));

        Name = name.ToLowerInvariant();
    }
}
