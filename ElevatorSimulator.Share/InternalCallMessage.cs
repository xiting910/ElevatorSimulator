namespace ElevatorSimulator.Share;

/// <summary>
/// 内部呼叫消息类, 表示电梯内部的呼叫请求
/// </summary>
public sealed class InternalCallMessage : Message
{
    /// <summary>
    /// 电梯 ID
    /// </summary>
    public int ElevatorId { get; init; }

    /// <summary>
    /// 呼叫的目标楼层
    /// </summary>
    public int TargetFloor { get; init; }
}
