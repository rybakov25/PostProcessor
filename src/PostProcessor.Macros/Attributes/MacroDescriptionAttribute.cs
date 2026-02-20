namespace PostProcessor.Macros.Attributes;

/// <summary>
/// Предоставляет человекочитаемое описание макроса.
/// Используется для генерации документации и отладки.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class MacroDescriptionAttribute : Attribute
{
    public string Description { get; }

    public MacroDescriptionAttribute(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}
