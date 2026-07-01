namespace ElevatorSimulator.Server.Core.MessageHandlers;

/// <summary>
/// 内部呼叫消息处理器
/// </summary>
public sealed class InternalCallHandler : Interfaces.IMessageHandler<Messages.InternalCallMessage>
{
    /// <inheritdoc />
    void Interfaces.IMessageHandler.Handle(Messages.Message msg, Interfaces.IElevatorManager elevatorManager)
    {
        Handle((Messages.InternalCallMessage)msg, elevatorManager);
    }

    /// <inheritdoc />
    public void Handle(Messages.InternalCallMessage msg, Interfaces.IElevatorManager elevatorManager)
    {
        elevatorManager.AddElevatorCall(msg.ElevatorId, msg.TargetFloor);
    }
}
