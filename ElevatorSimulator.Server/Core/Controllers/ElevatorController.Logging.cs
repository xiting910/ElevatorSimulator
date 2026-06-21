using Microsoft.Extensions.Logging;

namespace ElevatorSimulator.Server.Core.Controllers;

// 电梯控制器的日志记录部分
public sealed partial class ElevatorController
{
    /// <summary>
    /// 记录电梯完成内部呼叫的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="elevatorId">电梯 ID</param>
    /// <param name="floor">当前楼层</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "电梯 {ElevatorId} 已完成 {Floor} 楼内部呼叫")]
    private static partial void LogInternalCallCompleted(ILogger logger, int elevatorId, int floor);

    /// <summary>
    /// 记录电梯完成外部呼叫的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="elevatorId">电梯 ID</param>
    /// <param name="floor">当前楼层</param>
    /// <param name="direction">运行方向</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "电梯 {ElevatorId} 已完成 {Floor} 楼 {Direction} 方向外部呼叫")]
    private static partial void LogExternalCallCompleted(ILogger logger, int elevatorId, int floor, Direction direction);
}
