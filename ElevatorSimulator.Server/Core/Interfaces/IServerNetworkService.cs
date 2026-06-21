using System;
using System.Collections.Generic;

namespace ElevatorSimulator.Server.Core.Interfaces;

/// <summary>
/// 服务端网络服务接口
/// </summary>
public interface IServerNetworkService : IDisposable
{
    /// <summary>
    /// 客户端列表更新事件, 当客户端连接或断开时触发
    /// </summary>
    event Action<IEnumerable<string>>? ClientListChanged;

    /// <summary>
    /// 启动服务端并开始监听客户端连接
    /// </summary>
    void Start();

    /// <summary>
    /// 向所有客户端广播电梯状态
    /// </summary>
    /// <param name="elevatorStatus">电梯的状态</param>
    void BroadcastElevatorStatus(Messages.ElevatorStatusMessage elevatorStatus);

    /// <summary>
    /// 向所有客户端广播楼层呼叫状态
    /// </summary>
    /// <param name="floorStatus">当前所有楼层的呼叫状态</param>
    void BroadcastFloorCallStatus(Messages.FloorStatusMessage floorStatus);

    /// <summary>
    /// 主动断开某个客户端的连接, 并将其客户端 ID 加入黑名单
    /// </summary>
    /// <param name="clientId">客户端 ID (客户端自己声明的持久化标识)</param>
    void DisconnectClient(string clientId);
}
