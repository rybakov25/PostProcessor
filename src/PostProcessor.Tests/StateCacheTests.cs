using PostProcessor.Core.Context;

namespace PostProcessor.Tests;

/// <summary>
/// Tests for StateCache functionality
/// </summary>
public class StateCacheTests
{
    private readonly StateCache _cache;

    public StateCacheTests()
    {
        _cache = new StateCache();
    }

    [Fact]
    public void Constructor_InitializesEmpty()
    {
        // Assert
        Assert.Equal(0, _cache.Count);
    }

    [Fact]
    public void HasChanged_FirstCall_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(_cache.HasChanged("LAST_FEED", 100.0));
    }

    [Fact]
    public void HasChanged_SameValue_ReturnsFalse()
    {
        // Arrange
        _cache.Update("LAST_FEED", 100.0);

        // Act & Assert
        Assert.False(_cache.HasChanged("LAST_FEED", 100.0));
    }

    [Fact]
    public void HasChanged_DifferentValue_ReturnsTrue()
    {
        // Arrange
        _cache.Update("LAST_FEED", 100.0);

        // Act & Assert
        Assert.True(_cache.HasChanged("LAST_FEED", 200.0));
    }

    [Fact]
    public void Update_StoresValue()
    {
        // Arrange
        _cache.Update("LAST_TOOL", 5);

        // Act
        var value = _cache.Get<int>("LAST_TOOL", 0);

        // Assert
        Assert.Equal(5, value);
    }

    [Fact]
    public void Get_NotExists_ReturnsDefaultValue()
    {
        // Act
        var value = _cache.Get("NON_EXISTENT", 42);

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void GetOrSet_NotExists_CreatesValue()
    {
        // Act
        var value = _cache.GetOrSet("NEW_KEY", 100);

        // Assert
        Assert.Equal(100, value);
        Assert.True(_cache.Contains("NEW_KEY"));
    }

    [Fact]
    public void GetOrSet_Exists_ReturnsExistingValue()
    {
        // Arrange
        _cache.Update("EXISTING_KEY", 50);

        // Act
        var value = _cache.GetOrSet("EXISTING_KEY", 100);

        // Assert
        Assert.Equal(50, value);
    }

    [Fact]
    public void Remove_RemovesKey()
    {
        // Arrange
        _cache.Update("TO_REMOVE", 123);
        Assert.True(_cache.Contains("TO_REMOVE"));

        // Act
        _cache.Remove("TO_REMOVE");

        // Assert
        Assert.False(_cache.Contains("TO_REMOVE"));
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        // Arrange
        _cache.Update("KEY1", 1);
        _cache.Update("KEY2", 2);
        _cache.Update("KEY3", 3);

        // Act
        _cache.Clear();

        // Assert
        Assert.Equal(0, _cache.Count);
    }

    [Fact]
    public void Keys_ReturnsAllKeys()
    {
        // Arrange
        _cache.Update("KEY1", 1);
        _cache.Update("KEY2", 2);

        // Act
        var keys = _cache.Keys.ToList();

        // Assert
        Assert.Equal(2, keys.Count);
        Assert.Contains("KEY1", keys);
        Assert.Contains("KEY2", keys);
    }

    [Fact]
    public void SetInitial_AddsValue()
    {
        // Act
        _cache.SetInitial("INIT_KEY", 999);

        // Assert
        Assert.Equal(999, _cache.Get("INIT_KEY", 0));
    }

    [Fact]
    public void HasChanged_DifferentTypes_ReturnsTrue()
    {
        // Arrange
        _cache.Update("MIXED_TYPE", "string_value");

        // Act & Assert
        Assert.True(_cache.HasChanged("MIXED_TYPE", 123));
    }

    [Fact]
    public void Update_DoubleValues_Precision()
    {
        // Arrange
        double value = 123.456789;
        _cache.Update("DOUBLE", value);

        // Act
        var retrieved = _cache.Get<double>("DOUBLE", 0.0);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public void Update_StringValues()
    {
        // Arrange
        _cache.Update("STRING", "test_value");

        // Act
        var retrieved = _cache.Get<string>("STRING", "");

        // Assert
        Assert.Equal("test_value", retrieved);
    }

    [Fact]
    public void Update_BoolValues()
    {
        // Arrange
        _cache.Update("BOOL", true);

        // Act
        var retrieved = _cache.Get<bool>("BOOL", false);

        // Assert
        Assert.True(retrieved);
    }

    [Fact]
    public void Contains_ExistingKey_ReturnsTrue()
    {
        // Arrange
        _cache.Update("EXISTS", 1);

        // Act & Assert
        Assert.True(_cache.Contains("EXISTS"));
    }

    [Fact]
    public void Contains_MissingKey_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_cache.Contains("MISSING"));
    }
}
