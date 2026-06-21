using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ElevatorSimulator.Server.ViewModels;

/// <summary>
/// 主视图模型, 作为服务层与 UI 层之间的桥梁
/// </summary>
public sealed class MainViewModel : Interfaces.IMainViewModel
{
    /// <inheritdoc/>
    public event Action<IEnumerable<Models.Interfaces.IElevatorState>>? ElevatorStatusChanged;

    /// <inheritdoc/>
    public event Action<Dictionary<int, Direction[]>>? FloorCallsChanged;

    /// <inheritdoc/>
    public event Action<IEnumerable<string>>? ClientListChanged;

    /// <inheritdoc/>
    public event Action<string>? LogReceived;

    /// <summary>
    /// 构造注入的电梯管理器
    /// </summary>
    private readonly Core.Interfaces.IElevatorManager _elevatorManager;

    /// <summary>
    /// 构造注入的服务端网络服务
    /// </summary>
    private readonly Core.Interfaces.IServerNetworkService _networkService;

    /// <summary>
    /// 构造注入的 UI 日志提供程序
    /// </summary>
    private readonly Logging.UiLoggerProvider _uiLoggerProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="elevatorManager">电梯调度管理器</param>
    /// <param name="networkService">服务端网络服务</param>
    /// <param name="uiLoggerProvider">UI 日志提供程序</param>
    public MainViewModel(Core.Interfaces.IElevatorManager elevatorManager, Core.Interfaces.IServerNetworkService networkService, Logging.UiLoggerProvider uiLoggerProvider)
    {
        _elevatorManager = elevatorManager;
        _networkService = networkService;
        _uiLoggerProvider = uiLoggerProvider;
        _uiLoggerProvider.LogReceived += OnLog;
    }

    /// <inheritdoc/>
    public void Start()
    {
        // 订阅电梯管理器事件
        _elevatorManager.ElevatorStatusChanged += OnElevatorStatusChanged;
        _elevatorManager.FloorCallsChanged += OnFloorCallsChanged;

        // 订阅 TCP 服务端管理器事件
        _networkService.ClientListChanged += OnClientListChanged;

        // 初始化电梯管理器
        _elevatorManager.Initialize();

        // 启动 TCP 服务端
        _networkService.Start();
    }

    /// <inheritdoc/>
    public void DisconnectClient(string clientId) => _networkService.DisconnectClient(clientId);

    /// <inheritdoc/>
    public void SetLogLevel(LogLevel level) => _uiLoggerProvider.MinimumLevel = level;

    /// <inheritdoc/>
    public void Dispose()
    {
        // 取消事件订阅
        _networkService.ClientListChanged -= OnClientListChanged;
        _elevatorManager.FloorCallsChanged -= OnFloorCallsChanged;
        _elevatorManager.ElevatorStatusChanged -= OnElevatorStatusChanged;
        _uiLoggerProvider.LogReceived -= OnLog;

        // 停止并释放服务
        _elevatorManager.Dispose();
        _networkService.Dispose();

        // 通知垃圾回收器不再调用终结器
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 电梯状态变化时的回调
    /// </summary>
    /// <param name="elevators">电梯状态数组</param>
    private void OnElevatorStatusChanged(IEnumerable<Models.Interfaces.IElevatorState> elevators) => ElevatorStatusChanged?.Invoke(elevators);

    /// <summary>
    /// 楼层呼叫状态变化时的回调
    /// </summary>
    /// <param name="activeCalls">当前激活的楼层呼叫</param>
    private void OnFloorCallsChanged(Dictionary<int, Direction[]> activeCalls) => FloorCallsChanged?.Invoke(activeCalls);

    /// <summary>
    /// 客户端列表变化时的回调
    /// </summary>
    /// <param name="clientIds">当前连接的客户端 ID 集合</param>
    private void OnClientListChanged(IEnumerable<string> clientIds) => ClientListChanged?.Invoke(clientIds);

    /// <summary>
    /// 日志消息回调
    /// </summary>
    /// <param name="message">格式化后的日志消息</param>
    private void OnLog(string message) => LogReceived?.Invoke(message);
}
