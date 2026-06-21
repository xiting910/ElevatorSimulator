using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElevatorSimulator.Client.Core;

/// <summary>
/// 消息服务, 负责向服务端发送业务消息
/// </summary>
/// <param name="messenger">流消息传输器</param>
/// <param name="streamAccessor">网络流访问器, 按需获取当前连接的网络流</param>
/// <param name="logger">日志记录器</param>
public sealed partial class MessageService(Share.Interfaces.IStreamMessenger messenger, Interfaces.IStreamAccessor streamAccessor, ILogger<MessageService> logger) : Interfaces.IMessageService
{
    /// <summary>
    /// 流消息传输器, 负责将消息对象序列化并通过网络流发送
    /// </summary>
    private readonly Share.Interfaces.IStreamMessenger _messenger = messenger;

    /// <summary>
    /// 网络流访问器, 按需获取当前连接的网络流, 以便在发送消息时使用
    /// </summary>
    private readonly Interfaces.IStreamAccessor _streamAccessor = streamAccessor;

    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<MessageService> _logger = logger;

    /// <inheritdoc/>
    public async Task SendAsync<TMessage>(TMessage message) where TMessage : Messages.Message
    {
        var s = _streamAccessor.Stream;
        if (s is null) { return; }
        try { await _messenger.SendAsync(s, message, CancellationToken.None).ConfigureAwait(false); }
        catch (Exception ex) { LogSendFailed(_logger, ex.Message); }
    }
}
