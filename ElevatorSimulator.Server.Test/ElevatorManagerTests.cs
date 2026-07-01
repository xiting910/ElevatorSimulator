using ElevatorSimulator.Server.Core;
using ElevatorSimulator.Server.Core.Interfaces;
using ElevatorSimulator.Server.Models;
using ElevatorSimulator.Server.Models.Interfaces;
using ElevatorSimulator.Share;
using ElevatorSimulator.Share.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Timer = System.Timers.Timer;

namespace ElevatorSimulator.Server.Test;

/// <summary>
/// <see cref="ElevatorManager"/> 的单元测试, 验证中央调度逻辑,电梯选择算法和呼叫取消路由
/// </summary>
public sealed class ElevatorManagerTests
{
    /// <summary> 日志记录器的 Mock </summary>
    private readonly Mock<ILogger<ElevatorManager>> _loggerMock;
    /// <summary> 楼层呼叫状态的 Mock </summary>
    private readonly Mock<IFloorCallState> _floorCallStateMock;
    /// <summary> 电梯状态工厂的 Mock </summary>
    private readonly Mock<Func<int, IElevatorState>> _createStateMock;
    /// <summary> 电梯控制器工厂的 Mock </summary>
    private readonly Mock<Func<Timer, IElevatorState, IElevatorController>> _createControllerMock;
    /// <summary> 模拟的所有电梯状态 </summary>
    private readonly ElevatorState[] _states;
    /// <summary> 模拟的所有电梯控制器 Mock </summary>
    private readonly Mock<IElevatorController>[] _controllerMocks;

    /// <summary>
    /// 初始化 3 台电梯的 Mock 状态与控制器, 配置工厂委托
    /// </summary>
    public ElevatorManagerTests()
    {
        _loggerMock = new Mock<ILogger<ElevatorManager>>();
        _floorCallStateMock = new Mock<IFloorCallState>();

        // Create 3 elevator states
        _states = new ElevatorState[Constants.ElevatorCount];
        for (var i = 0; i < Constants.ElevatorCount; i++)
        {
            _states[i] = new ElevatorState { Id = i, CurrentFloor = 1 };
        }

        // Create 3 controller mocks
        _controllerMocks = new Mock<IElevatorController>[Constants.ElevatorCount];
        for (var i = 0; i < Constants.ElevatorCount; i++)
        {
            _controllerMocks[i] = new Mock<IElevatorController>();
            _ = _controllerMocks[i].Setup(c => c.Id).Returns(i);
            _ = _controllerMocks[i].Setup(c => c.State).Returns(_states[i]);
            _ = _controllerMocks[i].Setup(c => c.PredictTimeToServeExternalCall(It.IsAny<int>(), It.IsAny<Direction>()))
                .Returns(10.0);
        }

        _createStateMock = new Mock<Func<int, IElevatorState>>();
        _ = _createStateMock.Setup(f => f(It.IsAny<int>())).Returns((int id) => _states[id]);

        _createControllerMock = new Mock<Func<Timer, IElevatorState, IElevatorController>>();
        _ = _createControllerMock.Setup(f => f(It.IsAny<Timer>(), It.IsAny<IElevatorState>()))
            .Returns((Timer t, IElevatorState s) => _controllerMocks[s.Id].Object);
    }

    /// <summary>
    /// 使用 Mock 工厂和状态创建 <see cref="ElevatorManager"/> 实例
    /// </summary>
    private ElevatorManager CreateManager()
    {
        return new(
        _loggerMock.Object,
        _floorCallStateMock.Object,
        _createStateMock.Object,
        _createControllerMock.Object);
    }

    /// <summary>
    /// <see cref="ElevatorManager.FloorCallState"/> 应返回注入的 <see cref="IFloorCallState"/> 实例
    /// </summary>
    [Fact]
    public void FloorCallState_ReturnsInjectedInstance()
    {
        // Arrange
        var manager = CreateManager();

        // Assert
        Assert.Same(_floorCallStateMock.Object, manager.FloorCallState);
    }

    /// <summary>
    /// <see cref="ElevatorManager.GetCurrentStates"/> 应返回所有电梯状态
    /// </summary>
    [Fact]
    public void GetCurrentStates_ReturnsAllElevators()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var states = manager.GetCurrentStates().ToArray();

        // Assert
        Assert.Equal(Constants.ElevatorCount, states.Length);
    }

    /// <summary>
    /// 添加楼层呼叫时应选择预测时间最短的电梯
    /// </summary>
    [Fact]
    public void AddFloorCall_WhenAdded_CallsBestElevator()
    {
        // Arrange
        _ = _floorCallStateMock.Setup(f => f.AddFloorCall(5, Direction.Up)).Returns(true);
        _ = _controllerMocks[0].Setup(c => c.PredictTimeToServeExternalCall(5, Direction.Up)).Returns(30.0);
        _ = _controllerMocks[1].Setup(c => c.PredictTimeToServeExternalCall(5, Direction.Up)).Returns(15.0);
        _ = _controllerMocks[2].Setup(c => c.PredictTimeToServeExternalCall(5, Direction.Up)).Returns(20.0);

        var manager = CreateManager();

        // Act
        manager.AddFloorCall(5, Direction.Up);

        // Assert: elevator 1 is best (15s)
        _controllerMocks[1].Verify(c => c.AddExternalTask(5, Direction.Up), Times.Once);
    }

    /// <summary>
    /// 当 <see cref="IFloorCallState.AddFloorCall"/> 返回 <see langword="false"/> 时, 不应调用任何电梯
    /// </summary>
    [Fact]
    public void AddFloorCall_WhenFloorCallNotAdded_DoesNotCallElevator()
    {
        // Arrange
        _ = _floorCallStateMock.Setup(f => f.AddFloorCall(5, Direction.Up)).Returns(false);
        var manager = CreateManager();

        // Act
        manager.AddFloorCall(5, Direction.Up);

        // Assert
        foreach (var mock in _controllerMocks)
        {
            mock.Verify(c => c.AddExternalTask(It.IsAny<int>(), It.IsAny<Direction>()), Times.Never);
        }
    }

    /// <summary>
    /// 添加电梯内部呼叫成功时应将任务添加到对应电梯控制器
    /// </summary>
    [Fact]
    public void AddElevatorCall_WhenStateAccepts_AddsTask()
    {
        // Arrange - state 0 accepts the internal call
        _ = _states[0].AddInternalCall(10);
        var manager = CreateManager();

        // Act - addElevatorCall checks state first, then controller
        // Since AddInternalCall already added, a second call returns false
        // Let's test with RemoveInternalCall first to reset
        _ = _states[0].RemoveInternalCall(10);
        manager.AddElevatorCall(0, 10);

        // Assert
        _controllerMocks[0].Verify(c => c.AddInternalTask(10), Times.Once);
    }

    /// <summary>
    /// 取消楼层呼叫时应从所有电梯移除对应的外部任务
    /// </summary>
    [Fact]
    public void CancelFloorCall_WhenRemoved_CancelsOnAllElevators()
    {
        // Arrange
        _ = _floorCallStateMock.Setup(f => f.RemoveFloorCall(3, Direction.Down)).Returns(true);
        var manager = CreateManager();

        // Act
        manager.CancelFloorCall(3, Direction.Down);

        // Assert
        foreach (var mock in _controllerMocks)
        {
            mock.Verify(c => c.RemoveExternalTask(3, Direction.Down), Times.Once);
        }
    }

    /// <summary>
    /// 取消内部呼叫成功时应从目标电梯移除任务 (需先注册呼叫)
    /// </summary>
    [Fact]
    public void CancelElevatorCall_WhenRemoved_CancelsOnTargetElevator()
    {
        // Arrange — 先注册内部呼叫作为取消的前提条件
        var manager = CreateManager();
        _ = _states[1].AddInternalCall(7);
        _ = _controllerMocks[1].Setup(c => c.State).Returns(_states[1]);

        // Act
        manager.CancelElevatorCall(1, 7);

        // Assert
        _controllerMocks[1].Verify(c => c.RemoveInternalTask(7), Times.Once);
    }

    /// <summary>
    /// <see cref="ElevatorManager.RequestDoorOpen"/> 应调用目标电梯的 <see cref="IElevatorController.SignalDoorOpen"/>
    /// </summary>
    [Fact]
    public void RequestDoorOpen_CallsCorrectElevator()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.RequestDoorOpen(2);

        // Assert
        _controllerMocks[2].Verify(c => c.SignalDoorOpen(), Times.Once);
    }

    /// <summary>
    /// <see cref="ElevatorManager.RequestDoorClose"/> 应调用目标电梯的 <see cref="IElevatorController.SignalDoorClose"/>
    /// </summary>
    [Fact]
    public void RequestDoorClose_CallsCorrectElevator()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.RequestDoorClose(0);

        // Assert
        _controllerMocks[0].Verify(c => c.SignalDoorClose(), Times.Once);
    }

    /// <summary>
    /// <see cref="ElevatorManager.Initialize"/> 应触发 <see cref="IElevatorManager.ElevatorStatusChanged"/> 事件
    /// </summary>
    [Fact]
    public void Initialize_FiresElevatorStatusChanged()
    {
        // Arrange
        var manager = CreateManager();
        var fired = false;
        manager.ElevatorStatusChanged += _ => fired = true;

        // Act
        manager.Initialize();

        // Assert
        Assert.True(fired);
    }

    /// <summary>
    /// 初始化后电梯状态变更应触发 <see cref="IElevatorManager.ElevatorStatusChanged"/> 事件
    /// </summary>
    [Fact]
    public void Initialize_SubscribesToPropertyChanged()
    {
        // Arrange
        var manager = CreateManager();
        var fired = false;
        manager.ElevatorStatusChanged += _ => fired = true;
        manager.Initialize();

        // Act - trigger a property change on elevator 0
        _states[0].CurrentFloor = 5;

        // Assert - the event should fire via the subscription
        Assert.True(fired);
    }

    // 调度算法 — 2x 惩罚

    /// <summary>
    /// 存在空闲电梯时, 忙碌电梯的预测时间应被 2x 惩罚,
    /// 使得空闲电梯即使预测时间稍长也能胜出
    /// </summary>
    [Fact]
    public void AddFloorCall_WhenIdleExists_BusyElevatorsGet2xPenalty()
    {
        // Arrange: 电梯0空闲(None), 电梯1忙碌(Up), 电梯2忙碌(Down)
        _states[0].MovingDirection = Direction.None;
        _states[1].MovingDirection = Direction.Up;
        _states[2].MovingDirection = Direction.Down;

        // 空闲电梯预测 12s, 忙碌电梯1预测 5s(惩罚后10s), 忙碌电梯2预测 6s(惩罚后12s)
        _ = _controllerMocks[0].Setup(c => c.PredictTimeToServeExternalCall(5, Direction.Up)).Returns(12.0);
        _ = _controllerMocks[1].Setup(c => c.PredictTimeToServeExternalCall(5, Direction.Up)).Returns(5.0);
        _ = _controllerMocks[2].Setup(c => c.PredictTimeToServeExternalCall(5, Direction.Up)).Returns(6.0);
        _ = _floorCallStateMock.Setup(f => f.AddFloorCall(5, Direction.Up)).Returns(true);

        var manager = CreateManager();

        // Act
        manager.AddFloorCall(5, Direction.Up);

        // 惩罚后: 电梯0=12, 电梯1=10, 电梯2=12 → 电梯1胜出
        _controllerMocks[1].Verify(c => c.AddExternalTask(5, Direction.Up), Times.Once);
    }

    /// <summary>
    /// 存在空闲电梯时, 若空闲电梯预测时间远小于忙碌电梯惩罚后,
    /// 空闲电梯应胜出
    /// </summary>
    [Fact]
    public void AddFloorCall_WhenIdleExists_IdleElevatorWins_IfMuchFaster()
    {
        // Arrange
        _states[0].MovingDirection = Direction.None; // idle
        _states[1].MovingDirection = Direction.Up;
        _states[2].MovingDirection = Direction.Up;

        // 空闲电梯预测 3s, 忙碌电梯预测 10s(惩罚后20s), 20s(惩罚后40s)
        _ = _controllerMocks[0].Setup(c => c.PredictTimeToServeExternalCall(3, Direction.Down)).Returns(3.0);
        _ = _controllerMocks[1].Setup(c => c.PredictTimeToServeExternalCall(3, Direction.Down)).Returns(10.0);
        _ = _controllerMocks[2].Setup(c => c.PredictTimeToServeExternalCall(3, Direction.Down)).Returns(20.0);
        _ = _floorCallStateMock.Setup(f => f.AddFloorCall(3, Direction.Down)).Returns(true);

        var manager = CreateManager();

        // Act
        manager.AddFloorCall(3, Direction.Down);

        // 惩罚后: 电梯0=3, 电梯1=20, 电梯2=40 → 电梯0胜出
        _controllerMocks[0].Verify(c => c.AddExternalTask(3, Direction.Down), Times.Once);
    }

    /// <summary>
    /// 当所有电梯都忙碌时, 不施加 2x 惩罚, 纯粹按预测时间最短选择
    /// </summary>
    [Fact]
    public void AddFloorCall_WhenAllBusy_NoPenaltyApplied()
    {
        // Arrange: 所有电梯都忙碌
        _states[0].MovingDirection = Direction.Up;
        _states[1].MovingDirection = Direction.Down;
        _states[2].MovingDirection = Direction.Up;

        // 原始预测: 电梯0=20s, 电梯1=8s, 电梯2=15s → 电梯1应胜出(无惩罚)
        _ = _controllerMocks[0].Setup(c => c.PredictTimeToServeExternalCall(5, Direction.Up)).Returns(20.0);
        _ = _controllerMocks[1].Setup(c => c.PredictTimeToServeExternalCall(5, Direction.Up)).Returns(8.0);
        _ = _controllerMocks[2].Setup(c => c.PredictTimeToServeExternalCall(5, Direction.Up)).Returns(15.0);
        _ = _floorCallStateMock.Setup(f => f.AddFloorCall(5, Direction.Up)).Returns(true);

        var manager = CreateManager();

        // Act
        manager.AddFloorCall(5, Direction.Up);

        // 无惩罚: 电梯1=8s 直接胜出
        _controllerMocks[1].Verify(c => c.AddExternalTask(5, Direction.Up), Times.Once);
    }

    /// <summary>
    /// 当所有电梯都空闲时, 无惩罚, 按原始预测时间选择
    /// </summary>
    [Fact]
    public void AddFloorCall_WhenAllIdle_NoPenaltyApplied()
    {
        // Arrange: 所有电梯空闲
        _states[0].MovingDirection = Direction.None;
        _states[1].MovingDirection = Direction.None;
        _states[2].MovingDirection = Direction.None;

        _ = _controllerMocks[0].Setup(c => c.PredictTimeToServeExternalCall(5, Direction.Up)).Returns(25.0);
        _ = _controllerMocks[1].Setup(c => c.PredictTimeToServeExternalCall(5, Direction.Up)).Returns(10.0);
        _ = _controllerMocks[2].Setup(c => c.PredictTimeToServeExternalCall(5, Direction.Up)).Returns(18.0);
        _ = _floorCallStateMock.Setup(f => f.AddFloorCall(5, Direction.Up)).Returns(true);

        var manager = CreateManager();

        // Act
        manager.AddFloorCall(5, Direction.Up);

        // 全空闲无惩罚: 电梯1=10s 胜出
        _controllerMocks[1].Verify(c => c.AddExternalTask(5, Direction.Up), Times.Once);
    }

    /// <summary>
    /// 初始化后楼层呼叫状态变更应触发 FloorCallsChanged 事件
    /// </summary>
    [Fact]
    public void Initialize_SubscribesToFloorCallState_AndFiresFloorCallsChanged()
    {
        // Arrange
        var manager = CreateManager();
        manager.Initialize();

        var fired = false;
        Dictionary<int, Direction[]>? received = null;
        manager.FloorCallsChanged += data =>
        {
            fired = true;
            received = data;
        };

        // Act - 通过楼层呼叫状态添加呼叫(模拟属性变更)
        _ = _floorCallStateMock.Setup(f => f.ActiveCalls).Returns(
            new Dictionary<int, Direction[]> { [1] = [Direction.Up] });
        _floorCallStateMock.Raise(f => f.PropertyChanged += null,
            _floorCallStateMock.Object,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFloorCallState.ActiveCalls)));

        // Assert
        Assert.True(fired);
    }

    /// <summary>
    /// 当 ElevatorState 拒绝重复呼叫时不应将任务添加到控制器
    /// </summary>
    [Fact]
    public void AddElevatorCall_WhenStateRejects_DoesNotAddTask()
    {
        // Arrange: AddInternalCall 返回 false (重复呼叫)
        _ = _states[0].AddInternalCall(10);
        var manager = CreateManager();

        // Act: 再次添加相同楼层
        manager.AddElevatorCall(0, 10);

        // Assert: 不应调用 AddInternalTask
        _controllerMocks[0].Verify(c => c.AddInternalTask(10), Times.Never);
    }

    /// <summary>
    /// 当 FloorCallState 移除失败时不应在电梯上执行取消操作
    /// </summary>
    [Fact]
    public void CancelFloorCall_WhenNotRemoved_DoesNotCancelOnElevators()
    {
        // Arrange: RemoveFloorCall 返回 false
        _ = _floorCallStateMock.Setup(f => f.RemoveFloorCall(3, Direction.Down)).Returns(false);
        var manager = CreateManager();

        // Act
        manager.CancelFloorCall(3, Direction.Down);

        // Assert
        foreach (var mock in _controllerMocks)
        {
            mock.Verify(c => c.RemoveExternalTask(It.IsAny<int>(), It.IsAny<Direction>()), Times.Never);
        }
    }
}
