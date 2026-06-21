namespace ElevatorSimulator.Share.Messages;

/// <summary>
/// 取消外部呼叫消息
/// </summary>
public sealed class CancelExternalCallMessage : Message
{
    /// <summary>
    /// 取消呼叫的楼层
    /// </summary>
    public int Floor { get; init; }

    /// <summary>
    /// 取消呼叫的方向
    /// </summary>
    public Enums.Direction Direction { get; init; }
}
