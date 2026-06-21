using System.Threading.Tasks;

namespace ElevatorSimulator.Client.Core.Interfaces;

/// <summary>
/// 消息服务接口, 负责向服务端发送业务消息
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// 向服务端发送一条业务消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型, 必须继承自 <see cref="Messages.Message"/></typeparam>
    /// <param name="message">要发送的消息实例</param>
    Task SendAsync<TMessage>(TMessage message) where TMessage : Messages.Message;
}
