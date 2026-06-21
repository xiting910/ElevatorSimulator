using ElevatorSimulator.Share.Enums;

namespace ElevatorSimulator.Share.Test;

/// <summary>
/// <see cref="DirectionExtensions.ToSymbol"/> 的单元测试, 验证各方向到显示符号的映射
/// </summary>
public sealed class DirectionExtensionsTests
{
    /// <summary>
    /// <see cref="Direction.Up"/> 应映射到 "↑"
    /// </summary>
    [Fact]
    public void ToSymbol_Up_ReturnsUpArrow()
    {
        var result = Direction.Up.ToSymbol();
        Assert.Equal("↑", result);
    }

    /// <summary>
    /// <see cref="Direction.Down"/> 应映射到 "↓"
    /// </summary>
    [Fact]
    public void ToSymbol_Down_ReturnsDownArrow()
    {
        var result = Direction.Down.ToSymbol();
        Assert.Equal("↓", result);
    }

    /// <summary>
    /// <see cref="Direction.None"/> 应映射到 "—"
    /// </summary>
    [Fact]
    public void ToSymbol_None_ReturnsDash()
    {
        var result = Direction.None.ToSymbol();
        Assert.Equal("—", result);
    }
}
