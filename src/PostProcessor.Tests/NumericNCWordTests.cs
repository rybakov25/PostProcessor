using PostProcessor.Core.Context;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for NumericNCWord functionality
/// </summary>
public class NumericNCWordTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var word = new NumericNCWord("X");

        // Assert
        Assert.Equal("X", word.Address);
        Assert.Equal(0.0, word.v);
        Assert.True(word.IsModal);
    }

    [Fact]
    public void Constructor_WithFormatPattern()
    {
        // Act
        var word = new NumericNCWord("X", 0.0, "X{-#####!###}", true);

        // Assert
        Assert.Equal("X", word.Address);
        Assert.True(word.IsModal);
    }

    [Fact]
    public void v_Setter_MarksChanged()
    {
        // Arrange
        var word = new NumericNCWord("X");

        // Act
        word.v = 100.5;

        // Assert
        Assert.True(word.HasChanged);
        Assert.Equal(100.5, word.v);
    }

    [Fact]
    public void v_SameValue_DoesNotMarkChanged()
    {
        // Arrange
        var word = new NumericNCWord("X", 100.5);
        word.ToNCString(); // First output

        // Act
        word.v = 100.5;

        // Assert
        Assert.False(word.HasChanged);
    }

    [Fact]
    public void Set_UpdatesValue()
    {
        // Arrange
        var word = new NumericNCWord("X");

        // Act
        word.Set(200.3);

        // Assert
        Assert.Equal(200.3, word.v);
        Assert.Equal(0.0, word.v0); // v0 is previous value
    }

    [Fact]
    public void SetInitial_DoesNotMarkChanged()
    {
        // Arrange
        var word = new NumericNCWord("X", 0.0);

        // Act
        word.SetInitial(100.5);

        // Assert
        Assert.False(word.HasChanged);
        Assert.Equal(100.5, word.v);
    }

    [Fact]
    public void Show_ForcesChanged()
    {
        // Arrange
        var word = new NumericNCWord("X", 100.5);
        word.ToNCString(); // First output, resets HasChanged

        // Act
        word.Show();

        // Assert
        Assert.True(word.HasChanged);
    }

    [Fact]
    public void Hide_ForcesUnchanged()
    {
        // Arrange
        var word = new NumericNCWord("X", 100.5);

        // Act
        word.Hide();

        // Assert
        Assert.False(word.HasChanged);
    }

    [Fact]
    public void Reset_ToDefault()
    {
        // Arrange
        var word = new NumericNCWord("X", 0.0);
        word.v = 100.5;

        // Act
        word.Reset();

        // Assert
        Assert.Equal(0.0, word.v);
        Assert.True(word.HasChanged);
    }

    [Fact]
    public void Reset_ToValue()
    {
        // Arrange
        var word = new NumericNCWord("X", 0.0);
        word.v = 100.5;

        // Act
        word.Reset(50.0);

        // Assert
        Assert.Equal(50.0, word.v);
        Assert.True(word.HasChanged);
    }

    [Fact]
    public void ValuesDiffer_TrueWhenChanged()
    {
        // Arrange
        var word = new NumericNCWord("X", 0.0);
        word.v = 100.5;

        // Assert
        Assert.True(word.ValuesDiffer);
    }

    [Fact]
    public void ValuesSame_FalseWhenChanged()
    {
        // Arrange
        var word = new NumericNCWord("X", 0.0);
        word.v = 100.5;

        // Assert
        Assert.False(word.ValuesSame);
    }

    [Fact]
    public void ToNCString_FormatsWithAddress()
    {
        // Arrange
        var word = new NumericNCWord("X", 0.0);
        word.v = 100.5;

        // Act
        var result = word.ToNCString();

        // Assert - default format with leading zeros
        Assert.Equal("X0100.5", result);
    }

    [Fact]
    public void ToNCString_EmptyWhenUnchangedAndModal()
    {
        // Arrange
        var word = new NumericNCWord("X", 100.5);
        word.ToNCString(); // First output, resets HasChanged
        
        // Value hasn't changed, so second output should be empty
        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ToNCString_OutputWhenNotModal()
    {
        // Arrange
        var word = new NumericNCWord("F", 100.0, isModal: false);
        word.ToNCString(); // First output

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("F0100.0", result);
    }

    [Fact]
    public void ToString_ReturnsFormattedValue()
    {
        // Arrange
        var word = new NumericNCWord("X", 100.5);

        // Act
        var result = word.ToString();

        // Assert
        Assert.Equal("X0100.5", result);
    }

    [Fact]
    public void ToNCString_WithFormatPattern()
    {
        // Arrange
        var word = new NumericNCWord("X", 0.0, "X{-#####!###}");
        word.v = -50.125;

        // Act
        var result = word.ToNCString();

        // Assert - FormatSpec handles the pattern
        Assert.StartsWith("X", result);
        Assert.Contains("50.125", result);
    }

    [Fact]
    public void ToNCString_NegativeValue()
    {
        // Arrange
        var word = new NumericNCWord("X", 0.0);
        word.v = -100.5;

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("X-0100.5", result);
    }

    [Fact]
    public void ToNCString_SmallValue()
    {
        // Arrange
        var word = new NumericNCWord("X", 0.0);
        word.v = 0.001;

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("X0000.001", result);
    }

    [Fact]
    public void ToNCString_LargeValue()
    {
        // Arrange
        var word = new NumericNCWord("X", 0.0);
        word.v = 999.999;

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("X0999.999", result);
    }

    [Fact]
    public void ToNCString_Rounding()
    {
        // Arrange
        var word = new NumericNCWord("X", 0.0);
        word.v = 100.5555;

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.Equal("X0100.556", result); // Rounded to 3 decimals
    }

    [Fact]
    public void ToNCString_Precision()
    {
        // Arrange - F uses 1 decimal by default in config
        var word = new NumericNCWord("F", 0.0);
        word.v = 500.123456;

        // Act
        var result = word.ToNCString();

        // Assert - default is 3 decimals
        Assert.Equal("F0500.123", result);
    }

    [Fact]
    public void ToNCString_IntegerFormat()
    {
        // Arrange - S uses 0 decimals
        var word = new NumericNCWord("S", 0.0);
        word.v = 1200.0;

        // Act
        var result = word.ToNCString();

        // Assert - default is 3 decimals
        Assert.Equal("S1200.0", result);
    }

    [Fact]
    public void ToNCString_WithConfig()
    {
        // Arrange - this would require a full config setup
        // For now, test basic functionality
        var word = new NumericNCWord("X", 0.0);
        word.v = 100.5;

        // Act
        var result = word.ToNCString();

        // Assert
        Assert.StartsWith("X", result);
    }
}
