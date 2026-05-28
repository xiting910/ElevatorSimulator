using System;
using System.ComponentModel;
using System.Timers;

namespace ElevatorSimulator.Server.Core;

/// <summary>
/// 电梯中央调度与状态管理器, 采用单例模式
/// </summary>
internal sealed class ElevatorManager : IDisposable
{
    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static ElevatorManager Instance => _lazyInstance.Value;

    /// <summary>
    /// 单例实例的延迟初始化, 确保在第一次访问时才创建实例并启动服务
    /// </summary>
    private static readonly Lazy<ElevatorManager> _lazyInstance = new(() => new());

    /// <summary>
    /// 用于控制所有控制器的定时器
    /// </summary>
    private readonly Timer _timer = new(Share.Constants.UpdateInterval) { AutoReset = true, Enabled = true };

    /// <summary>
    /// 系统中所有的电梯实体
    /// </summary>
    public Models.ElevatorState[] Elevators { get; }

    /// <summary>
    /// 激活的全局楼层外部呼叫集合
    /// </summary>
    public Models.FloorCallState FloorCallState { get; } = new();

    /// <summary>
    /// 系统中所有的电梯实体的控制器
    /// </summary>
    public ElevatorController[] ElevatorControllers { get; }

    /// <summary>
    /// 私有构造函数, 初始化电梯状态并准备调度系统
    /// </summary>
    private ElevatorManager()
    {
        // 初始化全部电梯状态
        Elevators = [new() { Id = 0 }, new() { Id = 1 }, new() { Id = 2 }];
        foreach (var elevator in Elevators)
        {
            elevator.PropertyChanged += OnElevatorStatePropertyChanged;
        }

        // 订阅楼层呼叫状态变化事件
        FloorCallState.PropertyChanged += OnFloorCallStatePropertyChanged;

        // 初始化全部电梯控制器
        ElevatorControllers = [new(0, _timer), new(1, _timer), new(2, _timer)];

        // 更新 UI 显示初始状态
        UI.MainForm.Instance.UpdateElevatorStatus(Elevators);
    }

    /// <summary>
    /// 添加楼层外部呼叫
    /// </summary>
    /// <param name="floor">呼叫所在的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    public void AddFloorCall(int floor, Share.Direction direction)
    {
        if (FloorCallState.AddFloorCall(floor, direction))
        {
            Utils.Logger.Info($"收到楼层呼叫: {floor} 楼, 方向 {direction}");
        }
    }

    /// <summary>
    /// 添加电梯内部呼叫
    /// </summary>
    /// <param name="elevatorId">呼叫所在的电梯 ID</param>
    /// <param name="targetFloor">呼叫的目标楼层</param>
    public void AddElevatorCall(int elevatorId, int targetFloor)
    {
        if (Elevators[elevatorId].AddInternalCall(targetFloor))
        {
            ElevatorControllers[elevatorId].AddInternalTask(targetFloor);
            Utils.Logger.Info($"收到电梯呼叫: 电梯 {elevatorId}, 目标楼层 {targetFloor}");
        }
    }

    /// <summary>
    /// 取消楼层外部呼叫
    /// </summary>
    /// <param name="floor">呼叫所在的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    public void CancelFloorCall(int floor, Share.Direction direction)
    {
        if (FloorCallState.RemoveFloorCall(floor, direction))
        {
            foreach (var controller in ElevatorControllers)
            {
                controller.RemoveExternalTask(floor, direction);
            }
            Utils.Logger.Info($"取消楼层呼叫: {floor} 楼, 方向 {direction}");
        }
    }

    /// <summary>
    /// 取消电梯内部呼叫
    /// </summary>
    /// <param name="elevatorId">呼叫所在的电梯 ID</param>
    /// <param name="targetFloor">呼叫的目标楼层</param>
    public void CancelElevatorCall(int elevatorId, int targetFloor)
    {
        if (Elevators[elevatorId].RemoveInternalCall(targetFloor))
        {
            ElevatorControllers[elevatorId].RemoveInternalTask(targetFloor);
            Utils.Logger.Info($"取消电梯呼叫: 电梯 {elevatorId}, 目标楼层 {targetFloor}");
        }
    }

    /// <summary>
    /// 电梯状态发生变化时调用
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">属性变化事件参数</param>
    private void OnElevatorStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Models.ElevatorState state)
        {
            if (e.PropertyName is nameof(Models.ElevatorState.CurrentFloor) or nameof(Models.ElevatorState.MovingDirection) or nameof(Models.ElevatorState.Door) or nameof(Models.ElevatorState.DoorOpenRatio))
            {
                // 仅当电梯的当前楼层、移动方向、门状态或门开度发生变化时才更新 UI, 避免过于频繁的刷新
                UI.MainForm.Instance.UpdateElevatorStatus(Elevators);
            }

            // 向客户端广播最新的电梯状态, 以便客户端更新显示
            _ = PipeServerManager.Instance.BroadcastElevatorStatusAsync(new()
            {
                Id = state.Id,
                CurrentFloor = state.CurrentFloor,
                MovingDirection = state.MovingDirection,
                Door = state.Door,
                DoorOpenRatio = state.DoorOpenRatio,
                InternalCalls = state.InternalCalls
            });
        }
    }

    /// <summary>
    /// 楼层呼叫状态发生变化时调用
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">属性变化事件参数</param>
    private void OnFloorCallStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Models.FloorCallState state)
        {
            // 更新 UI 显示最新的楼层呼叫状态
            UI.MainForm.Instance.UpdateFloorCalls(state.ActiveCalls);

            // 向客户端广播最新的楼层呼叫状态, 以便客户端更新显示
            _ = PipeServerManager.Instance.BroadcastFloorCallStatusAsync(new()
            {
                ActiveCalls = state.ActiveCalls
            });
        }
    }

    /// <summary>
    /// 释放资源, 停止定时器并清理事件订阅
    /// </summary>
    public void Dispose()
    {
        _timer.Dispose();
        foreach (var elevator in Elevators)
        {
            elevator.PropertyChanged -= OnElevatorStatePropertyChanged;
        }
        FloorCallState.PropertyChanged -= OnFloorCallStatePropertyChanged;
        GC.SuppressFinalize(this);
    }
}
