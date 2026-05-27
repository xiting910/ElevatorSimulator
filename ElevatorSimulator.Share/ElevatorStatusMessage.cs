namespace ElevatorSimulator.Share;

/// <summary>
/// 电梯状态消息类, 表示电梯当前的状态信息
/// </summary>
public sealed class ElevatorStatusMessage : Message
{
    /// <summary>
    /// 电梯 ID
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 电梯当前所在的楼层
    /// </summary>
    public int CurrentFloor { get; init; }

    /// <summary>
    /// 电梯当前的移动方向
    /// </summary>
    public Direction MovingDirection { get; init; }

    /// <summary>
    /// 电梯门的状态
    /// </summary>
    public DoorState Door { get; init; }

    /// <summary>
    /// 电梯门的打开比例
    /// </summary>
    public double DoorOpenRatio { get; init; }

    /// <summary>
    /// 电梯内部的呼叫请求列表
    /// </summary>
    public int[] InternalCalls { get; init; } = [];
}
