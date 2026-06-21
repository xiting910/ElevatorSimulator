namespace ElevatorSimulator.Server.Core.MessageHandlers;

/// <summary>
/// 关门请求消息处理器
/// </summary>
public sealed class RequestDoorCloseHandler : Interfaces.IMessageHandler<Messages.RequestDoorCloseMessage>
{
    /// <inheritdoc />
    void Interfaces.IMessageHandler.Handle(Messages.Message msg, Interfaces.IElevatorManager elevatorManager) => Handle((Messages.RequestDoorCloseMessage)msg, elevatorManager);

    /// <inheritdoc />
    public void Handle(Messages.RequestDoorCloseMessage msg, Interfaces.IElevatorManager elevatorManager) => elevatorManager.RequestDoorClose(msg.ElevatorId);
}
