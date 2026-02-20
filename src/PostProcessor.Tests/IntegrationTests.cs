using PostProcessor.APT.Lexer;
using PostProcessor.Core.Context;

namespace PostProcessor.Tests;

/// <summary>
/// Integration tests for the full postprocessing pipeline
/// </summary>
public class IntegrationTests : IDisposable
{
    private readonly string _testInputPath;
    private readonly string _testOutputPath;

    public IntegrationTests()
    {
        _testInputPath = Path.Combine(Path.GetTempPath(), $"test_input_{Guid.NewGuid()}.apt");
        _testOutputPath = Path.Combine(Path.GetTempPath(), $"test_output_{Guid.NewGuid()}.nc");
    }

    [Fact]
    public async Task Lexer_ProcessesSimpleAptFile()
    {
        // Arrange - Create simple APT file
        var aptContent = @"GOTO/10.0, 20.0, 30.0
GOTO/50.0, 60.0, 70.0";
        await File.WriteAllTextAsync(_testInputPath, aptContent);

        // Act - Process with lexer
        var commands = new List<Core.Models.APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testInputPath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert - Check commands parsed
        Assert.Equal(2, commands.Count);
        Assert.Equal("goto", commands[0].MajorWord);
        Assert.Equal("goto", commands[1].MajorWord);
    }

    [Fact]
    public async Task Lexer_ProcessesWithRapidMotion()
    {
        // Arrange
        var aptContent = @"RAPID/100.0, 200.0, 300.0
GOTO/50.0, 60.0, 70.0";
        await File.WriteAllTextAsync(_testInputPath, aptContent);

        // Act
        var commands = new List<Core.Models.APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testInputPath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert
        Assert.Equal(2, commands.Count);
        Assert.Equal("rapid", commands[0].MajorWord);
    }

    [Fact]
    public async Task Lexer_ProcessesWithSpindle()
    {
        // Arrange
        var aptContent = @"SPINDL/ON, CLW, 1500
GOTO/10.0, 20.0, 30.0";
        await File.WriteAllTextAsync(_testInputPath, aptContent);

        // Act
        var commands = new List<Core.Models.APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testInputPath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert
        Assert.Equal(2, commands.Count);
        Assert.Equal("spindl", commands[0].MajorWord);
        Assert.Contains("on", commands[0].MinorWords);
        Assert.Contains("clw", commands[0].MinorWords);
        Assert.Equal(1500, commands[0].NumericValues[0]);
    }

    [Fact]
    public async Task Lexer_ProcessesWithCoolant()
    {
        // Arrange
        var aptContent = @"COOLNT/ON
GOTO/10.0, 20.0, 30.0
COOLNT/OFF";
        await File.WriteAllTextAsync(_testInputPath, aptContent);

        // Act
        var commands = new List<Core.Models.APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testInputPath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert
        Assert.Equal(3, commands.Count);
        Assert.Equal("coolnt", commands[0].MajorWord);
    }

    [Fact]
    public async Task Lexer_ProcessesWithFeedrate()
    {
        // Arrange
        var aptContent = @"FEDRAT/100.0
GOTO/10.0, 20.0, 30.0
GOTO/50.0, 60.0, 70.0";
        await File.WriteAllTextAsync(_testInputPath, aptContent);

        // Act
        var commands = new List<Core.Models.APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testInputPath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert
        Assert.Equal(3, commands.Count);
        Assert.Equal("fedrat", commands[0].MajorWord);
        Assert.Equal(100.0, commands[0].NumericValues[0]);
    }

    [Fact]
    public async Task Lexer_ProcessesWithToolChange()
    {
        // Arrange
        var aptContent = @"LOADTL/5
GOTO/10.0, 20.0, 30.0";
        await File.WriteAllTextAsync(_testInputPath, aptContent);

        // Act
        var commands = new List<Core.Models.APTCommand>();
        await using (var lexer = new StreamingAPTLexer(_testInputPath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        // Assert
        Assert.Equal(2, commands.Count);
        Assert.Equal("loadtl", commands[0].MajorWord);
        Assert.Equal(5, commands[0].NumericValues[0]);
    }

    [Fact]
    public async Task Context_StatisticsAreCorrect()
    {
        // Arrange
        var aptContent = @"GOTO/10.0, 20.0, 30.0
RAPID/50.0, 60.0, 70.0
GOTO/100.0, 200.0, 300.0";
        await File.WriteAllTextAsync(_testInputPath, aptContent);

        // Act
        await using var outputWriter = new StreamWriter(_testOutputPath);
        var context = new PostContext(outputWriter);
        var commands = new List<Core.Models.APTCommand>();
        
        await using (var lexer = new StreamingAPTLexer(_testInputPath))
        {
            await foreach (var command in lexer.ParseStreamAsync())
            {
                commands.Add(command);
            }
        }

        foreach (var command in commands)
        {
            await foreach (var evt in context.ProcessCommandAsync(command))
            {
                // Process events
            }
        }

        // Assert
        var stats = context.GetStatistics();
        Assert.Equal(3, stats.CommandCount);  // GOTO, RAPID, GOTO
        Assert.Equal(3, stats.MotionCount);   // All are motions
    }

    public void Dispose()
    {
        if (File.Exists(_testInputPath))
        {
            File.Delete(_testInputPath);
        }
        if (File.Exists(_testOutputPath))
        {
            File.Delete(_testOutputPath);
        }
    }
}
