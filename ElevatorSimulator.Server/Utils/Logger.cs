using ElevatorSimulator.Server.Enums;

namespace ElevatorSimulator.Server.Utils;

/// <summary>
/// 日志处理模块
/// </summary>
internal static class Logger
{
    /// <summary>
    /// 当前日志过滤级别, 低于该级别的日志将被忽略
    /// </summary>
    public static LogLevel CurrentLevel { get; set; }

    /// <summary>
    /// 记录信息级别的日志
    /// </summary>
    /// <param name="message">日志内容</param>
    public static void Info(string message) => Log(LogLevel.Info, message);

    /// <summary>
    /// 记录警告级别的日志
    /// </summary>
    /// <param name="message">日志内容</param>
    public static void Warn(string message) => Log(LogLevel.Warn, message);

    /// <summary>
    /// 记录错误级别的日志
    /// </summary>
    /// <param name="message">日志内容</param>
    public static void Error(string message) => Log(LogLevel.Error, message);

    /// <summary>
    /// 记录日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志内容</param>
    private static void Log(LogLevel level, string message)
    {
        if (level < CurrentLevel) { return; }

        var prefix = level switch
        {
            LogLevel.Info => "[INFO]",
            LogLevel.Warn => "[WARN]",
            LogLevel.Error => "[ERROR]",
            _ => "[Unknown]"
        };

        UI.MainForm.Instance.Log($"{prefix} {message}");
    }
}
