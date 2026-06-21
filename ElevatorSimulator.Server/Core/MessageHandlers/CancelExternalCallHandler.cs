namespace ElevatorSimulator.Server.Core.MessageHandlers;

/// <summary>
/// 取消外部呼叫消息处理器
/// </summary>
public sealed class CancelExternalCallHandler : Interfaces.IMessageHandler<Messages.CancelExternalCallMessage>
{
    /// <inheritdoc />
    void Interfaces.IMessageHandler.Handle(Messages.Message msg, Interfaces.IElevatorManager elevatorManager) => Handle((Messages.CancelExternalCallMessage)msg, elevatorManager);

    /// <inheritdoc />
    public void Handle(Messages.CancelExternalCallMessage msg, Interfaces.IElevatorManager elevatorManager) => elevatorManager.CancelFloorCall(msg.Floor, msg.Direction);
}
