using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace ElevatorSimulator.Share.Logging;

/// <summary>
/// 通用日志记录器, 实现 <see cref="ILogger"/>, 格式化后通过委托输出
/// </summary>
/// <param name="categoryName">日志类别名称</param>
/// <param name="includeCategory">是否在日志中包含类别名称</param>
/// <param name="isEnabled">判断指定日志级别是否启用</param>
/// <param name="write">格式化后的日志消息输出动作</param>
public sealed class CustomLogger(string categoryName, bool includeCategory, Func<LogLevel, bool> isEnabled, Action<string> write) : ILogger
{
    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
    {
        return isEnabled(logLevel);
    }

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // 如果日志级别未启用, 则直接返回
        if (!IsEnabled(logLevel)) { return; }

        // 日志级别字符串映射
        var levelStr = logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            _ => "NONE"
        };

        // 构建单行日志
        var sb = new StringBuilder()
            .Append(DateTime.Now.ToString("HH:mm:ss"))
            .Append(" [").Append(levelStr).Append(']');

        // 如果需要在日志中包含类别名称
        if (includeCategory)
        {
            _ = sb.Append(" [").Append(categoryName).Append(']');
        }

        _ = sb.Append(' ').Append(formatter(state, exception));

        // 如果有异常, 追加异常信息
        if (exception is not null)
        {
            _ = sb.AppendLine().Append(exception);
        }

        // 通过委托输出
        write(sb.ToString());
    }
}
