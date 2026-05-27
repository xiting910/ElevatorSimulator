using System;
using System.ComponentModel;

namespace ElevatorSimulator.Server.Core;

/// <summary>
/// 电梯中央调度与状态管理器, 采用单例模式
/// </summary>
internal sealed class ElevatorManager
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
    /// 系统中所有的电梯实体
    /// </summary>
    public Models.ElevatorState[] Elevators { get; } = new Models.ElevatorState[Share.Constants.ElevatorCount];

    /// <summary>
    /// 激活的全局楼层外部呼叫集合
    /// </summary>
    public Models.FloorCallState FloorCallState { get; } = new();

    /// <summary>
    /// 私有构造函数, 初始化电梯状态并准备调度系统
    /// </summary>
    private ElevatorManager()
    {
        // 初始化全部电梯状态
        for (var i = 0; i < Share.Constants.ElevatorCount; i++)
        {
            Elevators[i] = new() { Id = i };
            Elevators[i].PropertyChanged += OnElevatorStatePropertyChanged;
        }

        // 更新 UI 显示初始状态
        UI.MainForm.Instance.UpdateElevatorStatus(Elevators);

        // 订阅楼层呼叫状态变化事件
        FloorCallState.PropertyChanged += OnFloorCallStatePropertyChanged;
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
            Utils.Logger.Info($"取消电梯呼叫: 电梯 {elevatorId}, 目标楼层 {targetFloor}");
        }
    }

    /// <summary>
    /// 完成楼层外部呼叫
    /// </summary>
    /// <param name="floor">呼叫所在的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    public void CompleteFloorCall(int floor, Share.Direction direction)
    {
        if (FloorCallState.RemoveFloorCall(floor, direction))
        {
            Utils.Logger.Info($"完成楼层呼叫: {floor} 楼, 方向 {direction}");
        }
    }

    /// <summary>
    /// 完成电梯内部呼叫
    /// </summary>
    /// <param name="elevatorId">呼叫所在的电梯 ID</param>
    /// <param name="targetFloor">呼叫的目标楼层</param>
    public void CompleteElevatorCall(int elevatorId, int targetFloor)
    {
        if (Elevators[elevatorId].RemoveInternalCall(targetFloor))
        {
            Utils.Logger.Info($"完成电梯呼叫: 电梯 {elevatorId}, 目标楼层 {targetFloor}");
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
        if (e.PropertyName == nameof(Models.FloorCallState.ActiveCalls))
        {
            // 更新 UI 显示最新的楼层呼叫状态
            UI.MainForm.Instance.UpdateFloorCalls(FloorCallState.ActiveCalls);

            // 向客户端广播最新的楼层呼叫状态, 以便客户端更新显示
            _ = PipeServerManager.Instance.BroadcastFloorCallStatusAsync(new()
            {
                ActiveCalls = FloorCallState.ActiveCalls
            });
        }
    }
}
