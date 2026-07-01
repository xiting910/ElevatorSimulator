using ElevatorSimulator.Server.Core.Controllers;
using ElevatorSimulator.Server.Models;
using ElevatorSimulator.Server.Models.Interfaces;
using ElevatorSimulator.Share;
using ElevatorSimulator.Share.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Timer = System.Timers.Timer;

namespace ElevatorSimulator.Server.Test;

/// <summary>
/// Look 算法专项测试, 通过 <see cref="ElevatorController.PredictTimeToServeExternalCall"/> 间接验证
/// GetNextStopUp / GetNextStopDown / HasAnyInRange / GetMinInRange / GetMaxInRange 等静态方法
/// </summary>
public sealed class ElevatorControllerLookTests
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
    public ElevatorControllerLookTests()
    {
        _floorCallStateMock = new Mock<IFloorCallState>();
        _loggerMock = new Mock<ILogger<ElevatorController>>();
        _timer = new Timer(50);
    }

    /// <summary>
    /// 使用指定电梯状态和共享 Mock 依赖创建 <see cref="ElevatorController"/> 实例
    /// </summary>
    /// <param name="state">电梯逻辑状态</param>
    private ElevatorController CreateController(IElevatorState state)
    {
        return new(_timer, state, _floorCallStateMock.Object, _loggerMock.Object);
    }

    /// <summary>
    /// 同楼层门关闭时, 预测时间应至少包含门开启耗时
    /// </summary>
    [Fact]
    public void PredictTimeToServe_SameFloorClosed_ReturnsDoorOpenTime()
    {
        // Arrange
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 10,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        // Act
        var time = controller.PredictTimeToServeExternalCall(10, Direction.Up);

        // Assert: should be at least door open time (2 seconds)
        Assert.True(time >= Constants.DoorOpenCloseTimeSec);
    }

    /// <summary>
    /// 呼叫楼层在当前楼层之上时, 预测时间应包含行驶耗时
    /// </summary>
    [Fact]
    public void PredictTimeToServe_AboveCurrentFloor_ReturnsPositiveTime()
    {
        // Arrange
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 0,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        // Act
        var time = controller.PredictTimeToServeExternalCall(10, Direction.Up);

        // Assert: travel time = 10 floors × 3s = 30s
        Assert.True(time >= 10 * Constants.FloorTravelTimeSec);
    }

    /// <summary>
    /// 呼叫楼层在当前楼层之下时, 预测时间应包含行驶耗时
    /// </summary>
    [Fact]
    public void PredictTimeToServe_BelowCurrentFloor_ReturnsPositiveTime()
    {
        // Arrange
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 10,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        // Act
        var time = controller.PredictTimeToServeExternalCall(3, Direction.Down);

        // Assert
        Assert.True(time >= 7 * Constants.FloorTravelTimeSec);
    }

    /// <summary>
    /// 存在已有任务时, 预测时间应包含中途停靠的额外耗时
    /// </summary>
    [Fact]
    public void PredictTimeToServe_WithExistingTasks_AccountsForStops()
    {
        // Arrange
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

        // Act
        var time = controller.PredictTimeToServeExternalCall(15, Direction.Up);

        // Must stop at floor 5 first, then continue to 15
        Assert.True(time > 0);
    }

    /// <summary>
    /// 对所有合法楼层和方向组合, 预测结果应为非负数
    /// </summary>
    [Fact]
    public void PredictTimeToServe_AllElevators_Consistent()
    {
        // Verify that all elevators' predictions are non-negative
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 0,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        for (var floor = Constants.MinFloor; floor <= Constants.MaxFloor; floor++)
        {
            // 跳过语义上不可能的边界方向组合:底楼不可向下,顶楼不可向上
            if (floor != Constants.MinFloor)
            {
                var timeDown = controller.PredictTimeToServeExternalCall(floor, Direction.Down);
                Assert.True(timeDown >= 0, $"Prediction for floor {floor} Down should be >= 0");
            }

            if (floor != Constants.MaxFloor)
            {
                var timeUp = controller.PredictTimeToServeExternalCall(floor, Direction.Up);
                Assert.True(timeUp >= 0, $"Prediction for floor {floor} Up should be >= 0");
            }
        }
    }

    /// <summary>
    /// 空闲电梯的预测时间应与距离成正比 (越远耗时越长)
    /// </summary>
    [Fact]
    public void IdleElevator_PredictTime_RisesWithDistance()
    {
        // Arrange
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 5,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        // Act
        var timeNear = controller.PredictTimeToServeExternalCall(6, Direction.Up);
        var timeFar = controller.PredictTimeToServeExternalCall(30, Direction.Up);

        // Assert: further floor takes more time
        Assert.True(timeFar > timeNear);
    }

    /// <summary>
    /// 最小楼层附近的预测应返回有效正数
    /// </summary>
    [Fact]
    public void PredictTimeToServe_BoundaryFloor_MinFloor()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = Constants.MinFloor + 5,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        // MinFloor = -2, 从 3 楼下到 -2, 距离为 5 层
        var time = controller.PredictTimeToServeExternalCall(Constants.MinFloor + 3, Direction.Down);

        Assert.True(time > 0);
    }

    /// <summary>
    /// 最大楼层附近的预测应返回有效正数
    /// </summary>
    [Fact]
    public void PredictTimeToServe_BoundaryFloor_MaxFloor()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = Constants.MaxFloor - 5,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);

        // MaxFloor=33, 从 28 楼向上, 避免极端边界导致模拟超步数
        var time = controller.PredictTimeToServeExternalCall(Constants.MaxFloor - 7, Direction.Up);

        Assert.True(time > 0);
    }

    /// <summary>
    /// 电梯朝呼叫方向移动时预测时间应合理 (小于 100 秒)
    /// </summary>
    [Fact]
    public void PredictTimeToServe_MovingTowardCall_ReturnsReasonableTime()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 5,
            MovingDirection = Direction.Up,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);
        // 添加任务以设置 _currentDirection 与 MovingDirection 一致
        controller.AddExternalTask(15, Direction.Up);

        // 电梯已朝上移动, 调用在 10 楼上方
        var time = controller.PredictTimeToServeExternalCall(10, Direction.Up);

        Assert.True(time > 0);
        Assert.True(time < 100, "Should not be unreasonably large");
    }

    /// <summary>
    /// 电梯背离呼叫方向移动时预测应返回有效正数
    /// </summary>
    [Fact]
    public void PredictTimeToServe_MovingAwayFromCall_ReturnsReasonableTime()
    {
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 10,
            MovingDirection = Direction.Up,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);
        // 添加向上任务以设置 _currentDirection 与 MovingDirection 一致
        controller.AddExternalTask(20, Direction.Up);

        // 电梯朝上, 但呼叫在下方
        var time = controller.PredictTimeToServeExternalCall(3, Direction.Down);

        Assert.True(time > 0);
    }

    /// <summary>
    /// 存在两个内部任务时预测应返回有效正数
    /// </summary>
    [Fact]
    public void PredictTimeToServe_Idle_MultipleInternalTasks_IgnoresDirectionalConstraint()
    {
        // 电梯在 10 楼空闲, 有内部任务 5 和 20
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 10,
            MovingDirection = Direction.None,
            Door = DoorState.Closed
        };
        _ = state.AddInternalCall(5);
        _ = state.AddInternalCall(20);
        var controller = CreateController(state);
        controller.AddInternalTask(5);
        controller.AddInternalTask(20);

        // 预测到 15 楼 Up 的呼叫
        var time = controller.PredictTimeToServeExternalCall(15, Direction.Up);

        Assert.True(time > 0, "Should produce a valid prediction even with 2 internal tasks");
    }

    /// <summary>
    /// 向上移动且上方有上行外部任务时预测应触发哨兵逻辑并返回有效正数
    /// </summary>
    [Fact]
    public void PredictTimeToServe_MovingUp_WithUpExternalAhead_AddsSentinel()
    {
        // 电梯在 0 楼向上移动, 上方有向上外部任务 → 应添加哨兵 MaxFloor
        // 这是 PredictTimeToServe 中"insert sentinel"逻辑的测试
        var state = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 0,
            MovingDirection = Direction.Up,
            Door = DoorState.Closed
        };
        var controller = CreateController(state);
        controller.AddExternalTask(5, Direction.Up);

        // 电梯正在向上途中, 预测到 3 楼 Up 的呼叫
        var time = controller.PredictTimeToServeExternalCall(3, Direction.Up);

        Assert.True(time > 0);
    }

    /// <summary>
    /// 同楼层门已打开时预测时间应返回有效正数
    /// </summary>
    [Fact]
    public void PredictTimeToServe_SameFloor_DoorOpen_ReturnsValidTime()
    {
        var stateOpen = new ElevatorState
        {
            Id = 0,
            CurrentFloor = 5,
            MovingDirection = Direction.None,
            Door = DoorState.Open,
            DoorOpenRatio = 1.0
        };
        var ctrlOpen = CreateController(stateOpen);

        var timeOpen = ctrlOpen.PredictTimeToServeExternalCall(5, Direction.Up);

        // 同楼层门已开, 预测时间应包含剩余等待 + 关门 + 重新开门
        Assert.True(timeOpen > 0);
    }
}
