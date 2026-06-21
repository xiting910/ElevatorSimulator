using Microsoft.Extensions.Logging;

namespace ElevatorSimulator.Client.Core;

// 客户端消息服务的日志记录部分
public sealed partial class MessageService
{
    /// <summary>
    /// 记录发送消息失败的警告
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="error">异常消息</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "发送消息失败: {Error}")]
    private static partial void LogSendFailed(ILogger logger, string error);
}
