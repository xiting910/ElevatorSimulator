using System;
using System.Threading.Tasks;

namespace ElevatorSimulator.Client.ViewModels.Interfaces;

/// <summary>
/// Client 端主视图模型接口, 用于依赖注入和单元测试 mock, 遵循接口隔离原则
/// </summary>
public interface IMainViewModel : IDisposable
{
    /// <summary>
    /// 连接状态变化事件, 当连接状态改变时触发
    /// </summary>
    event Action? ConnectionStateChanged;

    /// <summary>
    /// 楼层等待面板状态变化事件
    /// </summary>
    event Action? FloorPanelStateChanged;

    /// <summary>
    /// 电梯内部面板状态变化事件
    /// </summary>
    event Action? ElevatorPanelStateChanged;

    /// <summary>
    /// 面板切换请求事件
    /// </summary>
    event Action<Enums.PanelType>? PanelSwitchRequested;

    /// <summary>
    /// 客户端唯一标识
    /// </summary>
    string ClientId { get; }

    /// <summary>
    /// 客户端状态, 供视图读取缓存数据
    /// </summary>
    Core.Interfaces.IClientState State { get; }

    /// <summary>
    /// 消息服务, 供视图发送指令
    /// </summary>
    Core.Interfaces.IMessageService Sender { get; }

    /// <summary>
    /// 当前连接状态
    /// </summary>
    Enums.ConnectionStatus Status { get; }

    /// <summary>
    /// 服务端地址或端口变化时的防抖重连
    /// </summary>
    /// <param name="address">服务端地址</param>
    /// <param name="port">服务端端口</param>
    void OnServerInputChanged(string address, int port);

    /// <summary>
    /// 设置连接开关状态
    /// </summary>
    /// <param name="enabled">是否开启连接</param>
    void SetConnectionEnabled(bool enabled);

    /// <summary>
    /// 确认进入指定楼层
    /// </summary>
    /// <param name="floor">楼层号</param>
    void ConfirmFloor(int floor);

    /// <summary>
    /// 进入指定电梯
    /// </summary>
    /// <param name="elevatorId">电梯 ID</param>
    void EnterElevator(int elevatorId);

    /// <summary>
    /// 退出当前电梯, 返回楼层等待界面
    /// </summary>
    void ExitElevator();

    /// <summary>
    /// 请求打开当前电梯的门
    /// </summary>
    Task RequestDoorOpen();

    /// <summary>
    /// 请求关闭当前电梯的门
    /// </summary>
    Task RequestDoorClose();

    /// <summary>
    /// 返回欢迎界面
    /// </summary>
    void GoToWelcome();
}
