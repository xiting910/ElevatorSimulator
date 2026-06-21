namespace ElevatorSimulator.Share.Messages;

/// <summary>
/// 请求关门消息
/// </summary>
public sealed class RequestDoorCloseMessage : Message
{
    /// <summary>
    /// 电梯 ID
    /// </summary>
    public int ElevatorId { get; init; }
}
