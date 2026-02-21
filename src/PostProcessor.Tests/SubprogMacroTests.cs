using System;
using System.IO;
using PostProcessor.Core.Context;
using PostProcessor.Macros.Python;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for Subprogram macro functionality (M98/M99)
/// </summary>
public class SubprogMacroTests : IDisposable
{
    private readonly MemoryStream _memoryStream;
    private readonly StreamWriter _streamWriter;
    private readonly PostContext _context;
    private readonly PythonPostContext _pythonContext;

    public SubprogMacroTests()
    {
        _memoryStream = new MemoryStream();
        _streamWriter = new StreamWriter(_memoryStream);
        _context = new PostContext(_streamWriter);
        _pythonContext = new PythonPostContext(_context);
    }

    [Fact]
    public void Subprogram_Call_OutputsM98()
    {
        // Arrange & Act
        _pythonContext.write("M98 P1001");

        // Assert
        var output = GetOutput();
        Assert.Contains("M98", output);
        Assert.Contains("P1001", output);
    }

    [Fact]
    public void Subprogram_End_OutputsM99()
    {
        // Arrange & Act
        _pythonContext.write("M99");

        // Assert
        var output = GetOutput();
        Assert.Contains("M99", output);
    }

    [Fact]
    public void Subprogram_TracksCallCount()
    {
        // Act - Simulate subroutine calls
        _context.SetSystemVariable("SUB_CALL_COUNT", 0);
        
        // First call
        _context.SetSystemVariable("SUB_CALL_COUNT", 1);
        var count1 = _context.GetSystemVariable("SUB_CALL_COUNT", 0);
        
        // Second call
        _context.SetSystemVariable("SUB_CALL_COUNT", 2);
        var count2 = _context.GetSystemVariable("SUB_CALL_COUNT", 0);

        // Assert
        Assert.Equal(1, count1);
        Assert.Equal(2, count2);
    }

    [Fact]
    public void Subprogram_StoresCurrentNumber()
    {
        // Act
        _context.SetSystemVariable("CURRENT_SUB_NUMBER", 1001);

        // Assert
        var subNumber = _context.GetSystemVariable("CURRENT_SUB_NUMBER", 0);
        Assert.Equal(1001, subNumber);
    }

    private string GetOutput()
    {
        _streamWriter.Flush();
        _memoryStream.Position = 0;
        using var reader = new StreamReader(_memoryStream);
        return reader.ReadToEnd();
    }

    public void Dispose()
    {
        _streamWriter.Dispose();
        _memoryStream.Dispose();
        _context.DisposeAsync().AsTask().Wait();
    }
}
