using ElevatorSimulator.Client.Core;
using ElevatorSimulator.Share;
using ElevatorSimulator.Share.Enums;
using ElevatorSimulator.Share.Messages;

namespace ElevatorSimulator.Client.Test;

/// <summary>
/// <see cref="ClientState"/> 的单元测试, 验证状态属性读写,电梯进出逻辑,状态更新事件
/// </summary>
public sealed class ClientStateTests
{
    /// <summary>
    /// 被测的客户端状态实例
    /// </summary>
    private readonly ClientState _state = new();

    /// <summary>
    /// <see cref="ClientState.ClientId"/> 应非空
    /// </summary>
    [Fact]
    public void ClientId_IsNotEmpty() => Assert.False(string.IsNullOrEmpty(_state.ClientId));

    /// <summary>
    /// <see cref="ClientState.ClientId"/> 应为 32 字符的 GUID (去连字符)
    /// </summary>
    [Fact]
    public void ClientId_Is32Characters() => Assert.Equal(32, _state.ClientId.Length);

    /// <summary>
    /// 不同 <see cref="ClientState"/> 实例应有不同的 ClientId
    /// </summary>
    [Fact]
    public void ClientId_IsUniquePerInstance()
    {
        var state1 = new ClientState();
        var state2 = new ClientState();
        Assert.NotEqual(state1.ClientId, state2.ClientId);
    }

    /// <summary>
    /// <see cref="ClientState.CurrentFloor"/> 默认值应为 0
    /// </summary>
    [Fact]
    public void CurrentFloor_DefaultIsZero() => Assert.Equal(0, _state.CurrentFloor);

    /// <summary>
    /// <see cref="ClientState.CurrentFloor"/> 应可正常读写
    /// </summary>
    [Fact]
    public void CurrentFloor_CanBeSet()
    {
        // Act
        _state.CurrentFloor = 5;

        // Assert
        Assert.Equal(5, _state.CurrentFloor);
    }

    /// <summary>
    /// <see cref="ClientState.CurrentElevatorId"/> 默认值应为 <see langword="null"/>
    /// </summary>
    [Fact]
    public void CurrentElevatorId_DefaultIsNull() => Assert.Null(_state.CurrentElevatorId);

    /// <summary>
    /// <see cref="ClientState.CurrentElevatorId"/> 应可正常读写
    /// </summary>
    [Fact]
    public void CurrentElevatorId_CanBeSet()
    {
        // Act
        _state.CurrentElevatorId = 1;

        // Assert
        Assert.Equal(1, _state.CurrentElevatorId);
    }

    /// <summary>
    /// <see cref="ClientState.ElevatorStatuses"/> 数组长度应等于 <see cref="Constants.ElevatorCount"/>
    /// </summary>
    [Fact]
    public void ElevatorStatuses_HasCorrectLength() => Assert.Equal(Constants.ElevatorCount, _state.ElevatorStatuses.Length);

    /// <summary>
    /// 电梯 ID 越界时, <see cref="ClientState.CanEnterElevator"/> 应返回 <see langword="false"/>
    /// </summary>
    [Fact]
    public void CanEnterElevator_OutOfRange_ReturnsFalse()
    {
        Assert.False(_state.CanEnterElevator(-1));
        Assert.False(_state.CanEnterElevator(Constants.ElevatorCount));
    }

    /// <summary>
    /// 当前楼层与电梯所在楼层不同时, 应不允许进入
    /// </summary>
    [Fact]
    public void CanEnterElevator_DifferentFloor_ReturnsFalse()
    {
        // Arrange
        _state.CurrentFloor = 5;
        _state.UpdateElevatorStatus(new ElevatorStatusMessage
        {
            Id = 0,
            CurrentFloor = 3,
            Door = DoorState.Open
        });

        // Act
        var result = _state.CanEnterElevator(0);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 同楼层且电梯门打开时, 应允许进入
    /// </summary>
    [Fact]
    public void CanEnterElevator_SameFloorDoorOpen_ReturnsTrue()
    {
        // Arrange
        _state.CurrentFloor = 10;
        _state.UpdateElevatorStatus(new ElevatorStatusMessage
        {
            Id = 1,
            CurrentFloor = 10,
            Door = DoorState.Open
        });

        // Act
        var result = _state.CanEnterElevator(1);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 同楼层但电梯门关闭时, 应不允许进入
    /// </summary>
    [Fact]
    public void CanEnterElevator_SameFloorDoorClosed_ReturnsFalse()
    {
        // Arrange
        _state.CurrentFloor = 10;
        _state.UpdateElevatorStatus(new ElevatorStatusMessage
        {
            Id = 1,
            CurrentFloor = 10,
            Door = DoorState.Closed
        });

        // Act
        var result = _state.CanEnterElevator(1);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 未进入电梯时, <see cref="ClientState.CanExitElevator"/> 应返回 <see langword="false"/>
    /// </summary>
    [Fact]
    public void CanExitElevator_NotInElevator_ReturnsFalse()
    {
        _state.CurrentElevatorId = null;
        Assert.False(_state.CanExitElevator());
    }

    /// <summary>
    /// 在电梯内且门打开时, 应允许离开
    /// </summary>
    [Fact]
    public void CanExitElevator_DoorOpen_ReturnsTrue()
    {
        // Arrange
        _state.CurrentElevatorId = 0;
        _state.UpdateElevatorStatus(new ElevatorStatusMessage
        {
            Id = 0,
            CurrentFloor = 10,
            Door = DoorState.Open
        });

        // Act
        Assert.True(_state.CanExitElevator());
    }

    /// <summary>
    /// 当前楼层存在对应方向的呼叫时, 应返回 <see langword="true"/>
    /// </summary>
    [Fact]
    public void HasActiveCall_WhenCallExists_ReturnsTrue()
    {
        // Arrange
        _state.CurrentFloor = 3;
        _state.UpdateFloorStatus(new FloorStatusMessage
        {
            ActiveCalls = new() { [3] = [Direction.Up, Direction.Down] }
        });

        // Act
        Assert.True(_state.HasActiveCall(Direction.Up));
        Assert.True(_state.HasActiveCall(Direction.Down));
    }

    /// <summary>
    /// 当前楼层不存在对应方向的呼叫时, 应返回 <see langword="false"/>
    /// </summary>
    [Fact]
    public void HasActiveCall_WhenNoCall_ReturnsFalse()
    {
        // Arrange
        _state.CurrentFloor = 3;
        _state.UpdateFloorStatus(new FloorStatusMessage
        {
            ActiveCalls = new() { [5] = [Direction.Up] }
        });

        // Act
        Assert.False(_state.HasActiveCall(Direction.Up));
    }

    /// <summary>
    /// <see cref="ClientState.UpdateElevatorStatus"/> 应触发 <see cref="ClientState.OnElevatorStatusUpdated"/> 事件
    /// </summary>
    [Fact]
    public void UpdateElevatorStatus_FiresEvent()
    {
        // Arrange
        ElevatorStatusMessage? received = null;
        _state.OnElevatorStatusUpdated += msg => received = msg;
        var status = new ElevatorStatusMessage { Id = 0, CurrentFloor = 5 };

        // Act
        _state.UpdateElevatorStatus(status);

        // Assert
        Assert.NotNull(received);
        Assert.Equal(0, received!.Id);
    }

    /// <summary>
    /// <see cref="ClientState.UpdateFloorStatus"/> 应触发 <see cref="ClientState.OnFloorStatusUpdated"/> 事件
    /// </summary>
    [Fact]
    public void UpdateFloorStatus_FiresEvent()
    {
        // Arrange
        FloorStatusMessage? received = null;
        _state.OnFloorStatusUpdated += msg => received = msg;
        var status = new FloorStatusMessage();

        // Act
        _state.UpdateFloorStatus(status);

        // Assert
        Assert.NotNull(received);
    }

    /// <summary>
    /// 电梯 ID 越界时, <see cref="ClientState.UpdateElevatorStatus"/> 不应触发事件
    /// </summary>
    [Fact]
    public void UpdateElevatorStatus_OutOfRange_DoesNotFire()
    {
        // Arrange
        var fired = false;
        _state.OnElevatorStatusUpdated += _ => fired = true;
        var status = new ElevatorStatusMessage { Id = -1 };

        // Act
        _state.UpdateElevatorStatus(status);

        // Assert
        Assert.False(fired);
    }

    /// <summary>
    /// 门在 Opening 状态时不可进入电梯
    /// </summary>
    [Fact]
    public void CanEnterElevator_DoorOpening_ReturnsFalse()
    {
        _state.CurrentFloor = 10;
        _state.UpdateElevatorStatus(new ElevatorStatusMessage
        {
            Id = 0,
            CurrentFloor = 10,
            Door = DoorState.Opening
        });

        Assert.False(_state.CanEnterElevator(0));
    }

    /// <summary>
    /// 门在 Closing 状态时不可进入电梯
    /// </summary>
    [Fact]
    public void CanEnterElevator_DoorClosing_ReturnsFalse()
    {
        _state.CurrentFloor = 10;
        _state.UpdateElevatorStatus(new ElevatorStatusMessage
        {
            Id = 0,
            CurrentFloor = 10,
            Door = DoorState.Closing
        });

        Assert.False(_state.CanEnterElevator(0));
    }

    /// <summary>
    /// 门关闭时不可离开电梯
    /// </summary>
    [Fact]
    public void CanExitElevator_DoorClosed_ReturnsFalse()
    {
        _state.CurrentElevatorId = 0;
        _state.UpdateElevatorStatus(new ElevatorStatusMessage
        {
            Id = 0,
            CurrentFloor = 10,
            Door = DoorState.Closed
        });

        Assert.False(_state.CanExitElevator());
    }

    /// <summary>
    /// 门在 Opening 状态时不可离开电梯
    /// </summary>
    [Fact]
    public void CanExitElevator_DoorOpening_ReturnsFalse()
    {
        _state.CurrentElevatorId = 0;
        _state.UpdateElevatorStatus(new ElevatorStatusMessage
        {
            Id = 0,
            CurrentFloor = 10,
            Door = DoorState.Opening
        });

        Assert.False(_state.CanExitElevator());
    }

    /// <summary>
    /// 电梯 ID 超过数组上限时不应触发事件
    /// </summary>
    [Fact]
    public void UpdateElevatorStatus_IdTooHigh_DoesNotFireEvent()
    {
        var fired = false;
        _state.OnElevatorStatusUpdated += _ => fired = true;
        var status = new ElevatorStatusMessage { Id = Constants.ElevatorCount };

        _state.UpdateElevatorStatus(status);

        Assert.False(fired);
    }
}
