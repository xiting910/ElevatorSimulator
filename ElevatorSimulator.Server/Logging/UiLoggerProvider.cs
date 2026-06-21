using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace ElevatorSimulator.Server.Logging;

/// <summary>
/// UI 日志提供程序, 实现 <see cref="ILoggerProvider"/>, 将日志通过事件推送到 UI 层显示
/// </summary>
public sealed class UiLoggerProvider : ILoggerProvider, IDisposable
{
    /// <summary>
    /// 日志记录器缓存, 键为类别名称
    /// </summary>
    private readonly ConcurrentDictionary<string, Share.Logging.CustomLogger> _loggers = new();

    /// <summary>
    /// 当有新的日志消息产生时触发, 参数为格式化后的日志消息
    /// </summary>
    public event Action<string>? LogReceived;

    /// <summary>
    /// 最低日志级别, 低于此级别的日志将被忽略, 可在运行时动态调整以实现 UI 过滤
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, new Share.Logging.CustomLogger(categoryName, false, level => level >= MinimumLevel, RaiseLogReceived));

    /// <summary>
    /// 触发日志接收事件
    /// </summary>
    /// <param name="message">格式化后的日志消息</param>
    private void RaiseLogReceived(string message) => LogReceived?.Invoke(message);

    /// <inheritdoc/>
    public void Dispose()
    {
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}
