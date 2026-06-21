using ElevatorSimulator.Server.Models;
using ElevatorSimulator.Share.Enums;
using System.ComponentModel;

namespace ElevatorSimulator.Server.Test;

/// <summary>
/// <see cref="FloorCallState"/> 的单元测试, 验证楼层呼叫增删,防御性副本和 <see cref="INotifyPropertyChanged"/> 通知
/// </summary>
public sealed class FloorCallStateTests
{
    /// <summary>
    /// 添加新呼叫应返回 <see langword="true"/> 并出现在 ActiveCalls 中
    /// </summary>
    [Fact]
    public void AddFloorCall_NewCall_ReturnsTrue()
    {
        // Arrange
        var state = new FloorCallState();

        // Act
        var result = state.AddFloorCall(3, Direction.Up);

        // Assert
        Assert.True(result);
        Assert.Contains(Direction.Up, state.ActiveCalls[3]);
    }

    /// <summary>
    /// 重复添加相同呼叫应返回 <see langword="false"/>
    /// </summary>
    [Fact]
    public void AddFloorCall_DuplicateCall_ReturnsFalse()
    {
        // Arrange
        var state = new FloorCallState();
        _ = state.AddFloorCall(3, Direction.Up);

        // Act
        var result = state.AddFloorCall(3, Direction.Up);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 添加呼叫时应触发 ActiveCalls 的 PropertyChanged
    /// </summary>
    [Fact]
    public void AddFloorCall_FiresPropertyChanged()
    {
        // Arrange
        var state = new FloorCallState();
        PropertyChangedEventArgs? args = null;
        state.PropertyChanged += (_, e) => args = e;

        // Act
        _ = state.AddFloorCall(5, Direction.Down);

        // Assert
        Assert.NotNull(args);
        Assert.Equal(nameof(FloorCallState.ActiveCalls), args!.PropertyName);
    }

    /// <summary>
    /// 移除已存在的呼叫应返回 <see langword="true"/>
    /// </summary>
    [Fact]
    public void RemoveFloorCall_ExistingCall_ReturnsTrue()
    {
        // Arrange
        var state = new FloorCallState();
        _ = state.AddFloorCall(7, Direction.Down);

        // Act
        var result = state.RemoveFloorCall(7, Direction.Down);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 移除不存在的呼叫应返回 <see langword="false"/>
    /// </summary>
    [Fact]
    public void RemoveFloorCall_NonExisting_ReturnsFalse()
    {
        // Arrange
        var state = new FloorCallState();

        // Act
        var result = state.RemoveFloorCall(1, Direction.Up);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 移除楼层最后的方向时, 该楼层应从 ActiveCalls 中移除
    /// </summary>
    [Fact]
    public void RemoveFloorCall_LastDirection_RemovesFloor()
    {
        // Arrange
        var state = new FloorCallState();
        _ = state.AddFloorCall(3, Direction.Up);

        // Act
        _ = state.RemoveFloorCall(3, Direction.Up);

        // Assert - floor 3 should no longer have any active calls
        Assert.False(state.ActiveCalls.ContainsKey(3));
    }

    /// <summary>
    /// 移除楼层其中一个方向时, 该楼层应保留其他方向
    /// </summary>
    [Fact]
    public void RemoveFloorCall_NotLastDirection_KeepsFloor()
    {
        // Arrange
        var state = new FloorCallState();
        _ = state.AddFloorCall(5, Direction.Up);
        _ = state.AddFloorCall(5, Direction.Down);

        // Act
        _ = state.RemoveFloorCall(5, Direction.Up);

        // Assert - floor 5 still has Down
        Assert.True(state.ActiveCalls.ContainsKey(5));
        Assert.Contains(Direction.Down, state.ActiveCalls[5]);
        Assert.DoesNotContain(Direction.Up, state.ActiveCalls[5]);
    }

    /// <summary>
    /// ActiveCalls 返回防御性副本, 修改返回字典不影响原始状态
    /// </summary>
    [Fact]
    public void ActiveCalls_ReturnsDefensiveCopy()
    {
        // Arrange
        var state = new FloorCallState();
        _ = state.AddFloorCall(1, Direction.Up);

        // Act
        var copy = state.ActiveCalls;
        copy[99] = [Direction.Down]; // modify copy

        // Assert
        Assert.False(state.ActiveCalls.ContainsKey(99));
    }

    /// <summary>
    /// 初始状态 ActiveCalls 应为空
    /// </summary>
    [Fact]
    public void ActiveCalls_InitiallyEmpty()
    {
        var state = new FloorCallState();
        Assert.Empty(state.ActiveCalls);
    }

    /// <summary>
    /// 移除呼叫时应触发 ActiveCalls 的 PropertyChanged
    /// </summary>
    [Fact]
    public void RemoveFloorCall_FiresPropertyChanged()
    {
        // Arrange
        var state = new FloorCallState();
        _ = state.AddFloorCall(2, Direction.Up);
        PropertyChangedEventArgs? args = null;
        state.PropertyChanged += (_, e) => args = e;

        // Act
        _ = state.RemoveFloorCall(2, Direction.Up);

        // Assert
        Assert.NotNull(args);
    }
}
