using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ElevatorSimulator.Server.Core.Networking;

/// <summary>
/// TCP 服务端管理器, 负责监听客户端连接, 处理消息以及管理连接状态
/// </summary>
/// <param name="elevatorManager">电梯调度管理器</param>
/// <param name="logger">日志记录器</param>
/// <param name="streamMessenger">流消息传输器</param>
/// <param name="messageRouter">消息路由器</param>
public sealed partial class TcpServerManager(Interfaces.IElevatorManager elevatorManager, ILogger<TcpServerManager> logger, Share.Interfaces.IStreamMessenger streamMessenger, MessageRouter messageRouter) : Interfaces.IServerNetworkService
{
    /// <summary>
    /// 连接超时的时间阈值
    /// </summary>
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 心跳检查的定时间隔
    /// </summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// 客户端出站队列容量
    /// </summary>
    private const int OutgoingChannelCapacity = 32;

    /// <summary>
    /// 强制断开后的黑名单冷却时间, 单位为秒
    /// </summary>
    public const int BlacklistDurationSeconds = 5;

    /// <summary>
    /// 客户端上下文, 封装 TCP 连接和专属发送队列
    /// </summary>
    private sealed class ClientContext
    {
        /// <summary>
        /// TCP 客户端
        /// </summary>
        public TcpClient TcpClient { get; }

        /// <summary>
        /// 网络流, 用于收发数据
        /// </summary>
        public NetworkStream Stream { get; }

        /// <summary>
        /// 出站消息通道
        /// </summary>
        public Channel<Messages.Message> Outgoing { get; }

        /// <summary>
        /// 最近一次收到心跳的 UTC 时间, 由心跳检查定时器读取以判定是否超时
        /// </summary>
        public DateTime LastHeartbeatUtc { get; private set; }

        /// <summary>
        /// 更新心跳时间戳为当前 UTC 时间
        /// </summary>
        public void UpdateHeartbeat()
        {
            LastHeartbeatUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tcpClient">TCP 客户端</param>
        public ClientContext(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
            Stream = tcpClient.GetStream();
            LastHeartbeatUtc = DateTime.UtcNow;
            Outgoing = Channel.CreateBounded<Messages.Message>(new BoundedChannelOptions(OutgoingChannelCapacity)
            {
                // 队列满时丢弃最旧消息, 确保实时性
                FullMode = BoundedChannelFullMode.DropOldest
            });
        }
    }

    /// <summary>
    /// TCP 监听器
    /// </summary>
    private readonly TcpListener _listener = new(IPAddress.Any, Constants.DefaultServerPort);

    /// <summary>
    /// 服务端的取消令牌源, 用于在停止服务时通知所有相关任务退出
    /// </summary>
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// 电梯管理器
    /// </summary>
    private readonly Interfaces.IElevatorManager _elevatorManager = elevatorManager;

    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<TcpServerManager> _logger = logger;

    /// <summary>
    /// 流消息传输器
    /// </summary>
    private readonly Share.Interfaces.IStreamMessenger _streamMessenger = streamMessenger;

    /// <summary>
    /// 消息路由器
    /// </summary>
    private readonly MessageRouter _messageRouter = messageRouter;

    /// <inheritdoc/>
    public event Action<IEnumerable<string>>? ClientListChanged;

    /// <summary>
    /// 当前所有连接的客户端, 键为客户端 ID , 值为对应的客户端上下文
    /// </summary>
    private readonly ConcurrentDictionary<string, ClientContext> _clients = new();

    /// <summary>
    /// 黑名单字典, 键为客户端 ID, 值为解除封禁的时间
    /// </summary>
    private readonly ConcurrentDictionary<string, DateTime> _blacklist = new();

    /// <inheritdoc/>
    public void Start()
    {
        _listener.Start();
        _ = AcceptClientsAsync(_cts.Token);
        _ = HeartbeatCheckLoopAsync(_cts.Token);
        LogServerStarted(_logger, Constants.DefaultServerPort);
    }

    /// <summary>
    /// 异步接受客户端连接的主循环, 等待客户端发送身份消息后, 用其自报的 ID 完成注册
    /// </summary>
    /// <param name="token">取消令牌, 用于在服务停止时退出循环</param>
    private async Task AcceptClientsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                // 异步等待客户端连接
                var tcpClient = await _listener.AcceptTcpClientAsync(token).ConfigureAwait(false);

                // 获取客户端远程 IP 地址, 用于日志记录
                var remoteIp = tcpClient.Client.RemoteEndPoint is IPEndPoint ep ? ep.Address.ToString() : "unknown";

                // 连接超时令牌
                using var handshakeCts = new CancellationTokenSource(ConnectionTimeout);

                // 创建一个链接了服务取消令牌和连接超时令牌的联合令牌, 用于等待客户端发送身份消息
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, handshakeCts.Token);

                // 等待客户端发送第一条消息
                var firstMsg = await _streamMessenger.ReceiveAsync(tcpClient.GetStream(), linkedCts.Token).ConfigureAwait(false);

                // 如果第一条消息不是有效的身份消息, 则拒绝连接
                if (firstMsg is not Messages.ClientIdentityMessage identityMsg || string.IsNullOrWhiteSpace(identityMsg.ClientId))
                {
                    tcpClient.Dispose();
                    LogNoIdentity(_logger, remoteIp);
                    continue;
                }

                // 获取客户端 ID
                var clientId = identityMsg.ClientId;

                // 检查该客户端 ID 是否在黑名单中, 如果在且未过期则拒绝连接
                if (_blacklist.TryGetValue(clientId, out var bannedUntil) && DateTime.UtcNow < bannedUntil)
                {
                    tcpClient.Dispose();
                    LogBlacklistReject(_logger, clientId, remoteIp);
                    continue;
                }

                // 封禁期已过, 从黑名单中移除
                _ = _blacklist.TryRemove(clientId, out _);

                // 检查客户端 ID 是否已存在, 如果已存在则拒绝连接
                if (_clients.ContainsKey(clientId))
                {
                    tcpClient.Dispose();
                    LogDuplicateId(_logger, clientId, remoteIp);
                    continue;
                }

                // 创建客户端上下文并注册到客户端字典中
                var ctx = new ClientContext(tcpClient);
                _ = _clients.TryAdd(clientId, ctx);

                // 记录日志并通知订阅者客户端列表已更新
                LogClientConnected(_logger, clientId, remoteIp);
                ClientListChanged?.Invoke(_clients.Keys);

                // 启动处理该客户端消息的任务和专属发送任务, 并发送当前状态快照
                _ = HandleClientAsync(clientId, remoteIp, ctx, token);
                _ = ClientWriterLoopAsync(clientId, ctx, token);
                SendInitialStatus(clientId, ctx);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                LogAcceptError(_logger, ex.Message);
            }
        }
    }

    /// <summary>
    /// 处理单个客户端连接的消息循环, 持续读取网络流数据, 直到连接断开或者服务停止
    /// </summary>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="remoteIp">客户端远程 IP</param>
    /// <param name="ctx">客户端上下文</param>
    /// <param name="token">取消令牌, 用于在服务停止时退出循环</param>
    private async Task HandleClientAsync(string clientId, string remoteIp, ClientContext ctx, CancellationToken token)
    {
        try
        {
            while (ctx.TcpClient.Connected && !token.IsCancellationRequested)
            {
                // 从网络流中异步读取消息
                var msg = await _streamMessenger.ReceiveAsync(ctx.Stream, token).ConfigureAwait(false);

                // null 意味着连接被切断或数据不完整, 退出接收循环
                if (msg is null) { break; }

                // 心跳消息仅更新时间戳, 不进入业务处理
                if (msg is Messages.HeartbeatMessage)
                {
                    ctx.UpdateHeartbeat();
                    continue;
                }

                // 处理接收到的消息, 根据消息类型执行相应的业务逻辑
                _messageRouter.Route(msg, _elevatorManager);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            // 记录日志但不抛出, 确保连接能被正常清理
            LogHandleError(_logger, clientId, ex.Message);
        }
        finally
        {
            // 标记出站队列完成
            _ = ctx.Outgoing.Writer.TryComplete();

            // 从客户端字典中移除该客户端
            if (_clients.TryRemove(clientId, out _))
            {
                // 释放资源
                ctx.Stream.Dispose();
                ctx.TcpClient.Dispose();

                // 记录日志并通知订阅者客户端列表已更新
                LogClientDisconnected(_logger, clientId, remoteIp);
                ClientListChanged?.Invoke(_clients.Keys);
            }
        }
    }

    /// <summary>
    /// 客户端专属发送任务, 单线程串行消费出站队列, 逐条写入网络流
    /// </summary>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="ctx">客户端上下文</param>
    /// <param name="token">取消令牌</param>
    private async Task ClientWriterLoopAsync(string clientId, ClientContext ctx, CancellationToken token)
    {
        try
        {
            await foreach (var msg in ctx.Outgoing.Reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                await _streamMessenger.SendAsync(ctx.Stream, msg, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogSendError(_logger, clientId, ex.Message);
        }
    }

    /// <summary>
    /// 向新连接的客户端发送当前完整状态快照
    /// </summary>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="ctx">客户端上下文</param>
    private void SendInitialStatus(string clientId, ClientContext ctx)
    {
        // 写入所有电梯的当前状态到出站队列
        foreach (var elevator in _elevatorManager.GetCurrentStates())
        {
            if (!ctx.Outgoing.Writer.TryWrite(new Messages.ElevatorStatusMessage
            {
                Id = elevator.Id,
                CurrentFloor = elevator.CurrentFloor,
                MovingDirection = elevator.MovingDirection,
                Door = elevator.Door,
                DoorOpenRatio = elevator.DoorOpenRatio,
                InternalCalls = elevator.InternalCalls
            }))
            {
                LogSendElevatorStatusFail(_logger, clientId);
            }
        }

        // 写入当前楼层呼叫状态到出站队列
        if (!ctx.Outgoing.Writer.TryWrite(new Messages.FloorStatusMessage
        {
            ActiveCalls = _elevatorManager.FloorCallState.ActiveCalls
        }))
        {
            LogSendFloorStatusFail(_logger, clientId);
        }
    }

    /// <summary>
    /// 心跳检查循环, 定期扫描所有客户端, 主动断开那些超时未发送心跳的客户端
    /// </summary>
    /// <param name="token">取消令牌, 用于在服务停止时退出循环</param>
    private async Task HeartbeatCheckLoopAsync(CancellationToken token)
    {
        // 使用 PeriodicTimer 实现定时循环, 每隔一定时间检查一次所有客户端的心跳状态
        using var timer = new PeriodicTimer(CheckInterval);

        // 每次定时器触发时扫描所有客户端, 找出心跳超时的客户端并断开连接, 直到服务停止
        while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
        {
            // 计算心跳超时的时间阈值, 任何最后一次心跳时间早于该阈值的客户端都将被视为断线
            var timeoutThreshold = DateTime.UtcNow.AddSeconds(-Constants.HeartbeatTimeoutSec);

            // 是否需要更新 UI 的客户端列表
            var needUpdateUI = false;

            // 扫描所有客户端, 找出心跳超时的客户端并断开连接
            foreach (var (clientId, ctx) in _clients)
            {
                if (ctx.LastHeartbeatUtc < timeoutThreshold)
                {
                    if (_clients.TryRemove(clientId, out _))
                    {
                        _ = ctx.Outgoing.Writer.TryComplete();
                        ctx.Stream.Dispose();
                        ctx.TcpClient.Dispose();
                        needUpdateUI = true;
                        LogHeartbeatTimeout(_logger, clientId);
                    }
                }
            }

            // 如果有客户端被断开, 则通知订阅者客户端列表已更新
            if (needUpdateUI)
            {
                ClientListChanged?.Invoke(_clients.Keys);
            }
        }
    }

    /// <inheritdoc/>
    public void BroadcastElevatorStatus(Messages.ElevatorStatusMessage elevatorStatus)
    {
        foreach (var (clientId, ctx) in _clients)
        {
            if (!ctx.Outgoing.Writer.TryWrite(elevatorStatus))
            {
                LogBroadcastElevatorFail(_logger, clientId);
            }
        }
    }

    /// <inheritdoc/>
    public void BroadcastFloorCallStatus(Messages.FloorStatusMessage floorStatus)
    {
        foreach (var (clientId, ctx) in _clients)
        {
            if (!ctx.Outgoing.Writer.TryWrite(floorStatus))
            {
                LogBroadcastFloorFail(_logger, clientId);
            }
        }
    }

    /// <inheritdoc/>
    public void DisconnectClient(string clientId)
    {
        if (_clients.TryRemove(clientId, out var ctx))
        {
            // 设置封禁时间
            var bannedUntilUtc = DateTime.UtcNow.AddSeconds(BlacklistDurationSeconds);

            // 将客户端 ID 和封禁时间添加到黑名单字典中
            _blacklist[clientId] = bannedUntilUtc;

            // 通知订阅者客户端列表已更新
            ClientListChanged?.Invoke(_clients.Keys);

            // 记录日志
            LogForceDisconnect(_logger, clientId, BlacklistDurationSeconds, bannedUntilUtc.ToLocalTime().ToString("T"));

            // 关闭连接并释放资源
            _ = ctx.Outgoing.Writer.TryComplete();
            ctx.Stream.Dispose();
            ctx.TcpClient.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_cts.IsCancellationRequested) { _cts.Cancel(); }

        _listener.Stop();

        foreach (var ctx in _clients.Values)
        {
            _ = ctx.Outgoing.Writer.TryComplete();
            ctx.Stream.Dispose();
            ctx.TcpClient.Dispose();
        }

        _clients.Clear();
        _cts.Dispose();

        GC.SuppressFinalize(this);
    }
}
