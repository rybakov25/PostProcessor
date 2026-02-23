using System.IO;
using PostProcessor.Core.Context;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for TextNCWord functionality
/// </summary>
public class TextNCWordTests
{
    private readonly StringWriter _stringWriter;
    private readonly BlockWriter _blockWriter;

    public TextNCWordTests()
    {
        _stringWriter = new StringWriter();
        _blockWriter = new BlockWriter(_stringWriter);
    }

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var word = new TextNCWord();

        // Assert
        Assert.Equal("(", word.Prefix);
        Assert.Equal(")", word.Suffix);
        Assert.False(word.Transliterate);
    }

    [Fact]
    public void Constructor_WithText()
    {
        // Act
        var word = new TextNCWord("Hello World");

        // Assert
        Assert.Equal("Hello World", word.Text);
    }

    [Fact]
    public void Constructor_CustomPrefixSuffix()
    {
        // Act
        var word = new TextNCWord("Test", prefix: ";", suffix: "");

        // Assert
        Assert.Equal(";", word.Prefix);
        Assert.Equal("", word.Suffix);
    }

    [Fact]
    public void ToNCString_WrapsInParentheses()
    {
        // Arrange
        var word = new TextNCWord("Comment");

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("(Comment)", result);
    }

    [Fact]
    public void ToNCString_CustomPrefixSuffix()
    {
        // Arrange
        var word = new TextNCWord("Comment", prefix: ";", suffix: "");

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal(";Comment", result);
    }

    [Fact]
    public void Text_Setter()
    {
        // Arrange
        var word = new TextNCWord();

        // Act
        word.Text = "New Text";

        // Assert
        Assert.Equal("New Text", word.Text);
        Assert.True(word.HasChanged);
    }

    [Fact]
    public void SetText_FluentInterface()
    {
        // Arrange
        var word = new TextNCWord();

        // Act
        var result = word.SetText("Fluent");

        // Assert
        Assert.Same(word, result);
        Assert.Equal("Fluent", word.Text);
    }

    [Fact]
    public void Transliterate_CyrillicToLatin()
    {
        // Arrange
        var word = new TextNCWord("Привет", transliterate: true);

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("(Privet)", result);
    }

    [Fact]
    public void Transliterate_MixedText()
    {
        // Arrange
        var word = new TextNCWord("Привет World", transliterate: true);

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("(Privet World)", result);
    }

    [Fact]
    public void Transliterate_FullAlphabet()
    {
        // Arrange
        var word = new TextNCWord("АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ", transliterate: true);

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("(ABVGDEYoZhZIYKLMNOPRSTUFKhTsChShSchYEYuYa)", result);
    }

    [Fact]
    public void Transliterate_Lowercase()
    {
        // Arrange
        var word = new TextNCWord("абвгдеёжзийклмнопрстуфхцчшщъыьэюя", transliterate: true);

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("(abvgdeyozhziyklmnoprstufkhtschshschyeyuya)", result);
    }

    [Fact]
    public void MaxLength_TruncatesText()
    {
        // Arrange
        var word = new TextNCWord("Long Comment");
        word.MaxLength = 4;

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("(Long)", result);
    }

    [Fact]
    public void MaxLength_NoTruncationIfShorter()
    {
        // Arrange
        var word = new TextNCWord("Short");
        word.MaxLength = 10;

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("(Short)", result);
    }

    [Fact]
    public void MaxLength_Null_NoLimit()
    {
        // Arrange
        var word = new TextNCWord("Very Long Comment");
        word.MaxLength = null;

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("(Very Long Comment)", result);
    }

    [Fact]
    public void ToString_ReturnsFormattedText()
    {
        // Arrange
        var word = new TextNCWord("Test");

        // Act
        var result = word.ToString();

        // Assert
        Assert.Equal("(Test)", result);
    }

    [Fact]
    public void Prefix_Property()
    {
        // Arrange
        var word = new TextNCWord();

        // Act
        word.Prefix = ";";

        // Assert
        Assert.Equal(";", word.Prefix);
    }

    [Fact]
    public void Suffix_Property()
    {
        // Arrange
        var word = new TextNCWord();

        // Act
        word.Suffix = " END";

        // Assert
        Assert.Equal(" END", word.Suffix);
    }

    [Fact]
    public void Transliterate_Property()
    {
        // Arrange
        var word = new TextNCWord("Тест");

        // Act
        word.Transliterate = true;
        var result = word.ToNCString();

        // Assert
        Assert.Equal("(Test)", result);
    }

    [Fact]
    public void BlockWriter_Integration()
    {
        // Arrange
        var comment = new TextNCWord("Test comment");
        _blockWriter.AddWord(comment);

        // Act
        _blockWriter.WriteBlock();

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("(Test comment)", output);
    }

    [Fact]
    public void EmptyText_EmptyOutput()
    {
        // Arrange
        var word = new TextNCWord("");

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("()", result);
    }

    [Fact]
    public void NullPrefixSuffix_NoCrash()
    {
        // Act
        var word = new TextNCWord("Test", prefix: null, suffix: null);

        // Assert
        Assert.Equal("", word.Prefix);
        Assert.Equal("", word.Suffix);
    }

    [Fact]
    public void SpecialCharacters_Preserved()
    {
        // Arrange
        var word = new TextNCWord("Test: 100% [OK]");

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("(Test: 100% [OK])", result);
    }

    [Fact]
    public void UnicodeCharacters_Preserved()
    {
        // Arrange
        var word = new TextNCWord("Test 日本語");

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("(Test 日本語)", result);
    }

    [Fact]
    public void Newlines_Preserved()
    {
        // Arrange
        var word = new TextNCWord("Line1\nLine2");

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Contains("\n", result);
    }

    [Fact]
    public void HasChanged_TrueAfterTextChange()
    {
        // Arrange
        var word = new TextNCWord("Original");
        word.ToNCString(); // First output

        // Act
        word.Text = "Changed";

        // Assert
        Assert.True(word.HasChanged);
    }

    [Fact]
    public void HasChanged_FalseAfterSameText()
    {
        // Arrange
        var word = new TextNCWord("Same");
        word.SetText("Same");

        // Assert
        Assert.False(word.HasChanged);
    }
}
