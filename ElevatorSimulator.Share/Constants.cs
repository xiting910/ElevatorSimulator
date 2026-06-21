using System;
using System.IO;

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
    /// 程序名称
    /// </summary>
    public const string AppName = "ElevatorSimulator";

    /// <summary>
    /// 日志文件根目录, 位于 AppData/Roaming/ElevatorSimulator
    /// </summary>
    public static readonly string LogBaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);

    /// <summary>
    /// TCP 客户端默认连接的服务端地址
    /// </summary>
    public const string DefaultServerAddress = "127.0.0.1";

    /// <summary>
    /// TCP 服务端监听的默认端口
    /// </summary>
    public const int DefaultServerPort = 8888;

    /// <summary>
    /// 心跳消息的发送间隔, 单位为秒
    /// </summary>
    public const int HeartbeatIntervalSec = 4;

    /// <summary>
    /// 心跳超时时间, 超过此时间未收到心跳视为客户端已断线, 单位为秒
    /// </summary>
    public const int HeartbeatTimeoutSec = 3 * HeartbeatIntervalSec;

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
    public const int MaxFloor = 33;

    /// <summary>
    /// 电梯每层之间的行驶时间, 单位为秒
    /// </summary>
    public const int FloorTravelTimeSec = 3;

    /// <summary>
    /// 电梯门开关时间, 单位为秒
    /// </summary>
    public const int DoorOpenCloseTimeSec = 2;

    /// <summary>
    /// 门完全打开后等待的时间, 单位为秒
    /// </summary>
    public const int DoorOpenWaitTimeSec = 4;

    /// <summary>
    /// 电梯状态计时器更新的时间间隔, 单位为毫秒
    /// </summary>
    public const int UpdateInterval = 250;
}
