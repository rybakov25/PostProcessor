using System.Text;
using System.Text.RegularExpressions;

namespace PostProcessor.APT.Encodings;

public class EncodingDetector : IEncodingDetector
{
    public Encoding Detect(string filePath)
    {
        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.SequentialScan
        );
        return DetectFromStream(stream);
    }

    public Encoding DetectFromStream(Stream stream)
    {
        var bom = new byte[4];
        var read = stream.Read(bom, 0, 4);
        stream.Seek(0, SeekOrigin.Begin);

        // Проверка BOM
        if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            return Encoding.UTF8;
        if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
            return Encoding.BigEndianUnicode;
        if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
            return Encoding.Unicode;
        if (read >= 4 && bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF)
            return Encoding.UTF32;

        // Эвристика для первых 4 КБ
        var buffer = new byte[Math.Min(4096, (int)stream.Length)];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        stream.Seek(0, SeekOrigin.Begin);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Проверка на кириллицу в UTF-8
        try
        {
            var utf8 = Encoding.UTF8.GetString(buffer.AsSpan(0, bytesRead));
            if (Regex.IsMatch(utf8, @"[аА-яЯёЁ]"))
                return Encoding.UTF8;
        }
        catch
        {
            // Игнорируем ошибки декодирования
        }

        // Проверка на CP1251
        try
        {
            var cp1251 = Encoding.GetEncoding(1251);
            var decoded = cp1251.GetString(buffer.AsSpan(0, bytesRead));
            if (Regex.IsMatch(decoded, @"[аА-яЯёЁ]"))
                return cp1251;
        }
        catch
        {
            // Игнорируем ошибки декодирования
        }

        // По умолчанию — UTF-8
        return Encoding.UTF8;
    }
}
