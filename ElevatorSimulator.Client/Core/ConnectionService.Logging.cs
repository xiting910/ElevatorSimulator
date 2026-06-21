using Microsoft.Extensions.Logging;

namespace ElevatorSimulator.Client.Core;

// 客户端连接服务的日志记录部分
public sealed partial class ConnectionService
{
    /// <summary>
    /// 记录开始连接服务端的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="address">服务端地址</param>
    /// <param name="port">服务端端口</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "开始连接 {Address}:{Port}")]
    private static partial void LogConnecting(ILogger logger, string address, int port);

    /// <summary>
    /// 记录连接失败等待重试的信息
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="retryDelay">重试延迟, 单位为秒</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "连接失败, {RetryDelay} 秒后重试...")]
    private static partial void LogConnectRetry(ILogger logger, double retryDelay);

    /// <summary>
    /// 记录成功连接到服务端的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "已成功连接到服务端, 开始接收消息")]
    private static partial void LogConnected(ILogger logger);

    /// <summary>
    /// 记录与服务端断开连接的信息
    /// </summary>
    /// <param name="logger">日志记录器</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "与服务端的连接已断开")]
    private static partial void LogDisconnected(ILogger logger);

    /// <summary>
    /// 记录 TCP 连接异常的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="error">异常消息</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "TCP 连接异常: {Error}")]
    private static partial void LogConnectException(ILogger logger, string error);

    /// <summary>
    /// 记录发送身份消息失败的警告
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="error">异常消息</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "发送身份消息失败: {Error}")]
    private static partial void LogSendIdentityFailed(ILogger logger, string error);

    /// <summary>
    /// 记录接收循环任务异常退出的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="error">异常消息</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "接收循环任务异常退出: {Error}")]
    private static partial void LogReceiveLoopError(ILogger logger, string error);

    /// <summary>
    /// 记录心跳循环任务异常退出的警告
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="error">异常消息</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "心跳循环任务异常退出: {Error}")]
    private static partial void LogHeartbeatLoopError(ILogger logger, string error);

    /// <summary>
    /// 记录连接生命周期异常的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="error">异常消息</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "连接生命周期异常: {Error}")]
    private static partial void LogTryConnectError(ILogger logger, string error);

    /// <summary>
    /// 记录消息接收循环异常的警告
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="error">异常消息</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "消息接收循环异常: {Error}")]
    private static partial void LogReceiveLoopFailed(ILogger logger, string error);

    /// <summary>
    /// 记录心跳发送异常的警告
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="error">异常消息</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "心跳发送异常: {Error}")]
    private static partial void LogHeartbeatSendFailed(ILogger logger, string error);
}
