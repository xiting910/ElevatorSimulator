using System;
using System.Text.Json.Serialization;

namespace ElevatorSimulator.Share;

/// <summary>
/// 抽象消息类
/// </summary>
[JsonDerivedType(typeof(ExternalCallMessage), "external")]
[JsonDerivedType(typeof(InternalCallMessage), "internal")]
[JsonDerivedType(typeof(CancelExternalCallMessage), "cancel_external")]
[JsonDerivedType(typeof(CancelInternalCallMessage), "cancel_internal")]
[JsonDerivedType(typeof(FloorStatusMessage), "floor_status")]
[JsonDerivedType(typeof(ElevatorStatusMessage), "elevator_status")]
public abstract class Message
{
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
