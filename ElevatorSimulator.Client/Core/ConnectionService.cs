using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ElevatorSimulator.Client.Core;

/// <summary>
/// 连接服务, 实现 <see cref="Interfaces.IConnectionService"/>
/// </summary>
/// <param name="transportFactory">传输连接工厂</param>
/// <param name="messenger">消息收发器</param>
/// <param name="state">客户端状态存储</param>
/// <param name="logger">日志记录器</param>
public sealed partial class ConnectionService(Func<Interfaces.ITransportConnection> transportFactory, Share.Interfaces.IStreamMessenger messenger, Interfaces.IClientState state, ILogger<ConnectionService> logger) : Interfaces.IConnectionService
{
    /// <summary>
    /// 连接重试延迟
    /// </summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 连接生命周期的取消令牌源
    /// </summary>
    private CancellationTokenSource _cts = new();

    /// <summary>
    /// 连接工厂
    /// </summary>
    private readonly Func<Interfaces.ITransportConnection> _transportFactory = transportFactory;

    /// <summary>
    /// 消息收发器
    /// </summary>
    private readonly Share.Interfaces.IStreamMessenger _messenger = messenger;

    /// <summary>
    /// 客户端状态存储
    /// </summary>
    private readonly Interfaces.IClientState _state = state;

    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<ConnectionService> _logger = logger;

    /// <summary>
    /// 消息类型到处理器的映射字典
    /// </summary>
    private Dictionary<Type, Action<Messages.Message>> MessageHandlers => field ??= new()
    {
        [typeof(Messages.ElevatorStatusMessage)] = msg => _state.UpdateElevatorStatus((Messages.ElevatorStatusMessage)msg),
        [typeof(Messages.FloorStatusMessage)] = msg => _state.UpdateFloorStatus((Messages.FloorStatusMessage)msg),
    };

    /// <inheritdoc/>
    public event Action? OnConnected;

    /// <inheritdoc/>
    public event Action? OnDisconnected;

    /// <inheritdoc/>
    public System.IO.Stream? Stream { get; private set; }

    /// <inheritdoc/>
    public void Connect(string serverAddress, int serverPort)
    {
        Disconnect();
        _ = ConnectLoopAsync(serverAddress, serverPort, _cts.Token);
    }

    /// <inheritdoc/>
    public void Disconnect()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new();
        Stream = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    /// <summary>
    /// 连接重试循环
    /// </summary>
    /// <param name="serverAddress">服务端地址</param>
    /// <param name="serverPort">服务端端口</param>
    /// <param name="token">取消令牌</param>
    private async Task ConnectLoopAsync(string serverAddress, int serverPort, CancellationToken token)
    {
        LogConnecting(_logger, serverAddress, serverPort);
        while (!token.IsCancellationRequested)
        {
            // 创建传输连接实例, 使用工厂方法按需获取
            using var transport = _transportFactory();

            // 尝试一次完整的连接生命周期, 如果成功则退出循环
            if (await TryConnectAsync(serverAddress, serverPort, transport, token).ConfigureAwait(false)) { break; }

            // 失败后等待一段时间后重试
            LogConnectRetry(_logger, RetryDelay.TotalSeconds);
            try { await Task.Delay(RetryDelay, token).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>
    /// 一次完整的连接生命周期
    /// </summary>
    /// <param name="serverAddress">服务端地址</param>
    /// <param name="serverPort">服务端端口</param>
    /// <param name="t">传输连接</param>
    /// <param name="token">取消令牌</param>
    /// <returns><see langword="true"/> 如果成功连接并接收过消息, 否则返回 <see langword="false"/></returns>
    private async Task<bool> TryConnectAsync(string serverAddress, int serverPort, Interfaces.ITransportConnection t, CancellationToken token)
    {
        // 尝试连接, 出现异常直接返回失败
        try { await t.ConnectAsync(serverAddress, serverPort, token).ConfigureAwait(false); }
        catch (OperationCanceledException) { return false; }
        catch (Exception ex)
        {
            LogConnectException(_logger, ex.Message);
            return false;
        }

        // 如果连接未成功建立, 直接返回失败
        if (!t.IsConnected) { return false; }

        // 获取网络流, 如果为 null 则返回失败
        Stream = t.GetStream();
        if (Stream is null) { return false; }

        // 发送身份消息
        try
        {
            await _messenger.SendAsync(Stream, new Messages.ClientIdentityMessage { ClientId = _state.ClientId }, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Stream = null;
            return false;
        }
        catch (Exception ex)
        {
            // 发送失败则断开连接并返回失败
            LogSendIdentityFailed(_logger, ex.Message);
            Stream = null;
            return false;
        }

        // 是否成功接收过消息的标志
        var hadConnected = false;
        try
        {
            // 创建一个链接的取消令牌源, 用于在任一循环结束时取消另一个循环
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);

            // 启动消息接收循环任务
            var recv = ReceiveLoopAsync(t, linked.Token);

            // 启动心跳发送循环任务
            var hb = HeartbeatLoopAsync(t, linked.Token);

            // 等待任一循环结束
            _ = await Task.WhenAny(recv, hb).ConfigureAwait(false);

            // 取消另一个循环
            linked.Cancel();

            // 等待接收循环完成并设置连接成功标志
            try { hadConnected = await recv.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { LogReceiveLoopError(_logger, ex.Message); }

            // 等待心跳循环完成
            try { await hb.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { LogHeartbeatLoopError(_logger, ex.Message); }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogTryConnectError(_logger, ex.Message);
        }
        finally
        {
            // 当之前成功连接过且当前未被取消时触发断开事件
            if (!token.IsCancellationRequested && hadConnected)
            {
                LogDisconnected(_logger);
                OnDisconnected?.Invoke();
            }

            // 确保连接断开时网络流被清理
            Stream = null;
        }

        // 返回是否成功连接并接收过消息的结果
        return hadConnected;
    }

    /// <summary>
    /// 消息接收循环, 更新 ClientState
    /// </summary>
    /// <param name="transport">传输连接</param>
    /// <param name="token">取消令牌</param>
    /// <returns><see langword="true"/> 如果成功接收过消息, 否则返回 <see langword="false"/></returns>
    private async Task<bool> ReceiveLoopAsync(Interfaces.ITransportConnection transport, CancellationToken token)
    {
        // 是否成功接收过消息的标志
        var hadReceived = false;
        try
        {
            // 循环接收消息, 直到连接断开或取消
            while (!token.IsCancellationRequested && transport.IsConnected && Stream is not null)
            {
                // 接收消息
                var msg = await _messenger.ReceiveAsync(Stream, token).ConfigureAwait(false);
                if (msg is null) { break; }

                // 首次成功接收消息时触发连接成功事件
                if (!hadReceived)
                {
                    hadReceived = true;
                    LogConnected(_logger);
                    OnConnected?.Invoke();
                }

                // 通过映射字典分派消息
                if (MessageHandlers.TryGetValue(msg.GetType(), out var handler))
                {
                    handler(msg);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogReceiveLoopFailed(_logger, ex.Message);
        }

        // 返回是否成功接收过消息的结果
        return hadReceived;
    }

    /// <summary>
    /// 心跳发送循环
    /// </summary>
    /// <param name="transport">传输连接</param>
    /// <param name="token">取消令牌</param>
    private async Task HeartbeatLoopAsync(Interfaces.ITransportConnection transport, CancellationToken token)
    {
        // 使用 PeriodicTimer 实现定时心跳, 连接期间每隔固定时间发送一次心跳消息, 直到连接断开或取消
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Constants.HeartbeatIntervalSec));

        // 每次定时器触发时检查连接状态和网络流, 如果有效则发送心跳消息, 否则退出循环
        while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
        {
            if (!transport.IsConnected || Stream is null) { break; }
            try { await _messenger.SendAsync(Stream, new Messages.HeartbeatMessage(), token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                LogHeartbeatSendFailed(_logger, ex.Message);
                break;
            }
        }
    }
}
