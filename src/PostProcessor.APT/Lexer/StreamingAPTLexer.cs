using PostProcessor.APT.Encodings;
using PostProcessor.Core.Models;
using System.Globalization;
using System.Text;

namespace PostProcessor.APT.Lexer;

public class StreamingAPTLexer : IAsyncDisposable
{
    private readonly StreamReader _reader;
    private readonly int _lineNumberStart;
    private int _currentLine;
    private bool _disposed = false;
    private string _continuationBuffer = string.Empty;

    public StreamingAPTLexer(string filePath, IEncodingDetector? detector = null, int lineNumberStart = 1)
    {
        detector ??= new EncodingDetector();
        var encoding = detector.Detect(filePath);

        _reader = new StreamReader(
            filePath,
            encoding,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 8192
        );
        _lineNumberStart = lineNumberStart;
        _currentLine = lineNumberStart;
    }

    public async IAsyncEnumerable<APTCommand> ParseStreamAsync()
    {
        string? line;
        while ((line = await _reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            _currentLine++;

            // Пропуск пустых строк
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Удаление комментариев '$$' до конца строки
            var commentIndex = line.IndexOf("$$", StringComparison.Ordinal);
            if (commentIndex >= 0)
                line = line[..commentIndex].TrimEnd();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Обработка продолжения строки: символ '$' в конце (не внутри кавычек)
            if (line.TrimEnd().EndsWith('$') && !IsInsideQuotes(line.TrimEnd(), line.TrimEnd().LastIndexOf('$')))
            {
                _continuationBuffer += line.TrimEnd().TrimEnd('$').TrimEnd() + " ";
                continue;
            }

            // Объединение с буфером продолжения
            if (!string.IsNullOrEmpty(_continuationBuffer))
            {
                line = _continuationBuffer + line.TrimStart();
                _continuationBuffer = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Парсинг объединённой команды
            var command = ParseLine(line, _currentLine);
            if (command != null)
                yield return command;
        }

        // Обработка оставшегося буфера
        if (!string.IsNullOrEmpty(_continuationBuffer))
        {
            var command = ParseLine(_continuationBuffer.Trim(), _currentLine);
            if (command != null)
                yield return command;
            _continuationBuffer = string.Empty;
        }
    }

    private bool IsInsideQuotes(string line, int position)
    {
        bool inSingleQuote = false;
        bool inDoubleQuote = false;

        for (int i = 0; i < position; i++)
        {
            char c = line[i];
            if (c == '\'' && !inDoubleQuote) inSingleQuote = !inSingleQuote;
            if (c == '"' && !inSingleQuote) inDoubleQuote = !inDoubleQuote;
        }
        return inSingleQuote || inDoubleQuote;
    }

    private APTCommand ParseLine(string line, int lineNumber)
    {
        // CATIA-специфика: команды могут начинаться с точки или числа (например, ".000000,")
        // Обработка: если строка начинается не с буквы — это продолжение предыдущей команды
        if (!string.IsNullOrEmpty(_continuationBuffer) ||
            (!char.IsLetter(line[0]) && !line.StartsWith("$$")))
        {
            // Это продолжение предыдущей команды — обрабатываем как параметры
            var (MinorWords, NumericValues, StringValues) = ParseParameters(line.Trim(), lineNumber);
            return new APTCommand(
                MajorWord: "continuation", // флаг для контекста
                MinorWords: MinorWords,
                NumericValues: NumericValues,
                StringValues: StringValues,
                LineNumber: lineNumber
            );
        }

        // Поиск разделителя '/' с учётом вложенности скобок
        int delimiterIndex = -1;
        bool inQuotes = false;
        char quoteChar = '\0';
        int bracketDepth = 0;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"' || c == '\'')
            {
                if (!inQuotes) { inQuotes = true; quoteChar = c; }
                else if (c == quoteChar) inQuotes = false;
                continue;
            }

            if (!inQuotes)
            {
                if (c == '(') bracketDepth++;
                if (c == ')') bracketDepth--;
                if (c == '/' && bracketDepth == 0)
                {
                    delimiterIndex = i;
                    break;
                }
            }
        }

        string majorWord;
        string paramsPart;

        if (delimiterIndex > 0)
        {
            majorWord = line.Substring(0, delimiterIndex).Trim();
            paramsPart = (delimiterIndex + 1 < line.Length)
                ? line.Substring(delimiterIndex + 1).Trim()
                : string.Empty;
        }
        else
        {
            // Альтернативный синтаксис без '/'
            majorWord = line.Trim();
            paramsPart = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(majorWord))
            throw new FormatException($"Empty major word at line {lineNumber}");

        // CATIA-специфика: удаление завершающей запятой из имени команды
        if (majorWord.EndsWith(','))
            majorWord = majorWord.TrimEnd(',');

        // Унификация регистра
        majorWord = majorWord.ToLowerInvariant();

        var parameters = ParseParameters(paramsPart, lineNumber);

        return new APTCommand(
            MajorWord: majorWord,
            MinorWords: parameters.MinorWords,
            NumericValues: parameters.NumericValues,
            StringValues: parameters.StringValues,
            LineNumber: lineNumber
        );
    }

    private (List<string> MinorWords, List<double> NumericValues, List<string> StringValues) ParseParameters(
        string paramPart,
        int lineNumber)
    {
        var minors = new List<string>();
        var numerics = new List<double>();
        var strings = new List<string>();

        if (string.IsNullOrWhiteSpace(paramPart))
            return (minors, numerics, strings);

        var tokens = SplitParameters(paramPart);

        foreach (var token in tokens)
        {
            var trimmed = token.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Строковое значение в кавычках
            if ((trimmed.StartsWith("'") && trimmed.EndsWith("'")) ||
                (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")))
            {
                strings.Add(trimmed.Trim('\'', '"'));
                continue;
            }

            // Геометрический примитив в скобках
            if (trimmed.StartsWith("(") && trimmed.EndsWith(")") && trimmed.Length > 2)
            {
                var geometry = trimmed[1..^1].Trim();
                strings.Add(geometry);
                continue;
            }

            // Числовое значение (с поддержкой запятой как десятичного разделителя для локалей)
            var normalized = trimmed.Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
            {
                numerics.Add(num);
                continue;
            }

            // Minor word → lowercase
            minors.Add(trimmed.ToLowerInvariant());
        }

        return (minors, numerics, strings);
    }

    private List<string> SplitParameters(string input)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        char quoteChar = '\0';
        int bracketDepth = 0;

        for (int i = 0; i < input.Length; i++)
        {
            var ch = input[i];

            if (ch == '"' || ch == '\'')
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    quoteChar = ch;
                }
                else if (ch == quoteChar)
                {
                    inQuotes = false;
                }
                current.Append(ch);
                continue;
            }

            if (!inQuotes)
            {
                if (ch == '(') { bracketDepth++; current.Append(ch); continue; }
                if (ch == ')') { bracketDepth--; current.Append(ch); continue; }
            }

            if (ch == ',' && !inQuotes && bracketDepth == 0)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0)
            result.Add(current.ToString());

        return result;
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _reader.Dispose();
            GC.SuppressFinalize(this);
            _disposed = true;
        }
        return ValueTask.CompletedTask;
    }
}
