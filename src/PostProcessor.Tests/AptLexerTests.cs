using PostProcessor.APT.Lexer;
using PostProcessor.Core.Models;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for the APT Lexer
/// </summary>
public class AptLexerTests : IDisposable
{
    private readonly string _testFilePath;

    public AptLexerTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"apt_test_{Guid.NewGuid()}.apt");
    }

    [Fact]
    public async Task ParseStreamAsync_ParsesSimpleCommand()
    {
        // Arrange
        await File.WriteAllTextAsync(_testFilePath, "GOTO/10.0, 20.0, 30.0");

        // Act
        var commands = new List<APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testFilePath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert
        Assert.Single(commands);
        Assert.Equal("goto", commands[0].MajorWord);
        Assert.Equal(3, commands[0].NumericValues.Count);
        Assert.Equal(10.0, commands[0].NumericValues[0]);
        Assert.Equal(20.0, commands[0].NumericValues[1]);
        Assert.Equal(30.0, commands[0].NumericValues[2]);
    }

    [Fact]
    public async Task ParseStreamAsync_ParsesMultipleCommands()
    {
        // Arrange
        var content = @"GOTO/10.0, 20.0, 30.0
RAPID/50.0, 60.0, 70.0
SPINDL/ON, CLW, 1200";
        await File.WriteAllTextAsync(_testFilePath, content);

        // Act
        var commands = new List<APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testFilePath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert
        Assert.Equal(3, commands.Count);
        Assert.Equal("goto", commands[0].MajorWord);
        Assert.Equal("rapid", commands[1].MajorWord);
        Assert.Equal("spindl", commands[2].MajorWord);
    }

    [Fact]
    public async Task ParseStreamAsync_SkipsEmptyLines()
    {
        // Arrange
        var content = @"GOTO/10.0

RAPID/20.0

SPINDL/ON";
        await File.WriteAllTextAsync(_testFilePath, content);

        // Act
        var commands = new List<APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testFilePath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert
        Assert.Equal(3, commands.Count);
    }

    [Fact]
    public async Task ParseStreamAsync_SkipsComments()
    {
        // Arrange
        var content = @"GOTO/10.0 $$ This is a comment
RAPID/20.0 $$ Another comment
$$ Comment on its own line
SPINDL/ON";
        await File.WriteAllTextAsync(_testFilePath, content);

        // Act
        var commands = new List<APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testFilePath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert
        Assert.Equal(3, commands.Count);
    }

    [Fact]
    public async Task ParseStreamAsync_HandlesContinuationCharacter()
    {
        // Arrange
        var content = @"GOTO/10.0, $
20.0, $
30.0";
        await File.WriteAllTextAsync(_testFilePath, content);

        // Act
        var commands = new List<APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testFilePath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert
        Assert.Single(commands);
        Assert.Equal(3, commands[0].NumericValues.Count);
    }

    [Fact]
    public async Task ParseStreamAsync_ParsesMinorWords()
    {
        // Arrange
        var content = "SPINDL/ON, CLW, 1200";
        await File.WriteAllTextAsync(_testFilePath, content);

        // Act
        var commands = new List<APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testFilePath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert
        Assert.Single(commands);
        Assert.Contains("on", commands[0].MinorWords);
        Assert.Contains("clw", commands[0].MinorWords);
        Assert.Equal(1200, commands[0].NumericValues[0]);
    }

    [Fact]
    public async Task ParseStreamAsync_HandlesCaseInsensitive()
    {
        // Arrange
        var content = @"goto/10.0, 20.0
RAPID/30.0, 40.0
SpIndL/ON";
        await File.WriteAllTextAsync(_testFilePath, content);

        // Act
        var commands = new List<APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testFilePath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert
        Assert.Equal(3, commands.Count);
        // Major words should be normalized to lowercase
        Assert.Equal("goto", commands[0].MajorWord);
        Assert.Equal("rapid", commands[1].MajorWord);
        Assert.Equal("spindl", commands[2].MajorWord);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}
