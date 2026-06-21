using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ElevatorSimulator.Share.Interfaces;

/// <summary>
/// 基于 <see cref="Stream"/> 的消息传输接口
/// </summary>
public interface IStreamMessenger
{
    /// <summary>
    /// 异步向流中写入一条消息
    /// </summary>
    /// <param name="stream">目标流</param>
    /// <param name="msg">要发送的消息对象</param>
    /// <param name="token">取消令牌</param>
    Task SendAsync(Stream stream, Messages.Message msg, CancellationToken token);

    /// <summary>
    /// 异步从流中读取一条消息
    /// </summary>
    /// <param name="stream">来源流</param>
    /// <param name="token">取消令牌</param>
    /// <returns>读取到的消息对象, 读取失败或流关闭时返回 <see langword="null"/></returns>
    Task<Messages.Message?> ReceiveAsync(Stream stream, CancellationToken token);
}
