using ElevatorSimulator.Server.Core.Controllers;
using ElevatorSimulator.Server.Models;
using ElevatorSimulator.Server.Models.Interfaces;
using ElevatorSimulator.Share;
using ElevatorSimulator.Share.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Timer = System.Timers.Timer;

namespace ElevatorSimulator.Server.Test;

/// <summary>
/// <see cref="ElevatorController"/> 的深度单元测试, 覆盖任务管理, 门控信号, 状态机转换和预测算法
/// </summary>
public sealed class ElevatorControllerTests
{
    /// <summary> 楼层呼叫状态的 Mock </summary>
    private readonly Mock<IFloorCallState> _floorCallStateMock;
    /// <summary> 日志记录器的 Mock </summary>
    private readonly Mock<ILogger<ElevatorController>> _loggerMock;
    /// <summary> 驱动电梯更新的定时器 </summary>
    private readonly Timer _timer;

    /// <summary>
    /// 初始化 Mock 与定时器
    /// </summary>
    public ElevatorControllerTests()
    {
        _floorCallStateMock = new Mock<IFloorCallState>();
        _loggerMock = new Mock<ILogger<ElevatorController>>();
        _timer = new Timer(50);
    }

    /// <summary>
    /// 使用指定电梯状态和共享 Mock 依赖创建 <see cref="ElevatorController"/> 实例
    /// </summary>
    /// <param name="state">电梯逻辑状态</param>
    private ElevatorController CreateController(IElevatorState state) =>
        new(_timer, state, _floorCallStateMock.Object, _loggerMock.Object);

    /// <summary>
    /// <see cref="ElevatorController.Id"/> 应返回注入的 <see cref="IElevatorState.Id"/>
    /// </summary>
    [Fact]
    public void Constructor_Id_ReturnsStateId()
    {
        var state = new ElevatorState { Id = 3 };
        var controller = CreateController(state);
        Assert.Equal(3, controller.Id);
    }

    /// <summary>
    /// <see cref="ElevatorController.State"/> 应返回构造函数注入的状态对象
    /// </summary>
    [Fact]
    public void Constructor_State_ReturnsInjectedState()
    {
        var state = new ElevatorState { Id = 1 };
        var controller = CreateController(state);
        Assert.Same(state, controller.State);
    }

    /// <summary>
    /// 添加上方内部任务后方向应设为 Up
    /// </summary>
    [Fact]
    public void AddInternalTask_NewFloor_UpdatesTarget()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 5 };
        var controller = CreateController(state);

        controller.AddInternalTask(10);

        Assert.Equal(Direction.Up, GetCurrentDirection(controller));
    }

    /// <summary>
    /// 添加下方内部任务后方向应设为 Down
    /// </summary>
    [Fact]
    public void AddInternalTask_BelowCurrentFloor_SetsDirectionDown()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 20 };
        var controller = CreateController(state);

        controller.AddInternalTask(5);

        Assert.Equal(Direction.Down, GetCurrentDirection(controller));
    }

    /// <summary>
    /// 重复添加相同楼层的内部任务不应改变方向
    /// </summary>
    [Fact]
    public void AddInternalTask_SameFloorTwice_SecondIsNoOp()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 3 };
        var controller = CreateController(state);

        controller.AddInternalTask(10);
        var dirAfterFirst = GetCurrentDirection(controller);
        controller.AddInternalTask(10);
        var dirAfterSecond = GetCurrentDirection(controller);

        Assert.Equal(dirAfterFirst, dirAfterSecond);
    }

    /// <summary>
    /// 添加上行外部任务后方向应设为 Up
    /// </summary>
    [Fact]
    public void AddExternalTask_Up_UpdatesTarget()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 0 };
        var controller = CreateController(state);

        controller.AddExternalTask(7, Direction.Up);

        Assert.Equal(Direction.Up, GetCurrentDirection(controller));
    }

    /// <summary>
    /// 添加下行外部任务后方向应设为 Down
    /// </summary>
    [Fact]
    public void AddExternalTask_Down_UpdatesTarget()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 10 };
        var controller = CreateController(state);

        controller.AddExternalTask(3, Direction.Down);

        Assert.Equal(Direction.Down, GetCurrentDirection(controller));
    }

    /// <summary>
    /// 上行外部任务在当前楼层下方时方向应设为 Down
    /// </summary>
    [Fact]
    public void AddExternalTask_Up_BelowCurrentFloor_HeadsDown()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 10 };
        var controller = CreateController(state);

        controller.AddExternalTask(3, Direction.Up);

        Assert.Equal(Direction.Down, GetCurrentDirection(controller));
    }

    /// <summary>
    /// 移除内部任务后若所有任务为空则方向应变为 None
    /// </summary>
    [Fact]
    public void RemoveInternalTask_ExistingTask_RemovesAndUpdatesTarget()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 5 };
        var controller = CreateController(state);
        controller.AddInternalTask(10);
        Assert.Equal(Direction.Up, GetCurrentDirection(controller));

        controller.RemoveInternalTask(10);

        Assert.Equal(Direction.None, GetCurrentDirection(controller));
    }

    /// <summary>
    /// 移除不存在的内部任务时方向不应改变且不抛异常
    /// </summary>
    [Fact]
    public void RemoveInternalTask_NonExisting_DoesNotChangeDirection()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 5 };
        var controller = CreateController(state);
        controller.AddInternalTask(10);
        var dirBefore = GetCurrentDirection(controller);

        controller.RemoveInternalTask(999);

        Assert.Equal(dirBefore, GetCurrentDirection(controller));
    }

    /// <summary>
    /// 移除上行外部任务后若所有任务为空则方向应变为 None
    /// </summary>
    [Fact]
    public void RemoveExternalTask_ExistingUpTask_RemovesAndUpdatesTarget()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 0 };
        var controller = CreateController(state);
        controller.AddExternalTask(7, Direction.Up);
        Assert.Equal(Direction.Up, GetCurrentDirection(controller));

        controller.RemoveExternalTask(7, Direction.Up);

        Assert.Equal(Direction.None, GetCurrentDirection(controller));
    }

    /// <summary>
    /// 使用错误方向移除外部任务不应影响状态
    /// </summary>
    [Fact]
    public void RemoveExternalTask_WrongDirection_NoEffect()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 0 };
        var controller = CreateController(state);
        controller.AddExternalTask(7, Direction.Up);
        var dirBefore = GetCurrentDirection(controller);

        controller.RemoveExternalTask(7, Direction.Down);

        Assert.Equal(dirBefore, GetCurrentDirection(controller));
    }

    /// <summary>
    /// 电梯停止时 SignalDoorOpen 应设置开门请求标志
    /// </summary>
    [Fact]
    public void SignalDoorOpen_WhenStopped_SetsRequestFlag()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 5,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        controller.SignalDoorOpen();

        Assert.True(GetRequestDoorOpen(controller));
    }

    /// <summary>
    /// 电梯移动时 SignalDoorOpen 不应设置开门请求标志
    /// </summary>
    [Fact]
    public void SignalDoorOpen_WhenMoving_DoesNotSetFlag()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 5,
            MovingDirection = Direction.Up,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        controller.SignalDoorOpen();

        Assert.False(GetRequestDoorOpen(controller));
    }

    /// <summary>
    /// 电梯停止且门开时 SignalDoorClose 应设置关门请求标志
    /// </summary>
    [Fact]
    public void SignalDoorClose_WhenStopped_SetsRequestFlag()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 5,
            MovingDirection = Direction.None,
            Door = DoorState.Open
        };
        var controller = CreateController(state);

        controller.SignalDoorClose();

        Assert.True(GetRequestDoorClose(controller));
    }

    /// <summary>
    /// 门在 Opening 状态时 HandleStopped 应递增 DoorOpenRatio
    /// </summary>
    [Fact]
    public void HandleStopped_DoorOpening_IncrementsRatio()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 3,
            MovingDirection = Direction.None,
            Door = DoorState.Opening,
            DoorOpenRatio = 0.0
        };
        var controller = CreateController(state);

        InvokeHandleStopped(controller);

        Assert.True(state.DoorOpenRatio > 0, "DoorOpenRatio should increase during Opening");
    }

    /// <summary>
    /// DoorOpenRatio 达到 1 时门应从 Opening 切换到 Open
    /// </summary>
    [Fact]
    public void HandleStopped_DoorOpening_TransitionsToOpen_WhenRatioReachesOne()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 3,
            MovingDirection = Direction.None,
            Door = DoorState.Opening,
            DoorOpenRatio = 0.99
        };
        var controller = CreateController(state);

        InvokeHandleStopped(controller);

        Assert.Equal(DoorState.Open, state.Door);
        Assert.Equal(1.0, state.DoorOpenRatio);
    }

    /// <summary>
    /// 门在 Closing 状态时 HandleStopped 应递减 DoorOpenRatio
    /// </summary>
    [Fact]
    public void HandleStopped_DoorClosing_DecrementsRatio()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 3,
            MovingDirection = Direction.None,
            Door = DoorState.Closing,
            DoorOpenRatio = 1.0
        };
        var controller = CreateController(state);

        InvokeHandleStopped(controller);

        Assert.True(state.DoorOpenRatio < 1.0, "DoorOpenRatio should decrease during Closing");
    }

    /// <summary>
    /// DoorOpenRatio 降到 0 时门应从 Closing 切换到 Closed
    /// </summary>
    [Fact]
    public void HandleStopped_DoorClosing_TransitionsToClosed_WhenRatioReachesZero()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 3,
            MovingDirection = Direction.None,
            Door = DoorState.Closing,
            DoorOpenRatio = 0.01
        };
        var controller = CreateController(state);

        InvokeHandleStopped(controller);

        Assert.Equal(DoorState.Closed, state.Door);
        Assert.Equal(0.0, state.DoorOpenRatio);
    }

    /// <summary>
    /// 门打开后经过等待间隔应自动开始关闭
    /// </summary>
    [Fact]
    public void HandleStopped_DoorOpen_CountsWaitIntervals()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 3,
            MovingDirection = Direction.None,
            Door = DoorState.Open,
            DoorOpenRatio = 1.0
        };
        var controller = CreateController(state);

        var intervalsDoorOpen = Constants.DoorOpenWaitTimeSec * 1000 / Constants.UpdateInterval;
        for (var i = 0; i < intervalsDoorOpen; i++)
        {
            InvokeHandleStopped(controller);
        }

        Assert.Equal(DoorState.Closing, state.Door);
    }

    /// <summary>
    /// 门关闭且目标在上方时应开始向上移动
    /// </summary>
    [Fact]
    public void HandleStopped_DoorClosed_StartsMoving_WhenTargetAhead()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 3,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);
        controller.AddInternalTask(10);

        InvokeHandleStopped(controller);

        Assert.Equal(Direction.Up, state.MovingDirection);
    }

    /// <summary>
    /// 门关闭且目标在下方时应开始向下移动
    /// </summary>
    [Fact]
    public void HandleStopped_DoorClosed_StartsMovingDown_WhenTargetBelow()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 10,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);
        controller.AddInternalTask(3);

        InvokeHandleStopped(controller);

        Assert.Equal(Direction.Down, state.MovingDirection);
    }

    /// <summary>
    /// HandleMoving 每次调用应递增经过的间隔计数
    /// </summary>
    [Fact]
    public void HandleMoving_IncrementsIntervalCounter()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 5,
            MovingDirection = Direction.Up
        };
        var controller = CreateController(state);
        controller.AddInternalTask(10);

        var before = GetPassedIntervalsSinceLastFloor(controller);
        InvokeHandleMoving(controller);

        Assert.True(GetPassedIntervalsSinceLastFloor(controller) > before,
            "Interval counter should increment each tick");
    }

    /// <summary>
    /// 移动间隔达到阈值时应切换到下一楼层
    /// </summary>
    [Fact]
    public void HandleMoving_ChangesFloor_WhenIntervalReached()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 5,
            MovingDirection = Direction.Up
        };
        var controller = CreateController(state);
        controller.AddInternalTask(10);

        var intervalsPerFloor = Constants.FloorTravelTimeSec * 1000 / Constants.UpdateInterval;
        for (var i = 0; i < intervalsPerFloor; i++)
        {
            InvokeHandleMoving(controller);
        }

        Assert.Equal(6, state.CurrentFloor);
    }

    /// <summary>
    /// 到达目标楼层后应停止并开始开门
    /// </summary>
    [Fact]
    public void HandleMoving_ReachesTarget_StopsAndOpensDoor()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 0,
            MovingDirection = Direction.Up
        };
        var controller = CreateController(state);
        controller.AddInternalTask(1);

        var intervalsPerFloor = Constants.FloorTravelTimeSec * 1000 / Constants.UpdateInterval;
        for (var i = 0; i < intervalsPerFloor; i++)
        {
            InvokeHandleMoving(controller);
        }

        Assert.Equal(1, state.CurrentFloor);
        Assert.Equal(Direction.None, state.MovingDirection);
        Assert.Equal(DoorState.Opening, state.Door);
    }

    /// <summary>
    /// 关门过程中收到开门请求应重新开门
    /// </summary>
    [Fact]
    public void HandleDoorRequests_OpenRequest_DuringClosing_Reopens()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 3,
            MovingDirection = Direction.None,
            Door = DoorState.Closing
        };
        var controller = CreateController(state);
        controller.SignalDoorOpen();

        InvokeHandleDoorRequests(controller);

        Assert.Equal(DoorState.Opening, state.Door);
    }

    /// <summary>
    /// 门开时收到关门请求应开始关门
    /// </summary>
    [Fact]
    public void HandleDoorRequests_CloseRequest_DuringOpen_StartsClosing()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 3,
            MovingDirection = Direction.None,
            Door = DoorState.Open
        };
        var controller = CreateController(state);
        controller.SignalDoorClose();

        InvokeHandleDoorRequests(controller);

        Assert.Equal(DoorState.Closing, state.Door);
    }

    /// <summary>
    /// 同时有开门和关门请求时开门应优先
    /// </summary>
    [Fact]
    public void HandleDoorRequests_OpenTakesPriority_OverClose()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 3,
            MovingDirection = Direction.None,
            Door = DoorState.Closing
        };
        var controller = CreateController(state);
        controller.SignalDoorClose();
        controller.SignalDoorOpen();

        InvokeHandleDoorRequests(controller);

        Assert.Equal(DoorState.Opening, state.Door);
    }

    /// <summary>
    /// 空闲电梯同楼层门关闭时预测时间应至少包含开门耗时
    /// </summary>
    [Fact]
    public void PredictTimeToServe_Idle_SameFloor_Closed_ReturnsDoorOpenTime()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 10,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        var time = controller.PredictTimeToServeExternalCall(10, Direction.Up);

        Assert.True(time >= Constants.DoorOpenCloseTimeSec);
    }

    /// <summary>
    /// 呼叫在上方时预测时间应包含行驶耗时和开门耗时
    /// </summary>
    [Fact]
    public void PredictTimeToServe_Idle_AboveFloor_ReturnsTravelPlusDoorTime()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 0,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        var time = controller.PredictTimeToServeExternalCall(10, Direction.Up);

        Assert.True(time >= 10 * Constants.FloorTravelTimeSec + Constants.DoorOpenCloseTimeSec);
    }

    /// <summary>
    /// 呼叫在下方时预测时间应包含行驶耗时和开门耗时
    /// </summary>
    [Fact]
    public void PredictTimeToServe_Idle_BelowFloor_ReturnsTravelPlusDoorTime()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 10,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        var time = controller.PredictTimeToServeExternalCall(3, Direction.Down);

        Assert.True(time >= 7 * Constants.FloorTravelTimeSec + Constants.DoorOpenCloseTimeSec);
    }

    /// <summary>
    /// 门正在打开时预测时间应包含剩余开门时长
    /// </summary>
    [Fact]
    public void PredictTimeToServe_DoorOpening_AccountsForRemainingOpenTime()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 5,
            MovingDirection = Direction.None,
            Door = DoorState.Opening,
            DoorOpenRatio = 0.5
        };
        var controller = CreateController(state);

        var time = controller.PredictTimeToServeExternalCall(5, Direction.Up);

        Assert.True(time > 0);
        Assert.True(time >= (1.0 - 0.5) * Constants.DoorOpenCloseTimeSec
            + Constants.DoorOpenWaitTimeSec
            + Constants.DoorOpenCloseTimeSec);
    }

    /// <summary>
    /// 门正在关闭时预测时间应包含剩余关门时长
    /// </summary>
    [Fact]
    public void PredictTimeToServe_DoorClosing_AccountsForRemainingCloseTime()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 5,
            MovingDirection = Direction.None,
            Door = DoorState.Closing,
            DoorOpenRatio = 0.8
        };
        var controller = CreateController(state);

        var time = controller.PredictTimeToServeExternalCall(5, Direction.Up);

        Assert.True(time > 0);
    }

    /// <summary>
    /// 存在已有任务时预测时间应包含中途停靠的额外耗时
    /// </summary>
    [Fact]
    public void PredictTimeToServe_WithExistingTasks_AccountsForIntermediateStops()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 0,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        _ = state.AddInternalCall(5);
        _ = state.AddInternalCall(20);
        var controller = CreateController(state);
        controller.AddInternalTask(5);
        controller.AddInternalTask(20);

        var time = controller.PredictTimeToServeExternalCall(15, Direction.Up);

        Assert.True(time >= 5 * Constants.FloorTravelTimeSec
            + Constants.DoorOpenCloseTimeSec + Constants.DoorOpenWaitTimeSec + Constants.DoorOpenCloseTimeSec
            + 10 * Constants.FloorTravelTimeSec
            + Constants.DoorOpenCloseTimeSec);
    }

    /// <summary>
    /// 对所有合法楼层和方向组合预测结果应为非负数
    /// </summary>
    [Fact]
    public void PredictTimeToServe_AllFloors_AllDirections_NonNegative()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 10,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        for (var floor = Constants.MinFloor; floor <= Constants.MaxFloor; floor++)
        {
            if (floor != Constants.MinFloor)
            {
                var timeDown = controller.PredictTimeToServeExternalCall(floor, Direction.Down);
                Assert.True(timeDown >= 0, $"Prediction for floor {floor} Down should be >= 0, got {timeDown}");
            }
            if (floor != Constants.MaxFloor)
            {
                var timeUp = controller.PredictTimeToServeExternalCall(floor, Direction.Up);
                Assert.True(timeUp >= 0, $"Prediction for floor {floor} Up should be >= 0, got {timeUp}");
            }
        }
    }

    /// <summary>
    /// CompleteCurrentFloorTask 应移除当前楼层的内部任务并从 ElevatorState 中删除
    /// </summary>
    [Fact]
    public void CompleteCurrentFloorTask_RemovesInternalTask_FromState()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 3,
            MovingDirection = Direction.None,
            Door = DoorState.Open
        };
        _ = state.AddInternalCall(3);
        var controller = CreateController(state);
        controller.AddInternalTask(3);

        InvokeCompleteCurrentFloorTask(controller, 3);

        // 内部呼叫应从 ElevatorState 中移除
        Assert.DoesNotContain(3, state.InternalCalls);
    }

    /// <summary>
    /// CompleteCurrentFloorTask 上行时应移除上行外部任务并从 FloorCallState 中删除
    /// </summary>
    [Fact]
    public void CompleteCurrentFloorTask_RemovesExternalUpTask_FromFloorCallState()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 5,
            MovingDirection = Direction.None,
            Door = DoorState.Open
        };
        var controller = CreateController(state);
        controller.AddExternalTask(5, Direction.Up);

        InvokeCompleteCurrentFloorTask(controller, 5);

        _floorCallStateMock.Verify(f => f.RemoveFloorCall(5, Direction.Up), Times.Once);
    }

    /// <summary>
    /// CompleteCurrentFloorTask 下行时应移除下行外部任务
    /// </summary>
    [Fact]
    public void CompleteCurrentFloorTask_RemovesExternalDownTask_FromFloorCallState()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 10,
            MovingDirection = Direction.None,
            Door = DoorState.Open
        };
        var controller = CreateController(state);
        controller.AddExternalTask(10, Direction.Down);

        InvokeCompleteCurrentFloorTask(controller, 10);

        _floorCallStateMock.Verify(f => f.RemoveFloorCall(10, Direction.Down), Times.Once);
    }

    /// <summary>
    /// 通过反射获取 ElevatorController 的 _currentDirection 私有字段值
    /// </summary>
    /// <param name="controller">电梯控制器实例</param>
    /// <returns>当前的逻辑移动方向</returns>
    private static Direction GetCurrentDirection(ElevatorController controller)
    {
        var field = typeof(ElevatorController).GetField("_currentDirection",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (Direction)field!.GetValue(controller)!;
    }

    /// <summary>
    /// 通过反射获取 _passedIntervalsSinceLastFloor 私有字段值
    /// </summary>
    /// <param name="controller">电梯控制器实例</param>
    /// <returns>从上一个楼层开始移动后经过的计时器周期数</returns>
    private static int GetPassedIntervalsSinceLastFloor(ElevatorController controller)
    {
        var field = typeof(ElevatorController).GetField("_passedIntervalsSinceLastFloor",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (int)field!.GetValue(controller)!;
    }

    /// <summary>
    /// 通过反射获取 _requestDoorOpen 私有字段值
    /// </summary>
    /// <param name="controller">电梯控制器实例</param>
    /// <returns>缓存的开门请求标志</returns>
    private static bool GetRequestDoorOpen(ElevatorController controller)
    {
        var field = typeof(ElevatorController).GetField("_requestDoorOpen",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (bool)field!.GetValue(controller)!;
    }

    /// <summary>
    /// 通过反射获取 _requestDoorClose 私有字段值
    /// </summary>
    /// <param name="controller">电梯控制器实例</param>
    /// <returns>缓存的关门请求标志</returns>
    private static bool GetRequestDoorClose(ElevatorController controller)
    {
        var field = typeof(ElevatorController).GetField("_requestDoorClose",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (bool)field!.GetValue(controller)!;
    }

    /// <summary>
    /// 通过反射调用 HandleStopped 私有方法以测试停靠状态逻辑
    /// </summary>
    /// <param name="controller">电梯控制器实例</param>
    private static void InvokeHandleStopped(ElevatorController controller)
    {
        var method = typeof(ElevatorController).GetMethod("HandleStopped",
            BindingFlags.NonPublic | BindingFlags.Instance);
        _ = method!.Invoke(controller, null);
    }

    /// <summary>
    /// 通过反射调用 HandleMoving 私有方法以测试移动状态逻辑
    /// </summary>
    /// <param name="controller">电梯控制器实例</param>
    private static void InvokeHandleMoving(ElevatorController controller)
    {
        var method = typeof(ElevatorController).GetMethod("HandleMoving",
            BindingFlags.NonPublic | BindingFlags.Instance);
        _ = method!.Invoke(controller, null);
    }

    /// <summary>
    /// 通过反射调用 HandleDoorRequests 私有方法以测试门请求处理逻辑
    /// </summary>
    /// <param name="controller">电梯控制器实例</param>
    private static void InvokeHandleDoorRequests(ElevatorController controller)
    {
        var method = typeof(ElevatorController).GetMethod("HandleDoorRequests",
            BindingFlags.NonPublic | BindingFlags.Instance);
        _ = method!.Invoke(controller, null);
    }

    /// <summary>
    /// 通过反射调用 CompleteCurrentFloorTask 私有方法以测试任务完成逻辑
    /// </summary>
    /// <param name="controller">电梯控制器实例</param>
    /// <param name="floor">当前楼层</param>
    private static void InvokeCompleteCurrentFloorTask(ElevatorController controller, int floor)
    {
        var method = typeof(ElevatorController).GetMethod("CompleteCurrentFloorTask",
            BindingFlags.NonPublic | BindingFlags.Instance);
        _ = method!.Invoke(controller, [floor]);
    }
}
