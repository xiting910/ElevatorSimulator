namespace ElevatorSimulator.Server.Core.MessageHandlers;

/// <summary>
/// 取消内部呼叫消息处理器
/// </summary>
public sealed class CancelInternalCallHandler : Interfaces.IMessageHandler<Messages.CancelInternalCallMessage>
{
    /// <inheritdoc />
    void Interfaces.IMessageHandler.Handle(Messages.Message msg, Interfaces.IElevatorManager elevatorManager) => Handle((Messages.CancelInternalCallMessage)msg, elevatorManager);

    /// <inheritdoc />
    public void Handle(Messages.CancelInternalCallMessage msg, Interfaces.IElevatorManager elevatorManager) => elevatorManager.CancelElevatorCall(msg.ElevatorId, msg.TargetFloor);
}
