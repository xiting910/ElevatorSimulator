using System;

namespace ElevatorSimulator.Client.Core.Interfaces;

/// <summary>
/// 连接服务接口
/// </summary>
public interface IConnectionService : IStreamAccessor, IDisposable
{
    /// <summary>
    /// 连接成功事件
    /// </summary>
    event Action? OnConnected;

    /// <summary>
    /// 连接断开事件
    /// </summary>
    event Action? OnDisconnected;

    /// <summary>
    /// 启动连接到指定服务端
    /// </summary>
    /// <param name="serverAddress">服务端地址</param>
    /// <param name="serverPort">服务端端口</param>
    void Connect(string serverAddress, int serverPort);

    /// <summary>
    /// 断开连接并停止重试
    /// </summary>
    void Disconnect();
}
