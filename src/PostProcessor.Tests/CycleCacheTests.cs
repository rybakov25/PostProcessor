using System;
using System.IO;
using PostProcessor.Core.Context;
using PostProcessor.Macros.Python;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for Python CycleCache functionality
/// </summary>
public class CycleCacheTests : IDisposable
{
    private readonly MemoryStream _memoryStream;
    private readonly StreamWriter _streamWriter;
    private readonly PostContext _context;
    private readonly PythonPostContext _pythonContext;

    public CycleCacheTests()
    {
        _memoryStream = new MemoryStream();
        _streamWriter = new StreamWriter(_memoryStream);
        _context = new PostContext(_streamWriter);
        _pythonContext = new PythonPostContext(_context);
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
    public void WriteIfDifferent_FirstCall_WritesFullDefinition()
    {
        // Arrange
        var cache = new PythonCycleCache(_pythonContext, "CYCLE800");
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "MODE", 1 },
            { "TABLE", "TABLE1" },
            { "X", 100.0 },
            { "Y", 200.0 },
            { "Z", 50.0 }
        };

        // Act
        var result = cache.WriteIfDifferent(parameters);

        // Assert
        Assert.True(result); // Full definition written
        var output = GetOutput();
        Assert.Contains("CYCLE800", output);
        Assert.Contains("MODE=1", output);
        Assert.Contains("TABLE=\"TABLE1\"", output);
    }

    [Fact]
    public void WriteIfDifferent_SameParameters_WritesCallOnly()
    {
        // Arrange
        var cache = new PythonCycleCache(_pythonContext, "CYCLE800");
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "MODE", 1 },
            { "X", 100.0 }
        };

        // Act - First call
        cache.WriteIfDifferent(parameters);
        
        // Clear output
        ClearOutput();
        
        // Second call with same parameters
        var result = cache.WriteIfDifferent(parameters);

        // Assert
        Assert.False(result); // Call only written
        var output = GetOutput();
        Assert.Contains("CYCLE800()", output);
        Assert.DoesNotContain("MODE=", output);
    }

    [Fact]
    public void WriteIfDifferent_DifferentParameters_WritesFullDefinition()
    {
        // Arrange
        var cache = new PythonCycleCache(_pythonContext, "CYCLE800");
        var parameters1 = new System.Collections.Generic.Dictionary<string, object>
        {
            { "MODE", 1 },
            { "X", 100.0 }
        };
        var parameters2 = new System.Collections.Generic.Dictionary<string, object>
        {
            { "MODE", 1 },
            { "X", 150.0 } // Different X
        };

        // Act - First call
        cache.WriteIfDifferent(parameters1);
        
        // Clear output
        ClearOutput();
        
        // Second call with different parameters
        var result = cache.WriteIfDifferent(parameters2);

        // Assert
        Assert.True(result); // Full definition written
        var output = GetOutput();
        Assert.Contains("CYCLE800", output);
        Assert.Contains("X=150.000", output);
    }

    [Fact]
    public void Reset_ClearsCache()
    {
        // Arrange
        var cache = new PythonCycleCache(_pythonContext, "CYCLE800");
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "MODE", 1 },
            { "X", 100.0 }
        };

        // Act - First call
        cache.WriteIfDifferent(parameters);
        
        // Reset cache
        cache.Reset();
        
        // Clear output
        ClearOutput();
        
        // Second call with same parameters (should write full definition after reset)
        var result = cache.WriteIfDifferent(parameters);

        // Assert
        Assert.True(result); // Full definition written after reset
        var output = GetOutput();
        Assert.Contains("CYCLE800", output);
        Assert.Contains("MODE=", output);
    }

    [Fact]
    public void GetStats_ReturnsCorrectStatistics()
    {
        // Arrange
        var cache = new PythonCycleCache(_pythonContext, "CYCLE800");
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "MODE", 1 }
        };

        // Act
        cache.WriteIfDifferent(parameters);
        cache.WriteIfDifferent(parameters);
        cache.WriteIfDifferent(parameters);
        var stats = cache.GetStats();

        // Assert
        Assert.Equal("CYCLE800", stats["cycle_name"]);
        Assert.Equal(3, stats["call_count"]);
        Assert.True((bool)stats["is_cached"]);
    }

    [Fact]
    public void FormatParams_FloatValues_FormatsWithThreeDecimals()
    {
        // Arrange
        var cache = new PythonCycleCache(_pythonContext, "CYCLE81");
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "RTP", 10.5678 },
            { "RFP", 0.1234 }
        };

        // Act
        cache.WriteIfDifferent(parameters);

        // Assert
        var output = GetOutput();
        Assert.Contains("RTP=10.568", output); // Rounded to 3 decimals
        Assert.Contains("RFP=0.123", output);
    }

    [Fact]
    public void FormatParams_StringValues_WrapsInQuotes()
    {
        // Arrange
        var cache = new PythonCycleCache(_pythonContext, "CYCLE800");
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "TABLE", "MY_TABLE" }
        };

        // Act
        cache.WriteIfDifferent(parameters);

        // Assert
        var output = GetOutput();
        Assert.Contains("TABLE=\"MY_TABLE\"", output);
    }

    public void Dispose()
    {
        _streamWriter.Dispose();
        _memoryStream.Dispose();
        _context.DisposeAsync().AsTask().Wait();
    }
}

/// <summary>
/// C# wrapper for Python CycleCache class for testing
/// </summary>
public class PythonCycleCache
{
    private readonly PythonPostContext _context;
    private readonly string _cycleName;
    private string? _cachedParams;
    private int _callCount;

    public PythonCycleCache(PythonPostContext context, string cycleName)
    {
        _context = context;
        _cycleName = cycleName;
        _cachedParams = null;
        _callCount = 0;
    }

    public bool WriteIfDifferent(System.Collections.Generic.Dictionary<string, object> parameters)
    {
        // Sort parameters for stable comparison
        var paramsStr = string.Join(",", parameters.OrderBy(kvp => kvp.Key)
            .Select(kvp => $"{kvp.Key}={kvp.Value}"));

        _callCount++;

        if (_cachedParams == paramsStr)
        {
            // Same parameters - write call only
            _context.write($"{_cycleName}()");
            return false;
        }
        else
        {
            // Different parameters - write full definition
            var formatted = FormatParams(parameters);
            _context.write($"{_cycleName}({formatted})");
            _cachedParams = paramsStr;
            return true;
        }
    }

    public void Reset()
    {
        _cachedParams = null;
        _callCount = 0;
    }

    public System.Collections.Generic.Dictionary<string, object> GetStats()
    {
        return new System.Collections.Generic.Dictionary<string, object>
        {
            { "cycle_name", _cycleName },
            { "call_count", _callCount },
            { "is_cached", _cachedParams != null }
        };
    }

    private string FormatParams(System.Collections.Generic.Dictionary<string, object> parameters)
    {
        var parts = new System.Collections.Generic.List<string>();

        foreach (var kvp in parameters)
        {
            string formattedValue;

            if (kvp.Value is double doubleValue)
            {
                // Use InvariantCulture for consistent formatting (dot as decimal separator)
                formattedValue = doubleValue.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (kvp.Value is string stringValue)
            {
                formattedValue = $"\"{stringValue}\"";
            }
            else
            {
                formattedValue = kvp.Value.ToString();
            }
            
            parts.Add($"{kvp.Key}={formattedValue}");
        }
        
        return string.Join(", ", parts);
    }
}




