namespace ElevatorSimulator.Share.Messages;

/// <summary>
/// 外部呼叫消息类, 表示某一层的电梯呼叫请求
/// </summary>
public sealed class ExternalCallMessage : Message
{
    /// <summary>
    /// 呼叫的楼层
    /// </summary>
    public int Floor { get; init; }

    /// <summary>
    /// 呼叫的方向
    /// </summary>
    public Enums.Direction Direction { get; init; }
}
