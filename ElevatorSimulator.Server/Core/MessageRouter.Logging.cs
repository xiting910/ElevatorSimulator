using Microsoft.Extensions.Logging;

namespace ElevatorSimulator.Server.Core;

// 消息路由器的日志记录部分
public sealed partial class MessageRouter
{
    /// <summary>
    /// 记录收到未知类型消息的警告
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="messageType">未知消息的类型名称</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "收到未知类型的消息: {MessageType}")]
    private static partial void LogUnknownMessage(ILogger logger, string messageType);
}
