using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElevatorSimulator.Client.ViewModels;

/// <summary>
/// 主视图模型, 封装客户端管理器的交互逻辑, 作为 UI 层与通信层之间的桥梁
/// </summary>
public sealed partial class MainViewModel : Interfaces.IMainViewModel
{
    /// <inheritdoc/>
    public event Action? ConnectionStateChanged;

    /// <inheritdoc/>
    public event Action? FloorPanelStateChanged;

    /// <inheritdoc/>
    public event Action? ElevatorPanelStateChanged;

    /// <inheritdoc/>
    public event Action<Enums.PanelType>? PanelSwitchRequested;

    /// <inheritdoc/>
    public string ClientId => State.ClientId;

    /// <inheritdoc/>
    public Core.Interfaces.IClientState State { get; }

    /// <inheritdoc/>
    public Core.Interfaces.IMessageService Sender { get; }

    /// <inheritdoc/>
    public Enums.ConnectionStatus Status { get; private set; } = Enums.ConnectionStatus.Connecting;

    /// <summary>
    /// 连接控制器
    /// </summary>
    private readonly Core.Interfaces.IConnectionService _connection;

    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<MainViewModel> _logger;

    /// <summary>
    /// 连接开关是否处于开启状态, 仅由用户通过 UI 设置
    /// </summary>
    private bool _isConnectionEnabled = true;

    /// <summary>
    /// 当前的服务端地址
    /// </summary>
    private string _serverAddress = Constants.DefaultServerAddress;

    /// <summary>
    /// 当前的服务端端口
    /// </summary>
    private int _serverPort = Constants.DefaultServerPort;

    /// <summary>
    /// 防抖取消令牌, 用于在用户连续输入时取消上一次的连接触发
    /// </summary>
    private CancellationTokenSource? _debounceCts;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connection">连接控制器</param>
    /// <param name="sender">消息发送器</param>
    /// <param name="state">状态查询器</param>
    /// <param name="logger">日志记录器</param>
    public MainViewModel(Core.Interfaces.IConnectionService connection, Core.Interfaces.IMessageService sender, Core.Interfaces.IClientState state, ILogger<MainViewModel> logger)
    {
        // 保存依赖的服务实例
        _connection = connection;
        Sender = sender;
        State = state;
        _logger = logger;

        // 订阅连接和状态更新事件
        _connection.OnConnected += OnConnected;
        State.OnElevatorStatusUpdated += OnElevatorStatusReceived;
        State.OnFloorStatusUpdated += OnFloorStatusReceived;
        _connection.OnDisconnected += OnDisconnected;
    }

    /// <inheritdoc/>
    public void OnServerInputChanged(string address, int port)
    {
        // 更新当前的服务端地址和端口
        _serverAddress = address;
        _serverPort = port;

        // 如果当前已经连接或连接功能被禁用, 则不执行连接操作
        if (Status is Enums.ConnectionStatus.Connected || !_isConnectionEnabled) { return; }

        // 执行防抖连接, 取消之前的连接触发并创建新的取消令牌
        _debounceCts?.Cancel();
        _debounceCts = new();
        var token = _debounceCts.Token;

        // 启动一个延迟任务, 在延迟结束后如果没有被取消则执行连接操作
        _ = Task.Delay(800, token).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested)
            {
                _connection.Connect(_serverAddress, _serverPort);
            }
        }, token);
    }

    /// <inheritdoc/>
    public void SetConnectionEnabled(bool enabled)
    {
        // 更新连接开关状态
        _isConnectionEnabled = enabled;

        // 如果连接被启用
        if (_isConnectionEnabled)
        {
            // 设置状态为连接中
            Status = Enums.ConnectionStatus.Connecting;

            // 立即触发连接操作
            _connection.Connect(_serverAddress, _serverPort);
        }
        // 如果连接被禁用
        else
        {
            // 设置状态为已关闭
            Status = Enums.ConnectionStatus.Closed;

            // 断开当前连接
            _connection.Disconnect();
        }

        // 触发连接状态改变事件以刷新 UI
        ConnectionStateChanged?.Invoke();
    }

    /// <inheritdoc/>
    public void ConfirmFloor(int floor)
    {
        State.CurrentFloor = floor;
        PanelSwitchRequested?.Invoke(Enums.PanelType.Floor);
    }

    /// <inheritdoc/>
    public void EnterElevator(int elevatorId)
    {
        if (!State.CanEnterElevator(elevatorId)) { return; }

        State.CurrentElevatorId = elevatorId;
        PanelSwitchRequested?.Invoke(Enums.PanelType.Elevator);
    }

    /// <inheritdoc/>
    public void ExitElevator()
    {
        if (State.CurrentElevatorId is not int elevatorId || !State.CanExitElevator()) { return; }

        State.CurrentFloor = State.ElevatorStatuses[elevatorId].CurrentFloor;
        State.CurrentElevatorId = null;
        PanelSwitchRequested?.Invoke(Enums.PanelType.Floor);
    }

    /// <inheritdoc/>
    public async Task RequestDoorOpen()
    {
        if (State.CurrentElevatorId is int eid)
        {
            await Sender.SendAsync(new Messages.RequestDoorOpenMessage { ElevatorId = eid });
        }
    }

    /// <inheritdoc/>
    public async Task RequestDoorClose()
    {
        if (State.CurrentElevatorId is int eid)
        {
            await Sender.SendAsync(new Messages.RequestDoorCloseMessage { ElevatorId = eid });
        }
    }

    /// <inheritdoc/>
    public void GoToWelcome() => PanelSwitchRequested?.Invoke(Enums.PanelType.Welcome);

    /// <inheritdoc/>
    public void Dispose()
    {
        _connection.OnConnected -= OnConnected;
        State.OnElevatorStatusUpdated -= OnElevatorStatusReceived;
        State.OnFloorStatusUpdated -= OnFloorStatusReceived;
        _connection.OnDisconnected -= OnDisconnected;
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 连接成功时的回调
    /// </summary>
    private void OnConnected()
    {
        Status = Enums.ConnectionStatus.Connected;
        LogConnectionStateChange(_logger, Status);
        ConnectionStateChanged?.Invoke();
    }

    /// <summary>
    /// 电梯状态更新时的回调
    /// </summary>
    /// <param name="msg">新的电梯状态消息</param>
    private void OnElevatorStatusReceived(Messages.ElevatorStatusMessage msg)
    {
        FloorPanelStateChanged?.Invoke();
        ElevatorPanelStateChanged?.Invoke();
    }

    /// <summary>
    /// 楼层状态更新时的回调
    /// </summary>
    /// <param name="msg">新的楼层状态消息</param>
    private void OnFloorStatusReceived(Messages.FloorStatusMessage msg) => FloorPanelStateChanged?.Invoke();

    /// <summary>
    /// 连接断开时的回调
    /// </summary>
    private void OnDisconnected()
    {
        Status = Enums.ConnectionStatus.Reconnecting;
        LogReconnecting(_logger, Status, _serverAddress, _serverPort);
        State.CurrentElevatorId = null;
        PanelSwitchRequested?.Invoke(Enums.PanelType.Welcome);
        if (_isConnectionEnabled) { _connection.Connect(_serverAddress, _serverPort); }
        ConnectionStateChanged?.Invoke();
    }
}
