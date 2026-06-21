using Microsoft.Extensions.Logging;

namespace ElevatorSimulator.Server.Core.Networking;

// TCP 服务端管理器的日志记录部分
public sealed partial class TcpServerManager
{
    /// <summary>
    /// 记录服务端启动日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="port">监听端口</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "TCP 服务端已启动, 监听端口 {Port}")]
    private static partial void LogServerStarted(ILogger logger, int port);

    /// <summary>
    /// 记录客户端未发送有效身份消息的信息
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="remoteIp">客户端远程 IP</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "客户端未发送有效身份消息, 连接被拒绝 (来自 {RemoteIp})")]
    private static partial void LogNoIdentity(ILogger logger, string remoteIp);

    /// <summary>
    /// 记录拒绝黑名单客户端连接的信息
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="remoteIp">客户端远程 IP</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "拒绝黑名单客户端连接: {ClientId} (来自 {RemoteIp})")]
    private static partial void LogBlacklistReject(ILogger logger, string clientId, string remoteIp);

    /// <summary>
    /// 记录客户端 ID 重复被拒绝的信息
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="remoteIp">客户端远程 IP</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "客户端 ID 重复, 连接被拒绝: {ClientId} (来自 {RemoteIp})")]
    private static partial void LogDuplicateId(ILogger logger, string clientId, string remoteIp);

    /// <summary>
    /// 记录客户端成功连接的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="remoteIp">客户端远程 IP</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "客户端已连接: {ClientId} (来自 {RemoteIp})")]
    private static partial void LogClientConnected(ILogger logger, string clientId, string remoteIp);

    /// <summary>
    /// 记录接受客户端连接时出错的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="error">错误消息</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "接受客户端连接时出错: {Error}")]
    private static partial void LogAcceptError(ILogger logger, string error);

    /// <summary>
    /// 记录处理客户端消息时出错的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="error">错误消息</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "处理客户端 {ClientId} 消息时出错: {Error}")]
    private static partial void LogHandleError(ILogger logger, string clientId, string error);

    /// <summary>
    /// 记录客户端断开连接的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="remoteIp">客户端远程 IP</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "客户端已断开: {ClientId} (来自 {RemoteIp})")]
    private static partial void LogClientDisconnected(ILogger logger, string clientId, string remoteIp);

    /// <summary>
    /// 记录发送消息给客户端时出错的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="error">错误消息</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "发送消息给客户端 {ClientId} 时出错: {Error}")]
    private static partial void LogSendError(ILogger logger, string clientId, string error);

    /// <summary>
    /// 记录向客户端发送电梯状态失败的警告
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientId">客户端 ID</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "向客户端 {ClientId} 发送电梯状态失败")]
    private static partial void LogSendElevatorStatusFail(ILogger logger, string clientId);

    /// <summary>
    /// 记录向客户端发送楼层呼叫状态失败的警告
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientId">客户端 ID</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "向客户端 {ClientId} 发送楼层呼叫状态失败")]
    private static partial void LogSendFloorStatusFail(ILogger logger, string clientId);

    /// <summary>
    /// 记录客户端心跳超时被断开的警告
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientId">客户端 ID</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "客户端心跳超时已断开: {ClientId}")]
    private static partial void LogHeartbeatTimeout(ILogger logger, string clientId);

    /// <summary>
    /// 记录广播电梯状态失败的警告
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientId">客户端 ID</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "向客户端 {ClientId} 广播电梯状态失败")]
    private static partial void LogBroadcastElevatorFail(ILogger logger, string clientId);

    /// <summary>
    /// 记录广播楼层呼叫状态失败的警告
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientId">客户端 ID</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "向客户端 {ClientId} 广播楼层呼叫状态失败")]
    private static partial void LogBroadcastFloorFail(ILogger logger, string clientId);

    /// <summary>
    /// 记录强制断开客户端并封禁的警告
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="duration">封禁时长, 单位为秒</param>
    /// <param name="until">封禁截止时间</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "强制断开客户端: {ClientId}, 封禁 {Duration} 秒直到 {Until}")]
    private static partial void LogForceDisconnect(ILogger logger, string clientId, int duration, string until);
}
