namespace ElevatorSimulator.Server.Core.MessageHandlers;

/// <summary>
/// 外部呼叫消息处理器
/// </summary>
public sealed class ExternalCallHandler : Interfaces.IMessageHandler<Messages.ExternalCallMessage>
{
    /// <inheritdoc />
    void Interfaces.IMessageHandler.Handle(Messages.Message msg, Interfaces.IElevatorManager elevatorManager)
    {
        Handle((Messages.ExternalCallMessage)msg, elevatorManager);
    }

    /// <inheritdoc />
    public void Handle(Messages.ExternalCallMessage msg, Interfaces.IElevatorManager elevatorManager)
    {
        elevatorManager.AddFloorCall(msg.Floor, msg.Direction);
    }
}
