using System.IO;
using PostProcessor.Core.Context;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for CycleCache functionality
/// </summary>
public class CycleCacheTests
{
    private readonly StringWriter _stringWriter;
    private readonly BlockWriter _blockWriter;

    public CycleCacheTests()
    {
        _stringWriter = new StringWriter();
        _blockWriter = new BlockWriter(_stringWriter);
    }

    [Fact]
    public void Constructor_InitializesWithName()
    {
        // Act
        var cache = new CycleCache("CYCLE800");

        // Assert
        Assert.Equal("CYCLE800", cache.CycleName);
    }

    [Fact]
    public void WriteIfDifferent_FirstCall_WritesFullDefinition()
    {
        // Arrange
        var cache = new CycleCache("CYCLE81");
        var parameters = new Dictionary<string, object>
        {
            { "RTP", 10.0 },
            { "RFP", 0.0 },
            { "SDIS", 5.0 },
            { "DP", -20.0 }
        };

        // Act
        var result = cache.WriteIfDifferent(_blockWriter, parameters);

        // Assert
        Assert.True(result); // Full definition written
        var output = _stringWriter.ToString();
        Assert.Contains("CYCLE81", output);
        Assert.Contains("RTP=", output);
    }

    [Fact]
    public void WriteIfDifferent_SameParameters_WritesCallOnly()
    {
        // Arrange
        var cache = new CycleCache("CYCLE81");
        var parameters = new Dictionary<string, object>
        {
            { "RTP", 10.0 },
            { "DP", -20.0 }
        };

        // Act - First call
        cache.WriteIfDifferent(_blockWriter, parameters);

        // Clear output
        _stringWriter.GetStringBuilder().Clear();

        // Second call with same parameters
        var result = cache.WriteIfDifferent(_blockWriter, parameters);

        // Assert
        Assert.False(result); // Call only written
        var output = _stringWriter.ToString();
        Assert.Contains("CYCLE81()", output);
    }

    [Fact]
    public void WriteIfDifferent_DifferentParameters_WritesFullDefinition()
    {
        // Arrange
        var cache = new CycleCache("CYCLE81");
        var parameters1 = new Dictionary<string, object>
        {
            { "RTP", 10.0 },
            { "DP", -20.0 }
        };
        var parameters2 = new Dictionary<string, object>
        {
            { "RTP", 15.0 }, // Different RTP
            { "DP", -20.0 }
        };

        // Act - First call
        cache.WriteIfDifferent(_blockWriter, parameters1);

        // Clear output
        _stringWriter.GetStringBuilder().Clear();

        // Second call with different parameters
        var result = cache.WriteIfDifferent(_blockWriter, parameters2);

        // Assert
        Assert.True(result); // Full definition written
        var output = _stringWriter.ToString();
        Assert.Contains("CYCLE81", output);
        Assert.Contains("RTP=15.000", output);
    }

    [Fact]
    public void Reset_ClearsCache()
    {
        // Arrange
        var cache = new CycleCache("CYCLE81");
        var parameters = new Dictionary<string, object>
        {
            { "RTP", 10.0 }
        };

        // Act - First call
        cache.WriteIfDifferent(_blockWriter, parameters);

        // Reset
        cache.Reset();

        // Clear output
        _stringWriter.GetStringBuilder().Clear();

        // Second call with same parameters (should write full definition after reset)
        var result = cache.WriteIfDifferent(_blockWriter, parameters);

        // Assert
        Assert.True(result); // Full definition written after reset
    }

    [Fact]
    public void GetStats_ReturnsCorrectStatistics()
    {
        // Arrange
        var cache = new CycleCache("CYCLE800");
        var parameters = new Dictionary<string, object>
        {
            { "MODE", 1 }
        };

        // Act - Multiple calls
        cache.WriteIfDifferent(_blockWriter, parameters);
        cache.WriteIfDifferent(_blockWriter, parameters);
        cache.WriteIfDifferent(_blockWriter, parameters);
        var stats = cache.GetStats();

        // Assert
        Assert.Equal("CYCLE800", stats["cycle_name"]);
        Assert.Equal(3, stats["call_count"]);
        Assert.Equal(1, stats["full_definition_count"]);
        Assert.True((bool)stats["is_cached"]);
    }

    [Fact]
    public void FormatParams_FloatValues_FormatsWithThreeDecimals()
    {
        // Arrange
        var cache = new CycleCache("CYCLE81");
        var parameters = new Dictionary<string, object>
        {
            { "RTP", 10.5678 },
            { "RFP", 0.1234 }
        };

        // Act
        cache.WriteIfDifferent(_blockWriter, parameters);

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("RTP=10.568", output); // Rounded to 3 decimals
        Assert.Contains("RFP=0.123", output);
    }

    [Fact]
    public void FormatParams_StringValues_WrapsInQuotes()
    {
        // Arrange
        var cache = new CycleCache("CYCLE800");
        var parameters = new Dictionary<string, object>
        {
            { "TABLE", "MY_TABLE" }
        };

        // Act
        cache.WriteIfDifferent(_blockWriter, parameters);

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("TABLE=\"MY_TABLE\"", output);
    }

    [Fact]
    public void FormatParams_IntValues_FormatsAsInteger()
    {
        // Arrange
        var cache = new CycleCache("CYCLE81");
        var parameters = new Dictionary<string, object>
        {
            { "MODE", 1 },
            { "COUNT", 42 }
        };

        // Act
        cache.WriteIfDifferent(_blockWriter, parameters);

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("MODE=1", output);
        Assert.Contains("COUNT=42", output);
    }

    [Fact]
    public void FormatParams_BoolValues_FormatsAsOneOrZero()
    {
        // Arrange
        var cache = new CycleCache("CYCLE800");
        var parameters = new Dictionary<string, object>
        {
            { "ENABLED", true },
            { "DISABLED", false }
        };

        // Act
        cache.WriteIfDifferent(_blockWriter, parameters);

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("ENABLED=1", output);
        Assert.Contains("DISABLED=0", output);
    }

    [Fact]
    public void WriteIfDifferent_EmptyParameters_WritesEmptyCall()
    {
        // Arrange
        var cache = new CycleCache("CYCLE800");
        var parameters = new Dictionary<string, object>();

        // Act
        cache.WriteIfDifferent(_blockWriter, parameters);

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("CYCLE800()", output);
    }

    [Fact]
    public void GetStats_AfterReset_ReturnsCorrectState()
    {
        // Arrange
        var cache = new CycleCache("CYCLE800");
        var parameters = new Dictionary<string, object>
        {
            { "MODE", 1 }
        };

        // Act
        cache.WriteIfDifferent(_blockWriter, parameters);
        cache.Reset();
        var stats = cache.GetStats();

        // Assert
        Assert.Equal("CYCLE800", stats["cycle_name"]);
        Assert.Equal(1, stats["call_count"]);
        Assert.False((bool)stats["is_cached"]);
        Assert.Equal(0, stats["cached_params_count"]);
    }

    [Fact]
    public void CycleCacheHelper_GetOrCreate_CreatesNewCache()
    {
        // Arrange
        var memoryStream = new MemoryStream();
        var streamWriter = new StreamWriter(memoryStream);
        var context = new PostContext(streamWriter);

        // Act
        var cache = CycleCacheHelper.GetOrCreate(context, "CYCLE83");

        // Assert
        Assert.NotNull(cache);
        Assert.Equal("CYCLE83", cache.CycleName);
    }

    [Fact]
    public void CycleCacheHelper_GetOrCreate_ReturnsExistingCache()
    {
        // Arrange
        var memoryStream = new MemoryStream();
        var streamWriter = new StreamWriter(memoryStream);
        var context = new PostContext(streamWriter);
        var cache1 = CycleCacheHelper.GetOrCreate(context, "CYCLE83");

        // Act
        var cache2 = CycleCacheHelper.GetOrCreate(context, "CYCLE83");

        // Assert
        Assert.Same(cache1, cache2);
    }

    [Fact]
    public void CycleCacheHelper_WriteIfDifferent_WritesToContext()
    {
        // Arrange
        var memoryStream = new MemoryStream();
        var streamWriter = new StreamWriter(memoryStream);
        var context = new PostContext(streamWriter);
        var parameters = new Dictionary<string, object>
        {
            { "RTP", 10.0 }
        };

        // Act
        var result = CycleCacheHelper.WriteIfDifferent(context, "CYCLE81", parameters);

        // Assert
        Assert.True(result);
        streamWriter.Flush();
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var output = reader.ReadToEnd();
        Assert.Contains("CYCLE81", output);
    }

    [Fact]
    public void CycleCacheHelper_Reset_ClearsCache()
    {
        // Arrange
        var memoryStream = new MemoryStream();
        var streamWriter = new StreamWriter(memoryStream);
        var context = new PostContext(streamWriter);
        var parameters = new Dictionary<string, object>
        {
            { "RTP", 10.0 }
        };

        // Act - First call
        CycleCacheHelper.WriteIfDifferent(context, "CYCLE81", parameters);

        // Reset
        CycleCacheHelper.Reset(context, "CYCLE81");

        // Clear output
        memoryStream.SetLength(0);

        // Second call with same parameters
        var result = CycleCacheHelper.WriteIfDifferent(context, "CYCLE81", parameters);

        // Assert
        Assert.True(result); // Full definition written after reset
    }
}
