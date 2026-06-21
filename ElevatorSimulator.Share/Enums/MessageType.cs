namespace ElevatorSimulator.Share.Enums;

/// <summary>
/// 消息类型枚举, 用于 JSON 多态序列化的类型鉴别器 (int 值)
/// </summary>
public enum MessageType
{
    /// <summary> 客户端身份消息 </summary>
    clientIdentity = 0,

    /// <summary> 心跳 </summary>
    Heartbeat = 1,

    /// <summary> 外部呼叫 </summary>
    ExternalCall = 2,

    /// <summary> 内部呼叫 </summary>
    InternalCall = 3,

    /// <summary> 取消外部呼叫 </summary>
    CancelExternalCall = 4,

    /// <summary> 取消内部呼叫 </summary>
    CancelInternalCall = 5,

    /// <summary> 请求开门 </summary>
    RequestDoorOpen = 6,

    /// <summary> 请求关门 </summary>
    RequestDoorClose = 7,

    /// <summary> 楼层状态 </summary>
    FloorStatus = 100,

    /// <summary> 电梯状态 </summary>
    ElevatorStatus = 101
}
