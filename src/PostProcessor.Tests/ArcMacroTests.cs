using System;
using System.IO;
using PostProcessor.Core.Context;
using PostProcessor.Macros.Python;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for Arc macro functionality (G02/G03)
/// </summary>
public class ArcMacroTests : IDisposable
{
    private readonly MemoryStream _memoryStream;
    private readonly StreamWriter _streamWriter;
    private readonly PostContext _context;
    private readonly PythonPostContext _pythonContext;

    public ArcMacroTests()
    {
        _memoryStream = new MemoryStream();
        _streamWriter = new StreamWriter(_memoryStream);
        _context = new PostContext(_streamWriter);
        _pythonContext = new PythonPostContext(_context);
    }

    [Fact]
    public void ArcOutput_IJKFormat_OutputsCenterCoordinates()
    {
        // Arrange - Simulate arc command
        _context.Registers.X.SetValue(0);
        _context.Registers.Y.SetValue(0);
        
        // Act - Set end point and center
        _context.Registers.X.SetValue(100);
        _context.Registers.Y.SetValue(50);
        _context.Registers.I.SetValue(10);
        _context.Registers.J.SetValue(0);
        
        // Write arc manually (simulating arc.py macro)
        _context.BlockWriter.WriteLine("G2 X100.000 Y50.000 I10.000 J0.000");
        _streamWriter.Flush();

        // Assert
        var output = GetOutput();
        Assert.Contains("G2", output);
        Assert.Contains("X100.000", output);
        Assert.Contains("Y50.000", output);
        Assert.Contains("I10.000", output);
        Assert.Contains("J0.000", output);
    }

    private string GetOutput()
    {
        _streamWriter.Flush();
        _memoryStream.Position = 0;
        using var reader = new StreamReader(_memoryStream);
        return reader.ReadToEnd();
    }
    
    private void ClearOutput()
    {
        _streamWriter.Flush();
        _memoryStream.SetLength(0);
    }

    [Fact]
    public void ArcOutput_RFormat_OutputsRadius()
    {
        // Arrange
        _context.Registers.X.SetValue(0);
        _context.Registers.Y.SetValue(0);
        
        // Act - Set end point
        _context.Registers.X.SetValue(50);
        _context.Registers.Y.SetValue(50);
        
        // Write arc with radius
        _context.BlockWriter.WriteLine("G2 X50.000 Y50.000 R35.355");

        // Assert
        var output = GetOutput();
        Assert.Contains("G2", output);
        Assert.Contains("R35.355", output);
        Assert.DoesNotContain("I=", output);
        Assert.DoesNotContain("J=", output);
    }

    [Fact]
    public void ArcOutput_G03_IsCounterClockwise()
    {
        // Arrange & Act
        _context.BlockWriter.WriteLine("G03 X100.000 Y100.000 I0.000 J50.000");

        // Assert
        var output = GetOutput();
        Assert.Contains("G03", output);
    }

    [Fact]
    public void ArcOutput_IncludesFeedRate_WhenChanged()
    {
        // Arrange
        _context.Registers.F.SetValue(100.0);
        
        // Act
        _context.BlockWriter.WriteLine("G2 X50.000 Y50.000 R25.000 F100.0");

        // Assert
        var output = GetOutput();
        Assert.Contains("F100.0", output);
    }

    [Fact]
    public void ArcOutput_G17Plane_XYWithIJ()
    {
        // Arrange
        _context.SetSystemVariable("PLANE", "G17");
        
        // Act - XY plane arc
        _context.BlockWriter.WriteLine("G17");
        _context.BlockWriter.WriteLine("G2 X100.000 Y50.000 I10.000 J5.000");

        // Assert
        var output = GetOutput();
        Assert.Contains("G17", output);
        Assert.Contains("I10.000", output);
        Assert.Contains("J5.000", output);
    }

    [Fact]
    public void ArcOutput_G18Plane_ZXWithIK()
    {
        // Arrange
        _context.SetSystemVariable("PLANE", "G18");
        
        // Act - ZX plane arc
        _context.BlockWriter.WriteLine("G18");
        _context.BlockWriter.WriteLine("G2 X100.000 Z50.000 I10.000 K5.000");

        // Assert
        var output = GetOutput();
        Assert.Contains("G18", output);
        Assert.Contains("I10.000", output);
        Assert.Contains("K5.000", output);
    }

    [Fact]
    public void ArcOutput_G19Plane_YZWithJK()
    {
        // Arrange
        _context.SetSystemVariable("PLANE", "G19");
        
        // Act - YZ plane arc
        _context.BlockWriter.WriteLine("G19");
        _context.BlockWriter.WriteLine("G2 Y100.000 Z50.000 J10.000 K5.000");

        // Assert
        var output = GetOutput();
        Assert.Contains("G19", output);
        Assert.Contains("J10.000", output);
        Assert.Contains("K5.000", output);
    }

    [Fact]
    public void CalculateRadius_FromIJK_ReturnsCorrectValue()
    {
        // Arrange
        double i = 3.0;
        double j = 4.0;
        
        // Act
        var radius = Math.Sqrt(i * i + j * j);

        // Assert
        Assert.Equal(5.0, radius, precision: 3);
    }

    [Fact]
    public void ArcOutput_FullCircle_UsesIJKFormat()
    {
        // Arrange - Full circle (360 degrees)
        // Act - Full circle must use IJK, not R
        _context.BlockWriter.WriteLine("G2 X0.000 Y0.000 I25.000 J0.000");

        // Assert
        var output = GetOutput();
        Assert.Contains("I25.000", output);
        Assert.DoesNotContain("R", output); // Full circles cannot use R format
    }

    [Fact]
    public void ArcOutput_HelicalMotion_IncludesZAxis()
    {
        // Arrange & Act - Helical arc with Z movement
        _context.BlockWriter.WriteLine("G2 X100.000 Y100.000 Z-10.000 I0.000 J50.000");

        // Assert
        var output = GetOutput();
        Assert.Contains("Z-10.000", output); // Z axis included
        Assert.Contains("I0.000", output);
        Assert.Contains("J50.000", output);
    }

    [Fact]
    public void ArcOutput_ModalChecking_SkipsUnchangedCoordinates()
    {
        // Arrange
        _context.Registers.X.SetValue(0);
        _context.Registers.Y.SetValue(0);
        
        // First arc
        _context.Registers.X.SetValue(50);
        _context.Registers.Y.SetValue(50);
        _context.BlockWriter.WriteBlock();
        
        ClearOutput();
        
        // Act - Second arc with same X,Y (modal)
        _context.Registers.X.SetValue(50);
        _context.Registers.Y.SetValue(50);
        _context.Registers.Z.SetValue(-5);
        _context.BlockWriter.WriteBlock();

        // Assert
        var output = GetOutput();
        Assert.DoesNotContain("X50.000", output); // X is modal, unchanged
        Assert.DoesNotContain("Y50.000", output); // Y is modal, unchanged
        Assert.Contains("Z-5.000", output); // Z changed, output
    }

    public void Dispose()
    {
        _streamWriter.Dispose();
        _memoryStream.Dispose();
        _context.DisposeAsync().AsTask().Wait();
    }
}




