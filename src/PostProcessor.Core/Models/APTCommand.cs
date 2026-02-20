namespace PostProcessor.Core.Models;

public record APTCommand(
    string MajorWord,
    List<string> MinorWords,
    List<double> NumericValues,
    List<string> StringValues,
    int LineNumber
);
