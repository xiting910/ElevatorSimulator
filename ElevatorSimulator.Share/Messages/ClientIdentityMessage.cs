namespace ElevatorSimulator.Share.Messages;

/// <summary>
/// 客户端身份消息, 连接建立后由客户端首先发送, 声明自己的唯一标识
/// </summary>
public sealed class ClientIdentityMessage : Message
{
    /// <summary>
    /// 客户端唯一 ID
    /// </summary>
    public string ClientId { get; init; } = string.Empty;
}
