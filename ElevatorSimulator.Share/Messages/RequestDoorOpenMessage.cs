namespace ElevatorSimulator.Share.Messages;

/// <summary>
/// 请求开门消息
/// </summary>
public sealed class RequestDoorOpenMessage : Message
{
    /// <summary>
    /// 电梯 ID
    /// </summary>
    public int ElevatorId { get; init; }
}
