namespace ElevatorSimulator.Share;

/// <summary>
/// 取消内部呼叫消息
/// </summary>
public sealed class CancelInternalCallMessage : Message
{
    /// <summary>
    /// 取消呼叫的电梯 ID
    /// </summary>
    public int ElevatorId { get; init; }

    /// <summary>
    /// 取消呼叫的目标楼层
    /// </summary>
    public int TargetFloor { get; init; }
}
