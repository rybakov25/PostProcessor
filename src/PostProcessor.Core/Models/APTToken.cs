namespace PostProcessor.Core.Models;

public enum APTTokenType
{
    MajorWord,
    MinorWord,
    NumericValue,
    StringValue,
    Delimiter,
    Comma,
    Comment,
    EndOfLine
}

public record APTToken(
    APTTokenType Type,
    string Value,
    int LineNumber,
    int Column
);
