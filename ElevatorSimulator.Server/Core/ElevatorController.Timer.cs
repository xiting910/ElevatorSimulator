using System.Timers;

namespace ElevatorSimulator.Server.Core;

/// <summary>
/// 电梯控制器
/// </summary>
internal sealed partial class ElevatorController
{
    /// <summary>
    /// 电梯经过一个楼层所需的时间间隔数
    /// </summary>
    private const int IntervalsPerFloor = Share.Constants.FloorTravelTimeSec * 1000 / Share.Constants.UpdateInterval;

    /// <summary>
    /// 电梯开门后等待的时间间隔数
    /// </summary>
    private const int IntervalsDoorOpen = Share.Constants.DoorOpenWaitTimeSec * 1000 / Share.Constants.UpdateInterval;

    /// <summary>
    /// 每个时间间隔电梯门开关的比例
    /// </summary>
    private const double DoorOpenCloseRatioPerInterval = Share.Constants.DoorOpenCloseTimeSec * 1000.0 / Share.Constants.UpdateInterval;

    /// <summary>
    /// 管理的电梯的 Id
    /// </summary>
    public int ElevatorId { get; }

    /// <summary>
    /// 电梯从上一个楼层开始移动后经过的计时器周期数, 由 <see cref="OnTimerElapsed"/> 方法维护
    /// </summary>
    private int _passedIntervalsSinceLastFloor;

    /// <summary>
    /// 电梯开门后等待的计时器周期数, 由 <see cref="OnTimerElapsed"/> 方法维护
    /// </summary>
    private int _passedIntervalsSinceDoorOpened;

    /// <summary>
    /// 构造函数, 订阅定时器的 Elapsed 事件以定期更新电梯状态
    /// </summary>
    /// <param name="elevatorId">电梯的 Id</param>
    /// <param name="timer">用于更新电梯状态的定时器</param>
    public ElevatorController(int elevatorId, Timer timer)
    {
        ElevatorId = elevatorId;
        timer.Elapsed += OnTimerElapsed;
    }

    /// <summary>
    /// 定时器的 Elapsed 事件处理程序, 定期更新电梯状态以模拟电梯的移动和门操作
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // 获取当前电梯状态
        var elevatorState = ElevatorManager.Instance.Elevators[ElevatorId];

        // 如果电梯正在上行或下行
        if (elevatorState.MovingDirection is Share.Direction.Up or Share.Direction.Down)
        {
            // 经过的时间间隔数加一
            _passedIntervalsSinceLastFloor++;

            // 判断是否到达新楼层
            if (_passedIntervalsSinceLastFloor == IntervalsPerFloor)
            {
                // 到达新楼层, 更新电梯状态和间隔计数器
                elevatorState.CurrentFloor += elevatorState.MovingDirection == Share.Direction.Up ? 1 : -1;
                _passedIntervalsSinceLastFloor = 0;

                // 如果到达了目标楼层, 则停靠并打开电梯门
                lock (_tasksLock)
                {
                    if (elevatorState.CurrentFloor == _currentTargetFloor)
                    {
                        elevatorState.MovingDirection = Share.Direction.None;
                        elevatorState.Door = Share.DoorState.Opening;
                    }
                }
            }
        }
        // 如果电梯当前停靠
        else if (elevatorState.MovingDirection is Share.Direction.None)
        {
            // 如果电梯门已打开, 则等待一段时间后关闭电梯门
            if (elevatorState.Door == Share.DoorState.Open)
            {
                _passedIntervalsSinceDoorOpened++;
                if (_passedIntervalsSinceDoorOpened == IntervalsDoorOpen)
                {
                    _passedIntervalsSinceDoorOpened = 0;
                    elevatorState.Door = Share.DoorState.Closing;
                }
            }
            // 如果电梯门正在打开, 则每次触发定时器事件时增加开门率直到完全打开
            else if (elevatorState.Door == Share.DoorState.Opening)
            {
                elevatorState.DoorOpenRatio += DoorOpenCloseRatioPerInterval;
                if (elevatorState.DoorOpenRatio >= 1)
                {
                    elevatorState.DoorOpenRatio = 1;
                    elevatorState.Door = Share.DoorState.Open;
                    CompleteCurrentFloorTask(elevatorState.CurrentFloor);
                }
            }
            // 如果电梯门正在关闭, 则每次触发定时器事件时减少开门率直到完全关闭
            else if (elevatorState.Door == Share.DoorState.Closing)
            {
                elevatorState.DoorOpenRatio -= DoorOpenCloseRatioPerInterval;
                if (elevatorState.DoorOpenRatio <= 0)
                {
                    elevatorState.DoorOpenRatio = 0;
                    elevatorState.Door = Share.DoorState.Closed;
                }

                // 关门过程时刻检查目标楼层是否变为当前楼层, 如果是则立即停止关门并打开电梯门
                lock (_tasksLock)
                {
                    if (elevatorState.CurrentFloor == _currentTargetFloor)
                    {
                        elevatorState.Door = Share.DoorState.Opening;
                    }
                }
            }
            // 如果电梯门已关闭
            else if (elevatorState.Door == Share.DoorState.Closed)
            {
                // 如果有目标楼层, 则设置电梯方向朝向目标楼层
                lock (_tasksLock)
                {
                    if (_currentTargetFloor is not null)
                    {
                        if (_currentTargetFloor > elevatorState.CurrentFloor)
                        {
                            elevatorState.MovingDirection = Share.Direction.Up;
                        }
                        else if (_currentTargetFloor < elevatorState.CurrentFloor)
                        {
                            elevatorState.MovingDirection = Share.Direction.Down;
                        }
                        else
                        {
                            // 目标楼层就是当前楼层, 则直接打开电梯门
                            elevatorState.Door = Share.DoorState.Opening;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 完成当前楼层的任务, 并更新目标
    /// </summary>
    /// <param name="currentFloor">当前楼层</param>
    private void CompleteCurrentFloorTask(int currentFloor)
    {
        lock (_tasksLock)
        {
            // 移除当前楼层的内部任务
            _ = _internalTasks.Remove(currentFloor);

            // 移除当前楼层的当前方向的外部任务
            if (_currentDirection is Share.Direction.Up)
            {
                _ = _upExternalTasks.Remove(currentFloor);
            }
            else if (_currentDirection is Share.Direction.Down)
            {
                _ = _downExternalTasks.Remove(currentFloor);
            }
        }

        // 更新目标楼层
        UpdateTarget();
    }
}
