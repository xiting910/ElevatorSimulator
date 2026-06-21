namespace ElevatorSimulator.Client.Enums;

/// <summary>
/// 客户端连接状态枚举
/// </summary>
public enum ConnectionStatus
{
    /// <summary> 正在连接中 </summary>
    Connecting = 0,

    /// <summary> 已成功连接到服务端 </summary>
    Connected = 1,

    /// <summary> 连接断开后正在自动重连 </summary>
    Reconnecting = 2,

    /// <summary> 用户已关闭连接 </summary>
    Closed = 3
}
