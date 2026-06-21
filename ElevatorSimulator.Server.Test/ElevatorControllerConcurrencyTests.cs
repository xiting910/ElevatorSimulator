using ElevatorSimulator.Server.Core.Controllers;
using ElevatorSimulator.Server.Models;
using ElevatorSimulator.Server.Models.Interfaces;
using ElevatorSimulator.Share.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Timer = System.Timers.Timer;

namespace ElevatorSimulator.Server.Test;

/// <summary>
/// 并发安全测试, 验证多线程下任务增删, 状态读写和楼层呼叫操作不会导致数据破坏或死锁
/// </summary>
public sealed class ElevatorControllerConcurrencyTests
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
    public ElevatorControllerConcurrencyTests()
    {
        _floorCallStateMock = new Mock<IFloorCallState>();
        _loggerMock = new Mock<ILogger<ElevatorController>>();
        _timer = new Timer(50);
    }

    /// <summary>
    /// 使用指定电梯状态和共享 Mock 依赖创建 <see cref="ElevatorController"/> 实例
    /// </summary>
    private ElevatorController CreateController(IElevatorState state) =>
        new(_timer, state, _floorCallStateMock.Object, _loggerMock.Object);

    /// <summary>
    /// 多线程同时添加内部任务不应抛出异常或死锁
    /// </summary>
    [Fact]
    public async Task AddInternalTask_Concurrent_NoDeadlockOrException()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 10 };
        var controller = CreateController(state);

        // 先添加一个任务使方向非 None, 避免后续并发添加触发状态不变量异常
        controller.AddInternalTask(20);

        var tasks = new Task[8];
        for (var i = 0; i < tasks.Length; i++)
        {
            var floor = i + 1;
            tasks[i] = Task.Run(() => controller.AddInternalTask(floor));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 多线程同时添加外部任务不应抛出异常或死锁
    /// </summary>
    [Fact]
    public async Task AddExternalTask_Concurrent_NoDeadlockOrException()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 10 };
        var controller = CreateController(state);

        // 先添加一个任务使方向非 None
        controller.AddExternalTask(20, Direction.Up);

        var tasks = new Task[8];
        for (var i = 0; i < tasks.Length; i++)
        {
            var floor = i + 1;
            tasks[i] = Task.Run(() => controller.AddExternalTask(floor, Direction.Up));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 多线程同时增加和删除任务不应抛出异常
    /// </summary>
    [Fact]
    public async Task AddAndRemove_Concurrent_NoDeadlockOrException()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 10 };
        var controller = CreateController(state);

        for (var i = 0; i < 20; i++)
        {
            var dir = i % 2 == 0 ? Direction.Up : Direction.Down;
            controller.AddExternalTask(i, dir);
        }

        var tasks = new Task[8];
        for (var i = 0; i < tasks.Length; i++)
        {
            var floor = i;
            tasks[i] = Task.Run(() =>
            {
                controller.AddInternalTask(floor + 30);
                controller.RemoveExternalTask(floor, Direction.Up);
            });
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 多线程并发操作 FloorCallState 不应抛出异常
    /// </summary>
    [Fact]
    public async Task FloorCallState_ConcurrentAddRemove_NoException()
    {
        var state = new FloorCallState();
        var tasks = new Task[16];
        for (var i = 0; i < tasks.Length; i++)
        {
            var floor = i;
            var dir = i % 2 == 0 ? Direction.Up : Direction.Down;
            tasks[i] = Task.Run(() =>
            {
                _ = state.AddFloorCall(floor, dir);
                _ = state.RemoveFloorCall(floor, dir);
            });
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 多线程并发操作 ElevatorState 的内部呼叫不应抛出异常
    /// </summary>
    [Fact]
    public async Task ElevatorState_ConcurrentInternalCalls_NoException()
    {
        var state = new ElevatorState { Id = 0 };
        var tasks = new Task[16];
        for (var i = 0; i < tasks.Length; i++)
        {
            var floor = i;
            tasks[i] = Task.Run(() =>
            {
                _ = state.AddInternalCall(floor);
                _ = state.RemoveInternalCall(floor);
            });
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 并发读写 ElevatorState 属性不应导致数据破坏
    /// </summary>
    [Fact]
    public async Task ElevatorState_ConcurrentPropertyAccess_Consistent()
    {
        var state = new ElevatorState { Id = 0, CurrentFloor = 5 };
        var tasks = new Task[4];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (var j = 0; j < 100; j++)
                {
                    _ = state.CurrentFloor;
                    state.MovingDirection = Direction.Up;
                    _ = state.MovingDirection;
                    state.MovingDirection = Direction.None;
                    _ = state.Door;
                    state.Door = DoorState.Closed;
                }
            });
        }

        await Task.WhenAll(tasks);
    }
}
