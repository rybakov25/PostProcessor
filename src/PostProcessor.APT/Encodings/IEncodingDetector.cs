using System.Text;

namespace PostProcessor.APT.Encodings;

public interface IEncodingDetector
{
    Encoding Detect(string filePath);
    Encoding DetectFromStream(Stream stream);
}
