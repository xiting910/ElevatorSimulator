using System;
using System.Collections.Generic;

namespace ElevatorSimulator.Server.Core.Interfaces;

/// <summary>
/// 电梯中央调度与状态管理器接口
/// </summary>
public interface IElevatorManager : IDisposable
{
    /// <summary>
    /// 激活的全局楼层外部呼叫集合
    /// </summary>
    Models.Interfaces.IFloorCallState FloorCallState { get; }

    /// <summary>
    /// 电梯状态更新事件, 当电梯状态发生变化时触发
    /// </summary>
    event Action<IEnumerable<Models.Interfaces.IElevatorState>>? ElevatorStatusChanged;

    /// <summary>
    /// 楼层呼叫状态更新事件, 当楼层呼叫状态发生变化时触发
    /// </summary>
    event Action<Dictionary<int, Direction[]>>? FloorCallsChanged;

    /// <summary>
    /// 获取当前所有电梯状态的快照
    /// </summary>
    IEnumerable<Models.Interfaces.IElevatorState> GetCurrentStates();

    /// <summary>
    /// 初始化电梯管理器
    /// </summary>
    void Initialize();

    /// <summary>
    /// 添加楼层外部呼叫
    /// </summary>
    /// <param name="floor">呼叫所在的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    void AddFloorCall(int floor, Direction direction);

    /// <summary>
    /// 添加电梯内部呼叫
    /// </summary>
    /// <param name="elevatorId">呼叫所在的电梯 ID</param>
    /// <param name="targetFloor">呼叫的目标楼层</param>
    void AddElevatorCall(int elevatorId, int targetFloor);

    /// <summary>
    /// 取消楼层外部呼叫
    /// </summary>
    /// <param name="floor">呼叫所在的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    void CancelFloorCall(int floor, Direction direction);

    /// <summary>
    /// 取消电梯内部呼叫
    /// </summary>
    /// <param name="elevatorId">呼叫所在的电梯 ID</param>
    /// <param name="targetFloor">呼叫的目标楼层</param>
    void CancelElevatorCall(int elevatorId, int targetFloor);

    /// <summary>
    /// 请求打开电梯门
    /// </summary>
    /// <param name="elevatorId">电梯 ID</param>
    void RequestDoorOpen(int elevatorId);

    /// <summary>
    /// 请求关闭电梯门
    /// </summary>
    /// <param name="elevatorId">电梯 ID</param>
    void RequestDoorClose(int elevatorId);
}
