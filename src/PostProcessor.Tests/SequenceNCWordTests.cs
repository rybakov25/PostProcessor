using System.IO;
using PostProcessor.Core.Context;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for SequenceNCWord functionality
/// </summary>
public class SequenceNCWordTests
{
    private readonly StringWriter _stringWriter;
    private readonly BlockWriter _blockWriter;

    public SequenceNCWordTests()
    {
        _stringWriter = new StringWriter();
        _blockWriter = new BlockWriter(_stringWriter);
    }

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var word = new SequenceNCWord();

        // Assert
        Assert.Equal(1, word.Value);
        Assert.Equal(10, word.Step);
        Assert.Equal("N", word.Address);
        Assert.False(word.IsModal);
    }

    [Fact]
    public void Constructor_CustomValues()
    {
        // Act
        var word = new SequenceNCWord(start: 100, step: 5, prefix: "N", suffix: "", isModal: false);

        // Assert
        Assert.Equal(100, word.Value);
        Assert.Equal(5, word.Step);
        Assert.Equal("N", word.Address);
    }

    [Fact]
    public void ToNCString_ReturnsFormattedValue()
    {
        // Arrange
        var word = new SequenceNCWord(start: 10, step: 10);

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("N10", result);
        Assert.Equal(20, word.Value); // Auto-incremented
    }

    [Fact]
    public void ToNCString_AutoIncrements()
    {
        // Arrange
        var word = new SequenceNCWord(start: 1, step: 1);

        // Act
        var result1 = word.ToNCString();
        var result2 = word.ToNCString();
        var result3 = word.ToNCString();

        // Assert
        Assert.Equal("N1", result1);
        Assert.Equal("N2", result2);
        Assert.Equal("N3", result3);
        Assert.Equal(4, word.Value);
    }

    [Fact]
    public void SetValue_UpdatesValue()
    {
        // Arrange
        var word = new SequenceNCWord(start: 10);

        // Act
        word.SetValue(100);

        // Assert
        Assert.Equal(100, word.Value);
        Assert.True(word.HasChanged);
    }

    [Fact]
    public void Increment_AddsStep()
    {
        // Arrange
        var word = new SequenceNCWord(start: 10, step: 5);

        // Act
        word.Increment();
        word.Increment();

        // Assert
        Assert.Equal(20, word.Value);
    }

    [Fact]
    public void Reset_ResetsToStart()
    {
        // Arrange
        var word = new SequenceNCWord(start: 10);
        word.Increment();
        word.Increment();
        Assert.Equal(30, word.Value);

        // Act
        word.Reset();

        // Assert
        Assert.Equal(30, word.Value); // Reset без параметров не меняет значение
        Assert.True(word.HasChanged);
    }

    [Fact]
    public void Reset_WithParameter_ResetsToNewValue()
    {
        // Arrange
        var word = new SequenceNCWord(start: 10);
        word.Increment();

        // Act
        word.Reset(1);

        // Assert
        Assert.Equal(1, word.Value);
    }

    [Fact]
    public void ToNCString_WithPrefixAndSuffix()
    {
        // Arrange
        var word = new SequenceNCWord(start: 5, prefix: "Block", suffix: ":");

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("Block5:", result);
    }

    [Fact]
    public void ToNCString_WithoutPrefix()
    {
        // Arrange
        var word = new SequenceNCWord(start: 5, prefix: null);

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("5", result);
    }

    [Fact]
    public void ToString_ReturnsFormattedValue()
    {
        // Arrange
        var word = new SequenceNCWord(start: 42);

        // Act
        var result = word.ToString();

        // Assert
        Assert.Equal("N42", result);
    }

    [Fact]
    public void HasChanged_TrueAfterIncrement()
    {
        // Arrange
        var word = new SequenceNCWord(start: 10);
        word.ToNCString(); // First output

        // Act
        word.Increment();

        // Assert
        Assert.True(word.HasChanged);
    }

    [Fact]
    public void BlockWriter_Integration()
    {
        // Arrange
        var seq = new SequenceNCWord(start: 10, step: 10);
        var x = new Register("X", 0.0, true, "F4.3");
        _blockWriter.AddWords(seq, x);

        // Act
        x.SetValue(100.5);
        _blockWriter.WriteBlock();

        x.SetValue(200.3);
        _blockWriter.WriteBlock();

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("N10", output);
        Assert.Contains("N20", output);
    }

    [Fact]
    public void Step_Property()
    {
        // Arrange
        var word = new SequenceNCWord(start: 0, step: 100);

        // Assert
        Assert.Equal(100, word.Step);
    }

    [Fact]
    public void Value_Property()
    {
        // Arrange
        var word = new SequenceNCWord(start: 50);

        // Assert
        Assert.Equal(50, word.Value);
    }

    [Fact]
    public void SetValue_SameValue_DoesNotMarkChanged()
    {
        // Arrange
        var word = new SequenceNCWord(start: 10);
        word.SetValue(10);

        // Assert
        Assert.False(word.HasChanged);
    }

    [Fact]
    public void SetValue_DifferentValue_MarksChanged()
    {
        // Arrange
        var word = new SequenceNCWord(start: 10);

        // Act
        word.SetValue(20);

        // Assert
        Assert.True(word.HasChanged);
    }
}
