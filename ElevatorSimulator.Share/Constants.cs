namespace ElevatorSimulator.Share;

/// <summary>
/// 常量类, 包含程序中使用的各种常量
/// </summary>
public static class Constants
{
    /// <summary>
    /// 作者
    /// </summary>
    public const string Author = "xiting910";

    /// <summary>
    /// 命名管道的名称
    /// </summary>
    public const string PipeName = "ElevatorSimulatorPipe";

    /// <summary>
    /// 电梯的数量
    /// </summary>
    public const int ElevatorCount = 3;

    /// <summary>
    /// 电梯的最小楼层
    /// </summary>
    public const int MinFloor = -2;

    /// <summary>
    /// 电梯的最大楼层
    /// </summary>
    public const int MaxFloor = 32;

    /// <summary>
    /// 电梯每层之间的行驶时间, 单位为秒
    /// </summary>
    public const int FloorTravelTimeSec = 5;

    /// <summary>
    /// 电梯门开关时间, 单位为秒
    /// </summary>
    public const int DoorOpenCloseTimeSec = 3;
}
