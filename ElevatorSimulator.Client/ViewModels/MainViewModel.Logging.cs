using ElevatorSimulator.Client.Enums;
using Microsoft.Extensions.Logging;

namespace ElevatorSimulator.Client.ViewModels;

// 客户端主视图模型的日志记录部分
public sealed partial class MainViewModel
{
    /// <summary>
    /// 记录连接状态变更的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="status">当前的连接状态</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "连接状态变更: {Status}")]
    private static partial void LogConnectionStateChange(ILogger logger, ConnectionStatus status);

    /// <summary>
    /// 记录连接断开并尝试重连的信息
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="status">当前的连接状态</param>
    /// <param name="address">重连的目标地址</param>
    /// <param name="port">重连的目标端口</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "连接断开, 状态变更: {Status}, 尝试重连 {Address}:{Port}")]
    private static partial void LogReconnecting(ILogger logger, ConnectionStatus status, string address, int port);
}
