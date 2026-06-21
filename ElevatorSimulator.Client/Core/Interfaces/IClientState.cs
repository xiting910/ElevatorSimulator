using System;

namespace ElevatorSimulator.Client.Core.Interfaces;

/// <summary>
/// 客户端状态接口, 存储电梯与楼层缓存数据
/// </summary>
public interface IClientState
{
    /// <summary>
    /// 本客户端唯一标识
    /// </summary>
    string ClientId { get; }

    /// <summary>
    /// 当前所在楼层
    /// </summary>
    int CurrentFloor { get; set; }

    /// <summary>
    /// 当前所在电梯 ID, <see langword="null"/> 表示未进入电梯
    /// </summary>
    int? CurrentElevatorId { get; set; }

    /// <summary>
    /// 缓存的电梯状态数组
    /// </summary>
    Messages.ElevatorStatusMessage[] ElevatorStatuses { get; }

    /// <summary>
    /// 缓存的楼层呼叫状态
    /// </summary>
    Messages.FloorStatusMessage FloorStatus { get; }

    /// <summary>
    /// 电梯状态更新事件
    /// </summary>
    event Action<Messages.ElevatorStatusMessage>? OnElevatorStatusUpdated;

    /// <summary>
    /// 楼层状态更新事件
    /// </summary>
    event Action<Messages.FloorStatusMessage>? OnFloorStatusUpdated;

    /// <summary>
    /// 检查指定电梯是否可进入
    /// </summary>
    /// <param name="elevatorId">电梯 ID</param>
    /// <returns><see langword="true"/> 如果电梯可以进入, 否则为 <see langword="false"/></returns>
    bool CanEnterElevator(int elevatorId);

    /// <summary>
    /// 检查当前是否可以退出电梯
    /// </summary>
    /// <returns><see langword="true"/> 如果可以退出电梯, 否则为 <see langword="false"/></returns>
    bool CanExitElevator();

    /// <summary>
    /// 检查当前楼层是否有指定方向的外部呼叫
    /// </summary>
    /// <param name="direction">呼叫方向</param>
    /// <returns><see langword="true"/> 如果有外部呼叫, 否则为 <see langword="false"/></returns>
    bool HasActiveCall(Direction direction);

    /// <summary>
    /// 更新电梯状态缓存
    /// </summary>
    /// <param name="status">新的电梯状态</param>
    void UpdateElevatorStatus(Messages.ElevatorStatusMessage status);

    /// <summary>
    /// 更新楼层呼叫状态缓存
    /// </summary>
    /// <param name="status">新的楼层状态</param>
    void UpdateFloorStatus(Messages.FloorStatusMessage status);
}
