using System;

namespace ElevatorSimulator.Share.Messages;

/// <summary>
/// 抽象消息类, JSON 多态序列化的子类型通过 <see cref="StreamMessenger"/> 中反射自动发现
/// </summary>
public abstract class Message
{
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
