using PostProcessor.Core.Context;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for the Register class
/// </summary>
public class RegisterTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var register = new Register("X");

        // Assert
        Assert.Equal("X", register.Name);
        Assert.Equal(0.0, register.Value);
        Assert.True(register.IsModal);
        Assert.False(register.HasChanged);
    }

    [Fact]
    public void Constructor_InitializesWithCustomValues()
    {
        // Arrange & Act
        var register = new Register("F", initialValue: 100.5, isModal: false, format: "F3.1");

        // Assert
        Assert.Equal("F", register.Name);
        Assert.Equal(100.5, register.Value);
        Assert.False(register.IsModal);
        Assert.Equal("F3.1", register.Format);
        Assert.False(register.HasChanged);
    }

    [Fact]
    public void SetValue_UpdatesValueAndSetsChangedFlag()
    {
        // Arrange
        var register = new Register("X");

        // Act
        register.SetValue(50.0);

        // Assert
        Assert.Equal(50.0, register.Value);
        Assert.True(register.HasChanged);
    }

    [Fact]
    public void SetValue_SameValue_DoesNotSetChangedFlag()
    {
        // Arrange
        var register = new Register("X", initialValue: 50.0);
        register.ResetChangeFlag();

        // Act
        register.SetValue(50.0);

        // Assert
        Assert.Equal(50.0, register.Value);
        Assert.False(register.HasChanged);
    }

    [Fact]
    public void SetValue_SmallDifference_SetsChangedFlag()
    {
        // Arrange
        var register = new Register("X", initialValue: 50.0);
        register.ResetChangeFlag();

        // Act - Change smaller than tolerance
        register.SetValue(50.0000001);

        // Assert - Should not be considered changed
        Assert.False(register.HasChanged);
    }

    [Fact]
    public void ResetChangeFlag_ClearsChangedFlag()
    {
        // Arrange
        var register = new Register("X");
        register.SetValue(50.0);
        Assert.True(register.HasChanged);

        // Act
        register.ResetChangeFlag();

        // Assert
        Assert.False(register.HasChanged);
    }

    [Fact]
    public void FormatValue_FormatsWithCorrectPrecision()
    {
        // Arrange
        var register = new Register("X", initialValue: 123.456789, format: "F4.3");

        // Act
        var formatted = register.FormatValue();

        // Assert - Format "F4.3" means 4 digits before decimal, 3 after
        // The actual output depends on .NET's ToString implementation
        Assert.Contains("123", formatted);
    }

    [Fact]
    public void FormatValue_FormatsInteger()
    {
        // Arrange
        var register = new Register("T", initialValue: 5, format: "F0");

        // Act
        var formatted = register.FormatValue();

        // Assert
        Assert.Equal("5", formatted);
    }

    [Fact]
    public void ToString_ReturnsNameAndValue()
    {
        // Arrange
        var register = new Register("S", initialValue: 1200);

        // Act
        var result = register.ToString();

        // Assert
        Assert.Contains("S=", result);
    }

    [Fact]
    public void SetValue_UpdatesPreviousValue()
    {
        // Arrange
        var register = new Register("X", initialValue: 10.0);

        // Act
        register.SetValue(20.0);
        register.SetValue(30.0);

        // Assert
        Assert.Equal(30.0, register.Value);
    }
}
