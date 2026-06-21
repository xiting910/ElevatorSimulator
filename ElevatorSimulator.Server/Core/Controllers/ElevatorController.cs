using Microsoft.Extensions.Logging;
using System.Timers;

namespace ElevatorSimulator.Server.Core.Controllers;

/// <summary>
/// 电梯控制器
/// </summary>
public sealed partial class ElevatorController : Interfaces.IElevatorController
{
    /// <inheritdoc/>
    public int Id => State.Id;

    /// <inheritdoc/>
    public Models.Interfaces.IElevatorState State { get; }

    /// <summary> 全局楼层呼叫状态引用 </summary>
    private readonly Models.Interfaces.IFloorCallState _floorCallState;

    /// <summary> 日志记录器 </summary>
    private readonly ILogger<ElevatorController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="timer">用于驱动电梯状态更新的定时器</param>
    /// <param name="elevatorState">电梯状态对象</param>
    /// <param name="floorCallState">全局楼层呼叫状态对象</param>
    /// <param name="logger">日志记录器</param>
    public ElevatorController(Timer timer, Models.Interfaces.IElevatorState elevatorState, Models.Interfaces.IFloorCallState floorCallState, ILogger<ElevatorController> logger)
    {
        State = elevatorState;
        _floorCallState = floorCallState;
        _logger = logger;
        timer.Elapsed += OnTimerElapsed;
    }
}
