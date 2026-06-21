using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ElevatorSimulator.Server.ViewModels.Interfaces;

/// <summary>
/// 主视图模型接口
/// </summary>
public interface IMainViewModel : IDisposable
{
    /// <summary>
    /// 电梯状态更新事件, 当电梯状态发生变化时触发
    /// </summary>
    event Action<IEnumerable<Models.Interfaces.IElevatorState>>? ElevatorStatusChanged;

    /// <summary>
    /// 楼层呼叫状态更新事件, 当楼层呼叫状态发生变化时触发
    /// </summary>
    event Action<Dictionary<int, Direction[]>>? FloorCallsChanged;

    /// <summary>
    /// 客户端列表更新事件, 当客户端连接或断开时触发
    /// </summary>
    event Action<IEnumerable<string>>? ClientListChanged;

    /// <summary>
    /// 日志消息事件, 当有新的日志消息产生时触发
    /// </summary>
    event Action<string>? LogReceived;

    /// <summary>
    /// 启动服务
    /// </summary>
    void Start();

    /// <summary>
    /// 强制断开指定客户端连接, 并加入黑名单
    /// </summary>
    /// <param name="clientId">客户端 ID</param>
    void DisconnectClient(string clientId);

    /// <summary>
    /// 设置日志过滤级别
    /// </summary>
    /// <param name="level">日志级别</param>
    void SetLogLevel(LogLevel level);
}
