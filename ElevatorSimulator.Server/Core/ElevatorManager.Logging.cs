using Microsoft.Extensions.Logging;

namespace ElevatorSimulator.Server.Core;

// 电梯管理器的日志记录部分
public sealed partial class ElevatorManager
{
    /// <summary>
    /// 记录收到楼层外部呼叫并分配电梯的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="floor">呼叫的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    /// <param name="elevatorId">分配的电梯 ID</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "收到楼层呼叫: {Floor} 楼, 方向 {Direction}, 分配给电梯 {ElevatorId}")]
    private static partial void LogAddFloorCall(ILogger logger, int floor, Direction direction, int elevatorId);

    /// <summary>
    /// 记录收到电梯内部呼叫的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="elevatorId">电梯 ID</param>
    /// <param name="targetFloor">目标楼层</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "收到电梯呼叫: 电梯 {ElevatorId}, 目标楼层 {TargetFloor}")]
    private static partial void LogAddElevatorCall(ILogger logger, int elevatorId, int targetFloor);

    /// <summary>
    /// 记录取消楼层呼叫的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="floor">取消的楼层</param>
    /// <param name="direction">取消的方向</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "取消楼层呼叫: {Floor} 楼, 方向 {Direction}")]
    private static partial void LogCancelFloorCall(ILogger logger, int floor, Direction direction);

    /// <summary>
    /// 记录取消电梯内部呼叫的日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="elevatorId">电梯 ID</param>
    /// <param name="targetFloor">目标楼层</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "取消电梯呼叫: 电梯 {ElevatorId}, 目标楼层 {TargetFloor}")]
    private static partial void LogCancelElevatorCall(ILogger logger, int elevatorId, int targetFloor);
}
