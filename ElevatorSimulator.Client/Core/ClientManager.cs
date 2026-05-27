using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ElevatorSimulator.Client.Core;

/// <summary>
/// 客户端管理器, 负责与服务器通信, 采用单例模式
/// </summary>
internal sealed class ClientManager : IDisposable
{
    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static ClientManager Instance => _lazyInstance.Value;

    /// <summary>
    /// 单例实例的延迟初始化, 确保在第一次访问时才创建实例并启动服务
    /// </summary>
    private static readonly Lazy<ClientManager> _lazyInstance = new(() => new());

    /// <summary>
    /// 等待连接的超时时间
    /// </summary>
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 连接重试的延迟时间
    /// </summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 客户端管道通信对象
    /// </summary>
    private readonly NamedPipeClientStream _pipeClient;

    /// <summary>
    /// 连接服务端的任务
    /// </summary>
    private Task _connectTask;

    /// <summary>
    /// 当前所在楼层
    /// </summary>
    public int CurrentFloor { get; set; }

    /// <summary>
    /// 是否已连接服务端
    /// </summary>
    public bool IsConnected => _pipeClient.IsConnected;

    /// <summary>
    /// 电梯状态更新事件
    /// </summary>
    public event Action<Share.ElevatorStatusMessage>? OnElevatorStatusReceived;

    /// <summary>
    /// 楼层状态更新事件
    /// </summary>
    public event Action<Share.FloorStatusMessage>? OnFloorStatusReceived;

    /// <summary>
    /// 连接断开事件
    /// </summary>
    public event Action? OnDisconnected;

    /// <summary>
    /// 私有构造函数, 防止外部实例化
    /// </summary>
    private ClientManager()
    {
        _pipeClient = new(".", Share.Constants.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        _connectTask = ConnectAsync(default);
    }

    /// <summary>
    /// 循环尝试连接服务器, 直到成功连接或取消请求
    /// </summary>
    private async Task ConnectAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await _pipeClient.ConnectAsync(ConnectTimeout, token);
                if (_pipeClient.IsConnected)
                {
                    _ = ReceiveLoopAsync(token);
                    break;
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception) { }

            await Task.Delay(RetryDelay, token);
        }
    }

    /// <summary>
    /// 接收循环, 持续监听服务器消息并触发相应事件
    /// </summary>
    /// <param name="token">取消令牌, 用于停止接收循环</param>
    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && _pipeClient.IsConnected)
            {
                var msg = await Share.NamedPipeMessenger.ReceiveAsync(_pipeClient, token);
                if (msg is null) { continue; }

                if (msg is Share.ElevatorStatusMessage elevatorStatus)
                {
                    OnElevatorStatusReceived?.Invoke(elevatorStatus);
                }
                else if (msg is Share.FloorStatusMessage floorStatus)
                {
                    OnFloorStatusReceived?.Invoke(floorStatus);
                }
            }
        }
        catch (Exception) { }
        finally
        {
            Disconnect();
        }
    }

    /// <summary>
    /// 取消连接但不立即释放资源, 以允许重新连接
    /// </summary>
    private void Disconnect()
    {
        // 如果当前有正在连接的任务, 则取消该任务
        if (!_connectTask.IsCompleted)
        {
            try { _connectTask.Wait(); }
            catch (AggregateException) { }
        }

        // 如果当前管道连接中, 则关闭连接并触发断开事件
        if (_pipeClient.IsConnected)
        {
            _pipeClient.Close();
            OnDisconnected?.Invoke();
        }
    }

    /// <summary>
    /// 重新连接服务器, 取消当前连接并启动新的连接任务
    /// </summary>
    public void Reconnect(CancellationToken token)
    {
        Disconnect();
        _connectTask = ConnectAsync(token);
    }

    /// <summary>
    /// 异步发送外部呼叫消息
    /// </summary>
    /// <param name="direction">呼叫方向</param>
    public async Task SendExternalCallAsync(Share.Direction direction)
    {
        if (!_pipeClient.IsConnected) { return; }
        var msg = new Share.ExternalCallMessage
        {
            Floor = CurrentFloor,
            Direction = direction
        };
        await Share.NamedPipeMessenger.SendAsync(_pipeClient, msg);
    }

    /// <summary>
    /// 异步发送内部呼叫消息
    /// </summary>
    /// <param name="elevatorId">电梯 ID</param>
    /// <param name="targetFloor">目标楼层</param>
    public async Task SendInternalCallAsync(int elevatorId, int targetFloor)
    {
        if (!_pipeClient.IsConnected) { return; }
        var msg = new Share.InternalCallMessage
        {
            ElevatorId = elevatorId,
            TargetFloor = targetFloor
        };
        await Share.NamedPipeMessenger.SendAsync(_pipeClient, msg);
    }

    /// <summary>
    /// 异步发送取消外部呼叫消息
    /// </summary>
    /// <param name="direction">呼叫方向</param>
    public async Task SendCancelExternalCallAsync(Share.Direction direction)
    {
        if (!_pipeClient.IsConnected) { return; }
        var msg = new Share.CancelExternalCallMessage
        {
            Floor = CurrentFloor,
            Direction = direction
        };
        await Share.NamedPipeMessenger.SendAsync(_pipeClient, msg);
    }

    /// <summary>
    /// 异步发送取消内部呼叫消息
    /// </summary>
    /// <param name="elevatorId">电梯 ID</param>
    /// <param name="targetFloor">目标楼层</param>
    public async Task SendCancelInternalCallAsync(int elevatorId, int targetFloor)
    {
        if (!_pipeClient.IsConnected) { return; }
        var msg = new Share.CancelInternalCallMessage
        {
            ElevatorId = elevatorId,
            TargetFloor = targetFloor
        };
        await Share.NamedPipeMessenger.SendAsync(_pipeClient, msg);
    }

    /// <summary>
    /// 释放引用的资源
    /// </summary>
    public void Dispose()
    {
        _pipeClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
