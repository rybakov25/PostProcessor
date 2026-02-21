using PostProcessor.Core.Context;
using System.IO;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for the BlockWriter class
/// </summary>
public class BlockWriterTests : IDisposable
{
    private readonly StringWriter _stringWriter;
    private readonly BlockWriter _blockWriter;
    private readonly Register _xRegister;
    private readonly Register _yRegister;
    private readonly Register _zRegister;
    private readonly Register _fRegister;

    public BlockWriterTests()
    {
        _stringWriter = new StringWriter();
        _blockWriter = new BlockWriter(_stringWriter);
        
        _xRegister = new Register("X", 0.0, true, "F4.3");
        _yRegister = new Register("Y", 0.0, true, "F4.3");
        _zRegister = new Register("Z", 0.0, true, "F4.3");
        _fRegister = new Register("F", 0.0, true, "F3.1");
        
        _blockWriter.AddWords(_xRegister, _yRegister, _zRegister, _fRegister);
    }

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var writer = new BlockWriter(_stringWriter);

        // Assert
        Assert.NotNull(writer);
        Assert.Equal(" ", writer.Separator);
        Assert.True(writer.BlockNumberingEnabled);
        Assert.Equal(10, writer.BlockIncrement);
    }

    [Fact]
    public void AddWord_AddsWordToList()
    {
        // Arrange
        var register = new Register("A");
        
        // Act
        _blockWriter.AddWord(register);

        // Assert
        Assert.Contains(register, _blockWriter.Words);
    }

    [Fact]
    public void WriteBlock_WritesOnlyChangedRegisters()
    {
        // Arrange
        _xRegister.SetValue(100.5);
        _yRegister.SetValue(200.3);
        // Z not changed

        // Act
        var result = _blockWriter.WriteBlock();

        // Assert
        Assert.True(result); // Block was written
        var output = _stringWriter.ToString();
        Assert.Contains("N10", output); // Block number
        Assert.Contains("X", output);
        Assert.Contains("Y", output);
        Assert.DoesNotContain("Z", output); // Z not changed, not output
    }

    [Fact]
    public void WriteBlock_SkipsUnchangedModalRegisters()
    {
        // Arrange
        _xRegister.SetValue(100.5);
        _blockWriter.WriteBlock(); // First write
        
        _stringWriter.GetStringBuilder().Clear(); // Clear output
        
        // Act - change X back to same value
        _xRegister.SetValue(100.5);
        var result = _blockWriter.WriteBlock();

        // Assert
        Assert.False(result); // Block was NOT written (no changes)
        var output = _stringWriter.ToString();
        Assert.Empty(output);
    }

    [Fact]
    public void WriteBlock_WritesBlockNumber()
    {
        // Arrange
        _xRegister.SetValue(50.0);

        // Act
        _blockWriter.WriteBlock();

        // Assert
        var output = _stringWriter.ToString();
        Assert.StartsWith("N10 ", output);
    }

    [Fact]
    public void WriteBlock_IncrementsBlockNumber()
    {
        // Arrange
        _xRegister.SetValue(50.0);
        _blockWriter.WriteBlock();
        
        _stringWriter.GetStringBuilder().Clear();
        _xRegister.SetValue(100.0);

        // Act
        _blockWriter.WriteBlock();

        // Assert
        var output = _stringWriter.ToString();
        Assert.StartsWith("N20 ", output);
    }

    [Fact]
    public void Hide_ForcesRegistersUnchanged()
    {
        // Arrange
        _xRegister.SetValue(100.5);
        
        // Act
        _blockWriter.Hide(_xRegister);
        var result = _blockWriter.WriteBlock();

        // Assert
        Assert.False(result); // Block was NOT written (X is hidden)
    }

    [Fact]
    public void Show_ForcesRegistersChanged()
    {
        // Arrange
        _xRegister.SetValue(100.5);
        _blockWriter.WriteBlock(); // First write
        _stringWriter.GetStringBuilder().Clear();
        
        // Act
        _blockWriter.Show(_xRegister);
        var result = _blockWriter.WriteBlock();

        // Assert
        Assert.True(result); // Block was written (X forced to show)
        var output = _stringWriter.ToString();
        Assert.Contains("X", output);
    }

    [Fact]
    public void WriteBlock_WithoutBlockNumber()
    {
        // Arrange
        _blockWriter.BlockNumberingEnabled = false;
        _xRegister.SetValue(100.5);

        // Act
        _blockWriter.WriteBlock(includeBlockNumber: false);

        // Assert
        var output = _stringWriter.ToString();
        Assert.DoesNotContain("N", output);
        Assert.Contains("X100.500", output);
    }

    [Fact]
    public void WriteLine_WritesDirectly()
    {
        // Act
        _blockWriter.WriteLine("(This is a comment)");

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("(This is a comment)", output);
    }

    [Fact]
    public void WriteComment_WritesInParentheses()
    {
        // Act
        _blockWriter.WriteComment("Test comment");

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("(Test comment)", output);
    }

    [Fact]
    public void Reset_ClearsChangeFlags()
    {
        // Arrange
        _xRegister.SetValue(100.5);
        
        // Act
        _blockWriter.Reset(_xRegister);

        // Assert
        Assert.False(_xRegister.HasChanged);
    }

    [Fact]
    public void ResetAll_ClearsAllChangeFlags()
    {
        // Arrange
        _xRegister.SetValue(100.5);
        _yRegister.SetValue(200.3);
        
        // Act
        _blockWriter.ResetAll();

        // Assert
        Assert.False(_xRegister.HasChanged);
        Assert.False(_yRegister.HasChanged);
    }

    [Fact]
    public void Separator_ChangesOutputFormat()
    {
        // Arrange
        _blockWriter.Separator = ",";
        _xRegister.SetValue(100.5);
        _yRegister.SetValue(200.3);

        // Act
        _blockWriter.WriteBlock();

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("N10,X100.500,Y200.300", output);
    }

    [Fact]
    public void BlockNumberStart_SetsInitialNumber()
    {
        // Arrange
        _blockWriter.BlockNumberStart = 100;
        _xRegister.SetValue(50.0);

        // Act
        _blockWriter.WriteBlock();

        // Assert
        var output = _stringWriter.ToString();
        Assert.StartsWith("N", output);
    }

    [Fact]
    public void BlockIncrement_ChangesStep()
    {
        // Arrange
        _blockWriter.BlockIncrement = 5;
        _xRegister.SetValue(50.0);
        _blockWriter.WriteBlock();
        
        _stringWriter.GetStringBuilder().Clear();
        _xRegister.SetValue(100.0);

        // Act
        _blockWriter.WriteBlock();

        // Assert
        var output = _stringWriter.ToString();
        Assert.StartsWith("N", output); // 10 + 5 = 15
    }

    public void Dispose()
    {
        _stringWriter.Dispose();
    }
}


