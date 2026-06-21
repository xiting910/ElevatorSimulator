namespace ElevatorSimulator.Share.Messages;

/// <summary>
/// 心跳消息, 客户端定期发送以表明连接存活, 无额外字段
/// </summary>
public sealed class HeartbeatMessage : Message { }
