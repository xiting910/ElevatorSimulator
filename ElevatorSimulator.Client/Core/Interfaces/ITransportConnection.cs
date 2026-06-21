using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ElevatorSimulator.Client.Core.Interfaces;

/// <summary>
/// 传输层连接抽象
/// </summary>
public interface ITransportConnection : IDisposable
{
    /// <summary>
    /// 是否已连接到远程端点
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 获取网络流, 用于收发数据
    /// </summary>
    /// <returns>网络流, 如果未连接则返回 <see langword="null"/></returns>
    Stream? GetStream();

    /// <summary>
    /// 异步连接到远程端点
    /// </summary>
    /// <param name="serverAddress">服务器地址</param>
    /// <param name="serverPort">服务器端口</param>
    /// <param name="token">取消令牌</param>
    Task ConnectAsync(string serverAddress, int serverPort, CancellationToken token);

    /// <summary>
    /// 关闭连接
    /// </summary>
    void Close();
}
