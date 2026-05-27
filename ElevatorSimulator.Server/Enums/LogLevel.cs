using System.ComponentModel;

namespace ElevatorSimulator.Server.Enums;

/// <summary>
/// 日志过滤级别枚举
/// </summary>
internal enum LogLevel
{
    [Description("信息 (Info)")]
    Info = 0,

    [Description("警告 (Warn)")]
    Warn = 1,

    [Description("错误 (Error)")]
    Error = 2
}
