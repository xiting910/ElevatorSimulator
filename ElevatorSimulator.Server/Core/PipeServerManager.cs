using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ElevatorSimulator.Server.Core;

/// <summary>
/// 命名管道服务端管理器, 采用单例模式, 负责监听客户端连接、处理消息以及管理连接状态
/// </summary>
internal sealed class PipeServerManager : IDisposable
{
    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static PipeServerManager Instance => _lazyInstance.Value;

    /// <summary>
    /// 单例实例的延迟初始化, 确保在第一次访问时才创建实例并启动服务
    /// </summary>
    private static readonly Lazy<PipeServerManager> _lazyInstance = new(() => new());

    /// <summary>
    /// 管道服务端的取消令牌源, 用于在停止服务时通知所有相关任务退出
    /// </summary>
    private readonly CancellationTokenSource _cts;

    /// <summary>
    /// 当前所有连接的管道, 键为客户端标识, 值为对应的命名管道流对象
    /// </summary>
    private readonly ConcurrentDictionary<string, NamedPipeServerStream> _clients;

    /// <summary>
    /// 私有构造函数
    /// </summary>
    private PipeServerManager()
    {
        _cts = new();
        _clients = new();
        _ = AcceptClientsAsync(_cts.Token);
    }

    /// <summary>
    /// 异步接受客户端连接的主循环, 每当有新的连接请求时创建一个新的命名管道服务端实例并分配一个独立的任务来处理该连接
    /// </summary>
    /// <param name="token">取消令牌, 用于在服务停止时退出循环</param>
    private async Task AcceptClientsAsync(CancellationToken token)
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                // 创建一个新的命名管道服务端实例
                var pipeServer = new NamedPipeServerStream(
                    Share.Constants.PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                // 等待客户端连接, 该方法会异步阻塞直到有客户端连接或者取消令牌被触发
                await pipeServer.WaitForConnectionAsync(token);

                // 生成短标识以便于管理
                var clientId = Guid.NewGuid().ToString("N")[..8];

                // 将新连接添加到连接集合中
                _ = _clients.TryAdd(clientId, pipeServer);

                // 记录日志
                Utils.Logger.Info($"客户端已连接: {clientId}");

                // 更新 UI 显示当前连接的客户端列表
                UI.MainForm.Instance.UpdateClients(_clients.Keys);

                // 分配任务独立处理该管道的数据读取
                _ = HandleClientAsync(clientId, pipeServer, token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Utils.Logger.Error($"接受客户端连接时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 处理单个客户端连接的消息循环, 持续读取该管道的数据, 直到连接断开或者服务停止
    /// </summary>
    /// <param name="clientId">客户端标识</param>
    /// <param name="pipe">对应的命名管道流对象</param>
    /// <param name="token">取消令牌, 用于在服务停止时退出循环</param>
    private async Task HandleClientAsync(string clientId, NamedPipeServerStream pipe, CancellationToken token)
    {
        try
        {
            while (pipe.IsConnected && !token.IsCancellationRequested)
            {
                // 从管道中异步读取消息, 该方法会根据协议解析完整的消息对象, 如果连接断开或者数据不完整则返回 null
                var msg = await Share.NamedPipeMessenger.ReceiveAsync(pipe, token);

                // null 意味着连接被切断或者数据不完整包
                if (msg is null) { continue; }

                // 处理接收到的消息, 根据消息类型执行相应的业务逻辑
                ProcessMessage(msg);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Utils.Logger.Error($"处理客户端 {clientId} 消息时出错: {ex.Message}");
        }
        finally
        {
            if (_clients.TryRemove(clientId, out _))
            {
                pipe.Dispose();
                Utils.Logger.Info($"客户端已断开: {clientId}");
                UI.MainForm.Instance.UpdateClients(_clients.Keys);
            }
        }
    }

    /// <summary>
    /// 处理来自特定客户端的消息
    /// </summary>
    /// <param name="msg">接收到的消息</param>
    private static void ProcessMessage(Share.Message msg)
    {
        switch (msg)
        {
            case Share.ExternalCallMessage ext:
                ElevatorManager.Instance.AddFloorCall(ext.Floor, ext.Direction);
                break;

            case Share.InternalCallMessage internalCall:
                ElevatorManager.Instance.AddElevatorCall(internalCall.ElevatorId, internalCall.TargetFloor);
                break;

            case Share.CancelExternalCallMessage cancelExt:
                ElevatorManager.Instance.CancelFloorCall(cancelExt.Floor, cancelExt.Direction);
                break;

            case Share.CancelInternalCallMessage cancelInt:
                ElevatorManager.Instance.CancelElevatorCall(cancelInt.ElevatorId, cancelInt.TargetFloor);
                break;

            default:
                Utils.Logger.Warn($"收到未知类型的消息: {msg.GetType().Name}");
                break;
        }
    }

    /// <summary>
    /// 安全地向客户端发送消息, 捕获并记录任何发送过程中发生的异常
    /// </summary>
    /// <param name="clientId">客户端标识</param>
    /// <param name="pipe">对应的命名管道流对象</param>
    /// <param name="msg">要发送的消息对象</param>
    /// <param name="token">取消令牌, 用于在服务停止时取消发送操作</param>
    private static async Task SafeSendAsync(string clientId, NamedPipeServerStream pipe, Share.Message msg, CancellationToken token)
    {
        try
        {
            await Share.NamedPipeMessenger.SendAsync(pipe, msg, token);
        }
        catch (Exception ex)
        {
            Utils.Logger.Warn($"向客户端 {clientId} 广播失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 向所有客户端广播电梯状态
    /// </summary>
    /// <param name="elevatorStatus">电梯的状态</param>
    public async Task BroadcastElevatorStatusAsync(Share.ElevatorStatusMessage elevatorStatus)
    {
        var tasks = new List<Task>();
        foreach (var (clientId, pipe) in _clients)
        {
            if (pipe.IsConnected)
            {
                tasks.Add(SafeSendAsync(clientId, pipe, elevatorStatus, _cts.Token));
            }
        }
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 向所有客户端广播楼层呼叫状态
    /// </summary>
    /// <param name="floorStatus">当前所有楼层的呼叫状态</param>
    public async Task BroadcastFloorCallStatusAsync(Share.FloorStatusMessage floorStatus)
    {
        var tasks = new List<Task>();
        foreach (var (clientId, pipe) in _clients)
        {
            if (pipe.IsConnected)
            {
                tasks.Add(SafeSendAsync(clientId, pipe, floorStatus, _cts.Token));
            }
        }
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 主动断开某个客户端的连接
    /// </summary>
    public void DisconnectClient(string clientId)
    {
        if (_clients.TryRemove(clientId, out var pipe))
        {
            pipe.Dispose();
            Utils.Logger.Warn($"已强制断开客户端: {clientId}");
            UI.MainForm.Instance.UpdateClients(_clients.Keys);
        }
    }

    /// <summary>
    /// 停止服务并释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }

        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

        _clients.Clear();
        _cts.Dispose();

        GC.SuppressFinalize(this);
    }
}
