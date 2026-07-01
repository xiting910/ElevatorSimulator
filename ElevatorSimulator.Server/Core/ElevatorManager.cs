using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;

namespace ElevatorSimulator.Server.Core;

/// <summary>
/// 电梯中央调度与状态管理器
/// </summary>
public sealed partial class ElevatorManager : Interfaces.IElevatorManager
{
    /// <inheritdoc/>
    public Models.Interfaces.IFloorCallState FloorCallState { get; }

    /// <inheritdoc/>
    public event Action<IEnumerable<Models.Interfaces.IElevatorState>>? ElevatorStatusChanged;

    /// <inheritdoc/>
    public event Action<Dictionary<int, Direction[]>>? FloorCallsChanged;

    /// <inheritdoc/>
    public IEnumerable<Models.Interfaces.IElevatorState> GetCurrentStates()
    {
        return _controllers.Select(c => c.State);
    }

    /// <summary>
    /// 用于控制所有控制器的定时器
    /// </summary>
    private readonly Timer _timer;

    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<ElevatorManager> _logger;

    /// <summary>
    /// 系统中所有的电梯实体的控制器
    /// </summary>
    private readonly Interfaces.IElevatorController[] _controllers;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="floorCallState">楼层呼叫状态</param>
    /// <param name="createState">电梯状态创建委托</param>
    /// <param name="createController">电梯控制器创建委托</param>
    public ElevatorManager(ILogger<ElevatorManager> logger, Models.Interfaces.IFloorCallState floorCallState, Func<int, Models.Interfaces.IElevatorState> createState, Func<Timer, Models.Interfaces.IElevatorState, Interfaces.IElevatorController> createController)
    {
        _logger = logger;
        FloorCallState = floorCallState;
        _timer = new(Constants.UpdateInterval) { Enabled = true };

        // 通过委托创建全部电梯控制器
        _controllers = new Interfaces.IElevatorController[Constants.ElevatorCount];
        for (var i = 0; i < Constants.ElevatorCount; i++)
        {
            _controllers[i] = createController(_timer, createState(i));
        }
    }

    /// <inheritdoc/>
    public void Initialize()
    {
        // 订阅电梯状态变化事件
        foreach (var c in _controllers)
        {
            c.State.PropertyChanged += OnElevatorStatePropertyChanged;
        }

        // 订阅楼层呼叫状态变化事件
        FloorCallState.PropertyChanged += OnFloorCallStatePropertyChanged;

        // 通知订阅者初始电梯状态
        ElevatorStatusChanged?.Invoke(_controllers.Select(c => c.State));
    }

    /// <inheritdoc/>
    public void AddFloorCall(int floor, Direction direction)
    {
        if (FloorCallState.AddFloorCall(floor, direction))
        {
            // 检查是否存在空闲电梯
            var hasIdle = _controllers.Any(c => c.State.MovingDirection is Direction.None);

            // 有空闲电梯时, 给忙碌电梯的预测时间加 2 倍惩罚, 避免为省几秒而让已有乘客经历不必要的等待和被插队的感觉
            var best = _controllers.MinBy(c =>
            {
                var t = c.PredictTimeToServeExternalCall(floor, direction);
                return hasIdle && c.State.MovingDirection is not Direction.None ? t * 2.0 : t;
            })!;

            // 将任务添加到最优电梯
            best.AddExternalTask(floor, direction);

            // 记录日志
            LogAddFloorCall(_logger, floor, direction, best.Id);
        }
    }

    /// <inheritdoc/>
    public void AddElevatorCall(int elevatorId, int targetFloor)
    {
        if (_controllers[elevatorId].State.AddInternalCall(targetFloor))
        {
            _controllers[elevatorId].AddInternalTask(targetFloor);
            LogAddElevatorCall(_logger, elevatorId, targetFloor);
        }
    }

    /// <inheritdoc/>
    public void CancelFloorCall(int floor, Direction direction)
    {
        if (FloorCallState.RemoveFloorCall(floor, direction))
        {
            foreach (var controller in _controllers)
            {
                controller.RemoveExternalTask(floor, direction);
            }
            LogCancelFloorCall(_logger, floor, direction);
        }
    }

    /// <inheritdoc/>
    public void CancelElevatorCall(int elevatorId, int targetFloor)
    {
        if (_controllers[elevatorId].State.RemoveInternalCall(targetFloor))
        {
            _controllers[elevatorId].RemoveInternalTask(targetFloor);
            LogCancelElevatorCall(_logger, elevatorId, targetFloor);
        }
    }

    /// <inheritdoc/>
    public void RequestDoorOpen(int elevatorId)
    {
        _controllers[elevatorId].SignalDoorOpen();
    }

    /// <inheritdoc/>
    public void RequestDoorClose(int elevatorId)
    {
        _controllers[elevatorId].SignalDoorClose();
    }

    /// <summary>
    /// 电梯状态发生变化时调用
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">属性变化事件参数</param>
    private void OnElevatorStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Models.Interfaces.IElevatorState)
        {
            ElevatorStatusChanged?.Invoke(_controllers.Select(c => c.State));
        }
    }

    /// <summary>
    /// 楼层呼叫状态发生变化时调用
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">属性变化事件参数</param>
    private void OnFloorCallStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Models.Interfaces.IFloorCallState state)
        {
            FloorCallsChanged?.Invoke(state.ActiveCalls);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _timer.Dispose();
        foreach (var c in _controllers)
        {
            c.State.PropertyChanged -= OnElevatorStatePropertyChanged;
        }
        FloorCallState.PropertyChanged -= OnFloorCallStatePropertyChanged;
        GC.SuppressFinalize(this);
    }
}
