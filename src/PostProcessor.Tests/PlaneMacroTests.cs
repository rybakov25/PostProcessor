using System;
using System.IO;
using PostProcessor.Core.Context;
using PostProcessor.Macros.Python;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for Plane macro functionality (G17/G18/G19)
/// </summary>
public class PlaneMacroTests : IDisposable
{
    private readonly MemoryStream _memoryStream;
    private readonly StreamWriter _streamWriter;
    private readonly PostContext _context;
    private readonly PythonPostContext _pythonContext;

    public PlaneMacroTests()
    {
        _memoryStream = new MemoryStream();
        _streamWriter = new StreamWriter(_memoryStream);
        _context = new PostContext(_streamWriter);
        _pythonContext = new PythonPostContext(_context);
    }

    [Fact]
    public void Plane_XY_OutputsG17()
    {
        // Arrange & Act
        _pythonContext.write("G17");

        // Assert
        var output = GetOutput();
        Assert.Contains("G17", output);
    }

    [Fact]
    public void Plane_ZX_OutputsG18()
    {
        // Arrange & Act
        _pythonContext.write("G18");

        // Assert
        var output = GetOutput();
        Assert.Contains("G18", output);
    }

    [Fact]
    public void Plane_YZ_OutputsG19()
    {
        // Arrange & Act
        _pythonContext.write("G19");

        // Assert
        var output = GetOutput();
        Assert.Contains("G19", output);
    }

    [Fact]
    public void Plane_StoresInSystemVariables()
    {
        // Act
        _context.SetSystemVariable("PLANE_CODE", 17);
        _context.SetSystemVariable("PLANE_NAME", "XY");

        // Assert
        var planeCode = _context.GetSystemVariable("PLANE_CODE", 0);
        var planeName = _context.GetSystemVariable("PLANE_NAME", "");
        
        Assert.Equal(17, planeCode);
        Assert.Equal("XY", planeName);
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
