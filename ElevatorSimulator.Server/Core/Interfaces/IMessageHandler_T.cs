namespace ElevatorSimulator.Server.Core.Interfaces;

/// <summary>
/// 泛型消息处理器接口, 提供编译时类型安全, 继承自 <see cref="IMessageHandler"/>
/// </summary>
/// <typeparam name="TMessage">要处理的消息类型, 必须继承自 <see cref="Messages.Message"/></typeparam>
public interface IMessageHandler<TMessage> : IMessageHandler where TMessage : Messages.Message
{
    /// <summary>
    /// 处理接收到的消息
    /// </summary>
    /// <param name="msg">接收到的消息</param>
    /// <param name="elevatorManager">电梯管理器, 用于调用调度逻辑</param>
    void Handle(TMessage msg, IElevatorManager elevatorManager);
}
