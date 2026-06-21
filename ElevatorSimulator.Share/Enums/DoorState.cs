namespace ElevatorSimulator.Share.Enums;

/// <summary>
/// 电梯门状态枚举
/// </summary>
public enum DoorState
{
    /// <summary> 关 </summary>
    Closed,

    /// <summary> 开 </summary>
    Open,

    /// <summary> 正在关闭 </summary>
    Closing,

    /// <summary> 正在开启 </summary>
    Opening
}
