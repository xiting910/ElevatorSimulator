namespace ElevatorSimulator.Server.Core.MessageHandlers;

/// <summary>
/// 开门请求消息处理器
/// </summary>
public sealed class RequestDoorOpenHandler : Interfaces.IMessageHandler<Messages.RequestDoorOpenMessage>
{
    /// <inheritdoc />
    void Interfaces.IMessageHandler.Handle(Messages.Message msg, Interfaces.IElevatorManager elevatorManager) => Handle((Messages.RequestDoorOpenMessage)msg, elevatorManager);

    /// <inheritdoc />
    public void Handle(Messages.RequestDoorOpenMessage msg, Interfaces.IElevatorManager elevatorManager) => elevatorManager.RequestDoorOpen(msg.ElevatorId);
}
