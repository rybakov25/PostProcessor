using PostProcessor.Core.Context;
using PostProcessor.Core.Models;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for the PostContext class
/// </summary>
public class PostContextTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly StreamWriter _testWriter;

    public PostContextTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.nc");
        _testWriter = new StreamWriter(_testFilePath);
    }

    [Fact]
    public void Constructor_InitializesWithEmptyRegisters()
    {
        // Arrange & Act
        var context = new PostContext(_testWriter);

        // Assert
        Assert.NotNull(context.Registers);
        Assert.Equal(0.0, context.Registers.X.Value);
        Assert.Equal(0.0, context.Registers.Y.Value);
        Assert.Equal(0.0, context.Registers.Z.Value);
    }

    [Fact]
    public void Constructor_InitializesStatisticsToZero()
    {
        // Arrange & Act
        var context = new PostContext(_testWriter);
        var stats = context.GetStatistics();

        // Assert
        Assert.Equal(0, stats.CommandCount);
        Assert.Equal(0, stats.MotionCount);
        Assert.Equal(0, stats.ToolChanges);
    }

    [Fact]
    public async Task ProcessCommandAsync_IncrementsCommandCount()
    {
        // Arrange
        var context = new PostContext(_testWriter);
        var command = new APTCommand("goto", new List<string>(), new List<double> { 10.0, 20.0, 30.0 }, new List<string>(), 1);

        // Act
        await foreach (var evt in context.ProcessCommandAsync(command))
        {
            // Process events
        }

        // Assert
        var stats = context.GetStatistics();
        Assert.Equal(1, stats.CommandCount);
    }

    [Fact]
    public async Task ProcessCommandAsync_IncrementsMotionCountForGoto()
    {
        // Arrange
        var context = new PostContext(_testWriter);
        var command = new APTCommand("goto", new List<string>(), new List<double> { 10.0, 20.0, 30.0 }, new List<string>(), 1);

        // Act
        await foreach (var evt in context.ProcessCommandAsync(command))
        {
            // Process events
        }

        // Assert
        var stats = context.GetStatistics();
        Assert.Equal(1, stats.MotionCount);
    }

    [Fact]
    public async Task ProcessCommandAsync_IncrementsMotionCountForRapid()
    {
        // Arrange
        var context = new PostContext(_testWriter);
        var command = new APTCommand("rapid", new List<string>(), new List<double> { 10.0, 20.0, 30.0 }, new List<string>(), 1);

        // Act
        await foreach (var evt in context.ProcessCommandAsync(command))
        {
            // Process events
        }

        // Assert
        var stats = context.GetStatistics();
        Assert.Equal(1, stats.MotionCount);
    }

    [Fact]
    public void SetSystemVariable_StoresAndRetrievesValue()
    {
        // Arrange
        var context = new PostContext(_testWriter);

        // Act
        context.SetSystemVariable("TEST_VAR", 42);
        var value = context.GetSystemVariable<int>("TEST_VAR");

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void GetSystemVariable_ReturnsDefaultValueForNonExistentVariable()
    {
        // Arrange
        var context = new PostContext(_testWriter);

        // Act
        var value = context.GetSystemVariable("NON_EXISTENT", "default");

        // Assert
        Assert.Equal("default", value);
    }

    [Fact]
    public void SetSystemVariable_OverwritesExistingValue()
    {
        // Arrange
        var context = new PostContext(_testWriter);
        context.SetSystemVariable("TEST_VAR", 42);

        // Act
        context.SetSystemVariable("TEST_VAR", 100);
        var value = context.GetSystemVariable<int>("TEST_VAR");

        // Assert
        Assert.Equal(100, value);
    }

    [Fact]
    public void SetSystemVariable_StoresDifferentTypes()
    {
        // Arrange
        var context = new PostContext(_testWriter);

        // Act & Assert - String
        context.SetSystemVariable("STR_VAR", "test");
        Assert.Equal("test", context.GetSystemVariable<string>("STR_VAR"));

        // Act & Assert - Double
        context.SetSystemVariable("DBL_VAR", 3.14);
        Assert.Equal(3.14, context.GetSystemVariable<double>("DBL_VAR"));

        // Act & Assert - Boolean
        context.SetSystemVariable("BOOL_VAR", true);
        Assert.True(context.GetSystemVariable<bool>("BOOL_VAR"));
    }

    public void Dispose()
    {
        _testWriter?.Dispose();
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}
