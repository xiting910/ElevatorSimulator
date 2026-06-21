using ElevatorSimulator.Server.Models;
using ElevatorSimulator.Share.Enums;
using System.ComponentModel;

namespace ElevatorSimulator.Server.Test;

/// <summary>
/// <see cref="ElevatorState"/> 的单元测试, 验证属性读写,内部呼叫增删和 <see cref="INotifyPropertyChanged"/> 通知
/// </summary>
public sealed class ElevatorStateTests
{
    /// <summary>
    /// <see cref="ElevatorState.Id"/> 应可正常读写
    /// </summary>
    [Fact]
    public void Id_CanBeSet()
    {
        var state = new ElevatorState { Id = 5 };
        Assert.Equal(5, state.Id);
    }

    /// <summary>
    /// 设相同值时不应触发 <see cref="INotifyPropertyChanged.PropertyChanged"/>
    /// </summary>
    [Fact]
    public void CurrentFloor_SetSameValue_DoesNotFire()
    {
        // Arrange
        var state = new ElevatorState { Id = 0, CurrentFloor = 3 };
        var fired = false;
        state.PropertyChanged += (_, _) => fired = true;

        // Act
        state.CurrentFloor = 3;

        // Assert
        Assert.False(fired);
    }

    /// <summary>
    /// 设不同值时触发 PropertyChanged, 参数为 CurrentFloor
    /// </summary>
    [Fact]
    public void CurrentFloor_SetDifferentValue_FiresPropertyChanged()
    {
        // Arrange
        var state = new ElevatorState { Id = 0, CurrentFloor = 1 };
        PropertyChangedEventArgs? args = null;
        state.PropertyChanged += (_, e) => args = e;

        // Act
        state.CurrentFloor = 2;

        // Assert
        Assert.NotNull(args);
        Assert.Equal(nameof(ElevatorState.CurrentFloor), args!.PropertyName);
        Assert.Equal(2, state.CurrentFloor);
    }

    /// <summary>
    /// 修改 MovingDirection 时触发 PropertyChanged
    /// </summary>
    [Fact]
    public void MovingDirection_SetDifferentValue_FiresPropertyChanged()
    {
        // Arrange
        var state = new ElevatorState { Id = 0 };
        PropertyChangedEventArgs? args = null;
        state.PropertyChanged += (_, e) => args = e;

        // Act
        state.MovingDirection = Direction.Up;

        // Assert
        Assert.NotNull(args);
        Assert.Equal(nameof(ElevatorState.MovingDirection), args!.PropertyName);
    }

    /// <summary>
    /// 修改 Door 时触发 PropertyChanged
    /// </summary>
    [Fact]
    public void Door_SetDifferentValue_FiresPropertyChanged()
    {
        // Arrange
        var state = new ElevatorState { Id = 0 };
        PropertyChangedEventArgs? args = null;
        state.PropertyChanged += (_, e) => args = e;

        // Act
        state.Door = DoorState.Opening;

        // Assert
        Assert.NotNull(args);
        Assert.Equal(nameof(ElevatorState.Door), args!.PropertyName);
    }

    /// <summary>
    /// 修改 DoorOpenRatio 时触发 PropertyChanged
    /// </summary>
    [Fact]
    public void DoorOpenRatio_SetDifferentValue_FiresPropertyChanged()
    {
        // Arrange
        var state = new ElevatorState { Id = 0 };
        PropertyChangedEventArgs? args = null;
        state.PropertyChanged += (_, e) => args = e;

        // Act
        state.DoorOpenRatio = 0.5;

        // Assert
        Assert.NotNull(args);
        Assert.Equal(nameof(ElevatorState.DoorOpenRatio), args!.PropertyName);
    }

    /// <summary>
    /// 添加新楼层的内部呼叫应返回 <see langword="true"/>
    /// </summary>
    [Fact]
    public void AddInternalCall_NewFloor_ReturnsTrue()
    {
        // Arrange
        var state = new ElevatorState { Id = 0 };

        // Act
        var result = state.AddInternalCall(5);

        // Assert
        Assert.True(result);
        Assert.Contains(5, state.InternalCalls);
    }

    /// <summary>
    /// 重复添加同一楼层应返回 <see langword="false"/>
    /// </summary>
    [Fact]
    public void AddInternalCall_DuplicateFloor_ReturnsFalse()
    {
        // Arrange
        var state = new ElevatorState { Id = 0 };
        _ = state.AddInternalCall(5);

        // Act
        var result = state.AddInternalCall(5);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 添加内部呼叫时触发 InternalCalls 的 PropertyChanged
    /// </summary>
    [Fact]
    public void AddInternalCall_FiresPropertyChanged()
    {
        // Arrange
        var state = new ElevatorState { Id = 0 };
        PropertyChangedEventArgs? args = null;
        state.PropertyChanged += (_, e) => args = e;

        // Act
        _ = state.AddInternalCall(3);

        // Assert
        Assert.NotNull(args);
        Assert.Equal(nameof(ElevatorState.InternalCalls), args!.PropertyName);
    }

    /// <summary>
    /// 移除已存在的内部呼叫应返回 <see langword="true"/>
    /// </summary>
    [Fact]
    public void RemoveInternalCall_ExistingFloor_ReturnsTrue()
    {
        // Arrange
        var state = new ElevatorState { Id = 0 };
        _ = state.AddInternalCall(7);

        // Act
        var result = state.RemoveInternalCall(7);

        // Assert
        Assert.True(result);
        Assert.DoesNotContain(7, state.InternalCalls);
    }

    /// <summary>
    /// 移除不存在的内部呼叫应返回 <see langword="false"/>
    /// </summary>
    [Fact]
    public void RemoveInternalCall_NonExistingFloor_ReturnsFalse()
    {
        // Arrange
        var state = new ElevatorState { Id = 0 };

        // Act
        var result = state.RemoveInternalCall(99);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 移除内部呼叫时触发 InternalCalls 的 PropertyChanged
    /// </summary>
    [Fact]
    public void RemoveInternalCall_FiresPropertyChanged()
    {
        // Arrange
        var state = new ElevatorState { Id = 0 };
        _ = state.AddInternalCall(5);
        PropertyChangedEventArgs? args = null;
        state.PropertyChanged += (_, e) => args = e;

        // Act
        _ = state.RemoveInternalCall(5);

        // Assert
        Assert.NotNull(args);
    }

    /// <summary>
    /// 初始状态 InternalCalls 应为空数组
    /// </summary>
    [Fact]
    public void InternalCalls_ReturnsEmpty_Initially()
    {
        var state = new ElevatorState { Id = 0 };
        Assert.Empty(state.InternalCalls);
    }

    /// <summary>
    /// InternalCalls 返回防御性副本, 修改返回数组不影响原始状态
    /// </summary>
    [Fact]
    public void InternalCalls_ReturnsDefensiveCopy()
    {
        // Arrange
        var state = new ElevatorState { Id = 0 };
        _ = state.AddInternalCall(1);

        // Act
        var calls = state.InternalCalls;
        calls[0] = 99; // attempt to modify

        // Assert - original state is unchanged
        Assert.Contains(1, state.InternalCalls);
        Assert.DoesNotContain(99, state.InternalCalls);
    }
}
