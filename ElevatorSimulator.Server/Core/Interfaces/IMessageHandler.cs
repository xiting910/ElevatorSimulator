namespace ElevatorSimulator.Server.Core.Interfaces;

/// <summary>
/// 消息处理器基接口, 作为 DI 集合注入和统一调用的锚点
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// 处理接收到的消息
    /// </summary>
    /// <param name="msg">接收到的消息</param>
    /// <param name="elevatorManager">电梯管理器, 用于调用调度逻辑</param>
    void Handle(Messages.Message msg, IElevatorManager elevatorManager);
}
