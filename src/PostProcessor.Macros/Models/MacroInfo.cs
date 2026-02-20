namespace PostProcessor.Macros.Models;

public record MacroInfo(
    string Name,
    string FilePath,
    DateTime LastModified,
    int Priority,
    bool IsCompiled
);
