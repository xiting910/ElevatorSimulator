using System.Timers;

namespace ElevatorSimulator.Server.Core.Controllers;

// 电梯模拟主循环
public sealed partial class ElevatorController
{
    /// <summary> 电梯经过一个楼层所需的时间间隔数 </summary>
    private const int IntervalsPerFloor = Constants.FloorTravelTimeSec * 1000 / Constants.UpdateInterval;

    /// <summary> 电梯开门后等待的时间间隔数 </summary>
    private const int IntervalsDoorOpen = Constants.DoorOpenWaitTimeSec * 1000 / Constants.UpdateInterval;

    /// <summary> 每个时间间隔电梯门开关的比例增量 </summary>
    private const double DoorOpenCloseRatioPerInterval = Constants.UpdateInterval / (Constants.DoorOpenCloseTimeSec * 1000.0);

    /// <summary> 从上一个楼层开始移动后经过的计时器周期数 </summary>
    private int _passedIntervalsSinceLastFloor;

    /// <summary> 电梯开门后等待的计时器周期数 </summary>
    private int _passedIntervalsSinceDoorOpened;

    /// <summary>
    /// 定时器 <see cref="Timer.Elapsed"/> 事件的处理程序
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (State.MovingDirection is Direction.Up or Direction.Down)
        {
            HandleMoving();
        }
        else if (State.MovingDirection is Direction.None)
        {
            HandleStopped();
        }
    }

    /// <summary>
    /// 处理电梯移动中的逻辑
    /// </summary>
    private void HandleMoving()
    {
        _passedIntervalsSinceLastFloor++;

        if (_passedIntervalsSinceLastFloor != IntervalsPerFloor) { return; }

        State.CurrentFloor += State.MovingDirection is Direction.Up ? 1 : -1;
        _passedIntervalsSinceLastFloor = 0;

        lock (_tasksLock)
        {
            if (_currentTargetFloor is not null)
            {
                if (State.CurrentFloor < _currentTargetFloor)
                {
                    State.MovingDirection = Direction.Up;
                }
                else if (State.CurrentFloor > _currentTargetFloor)
                {
                    State.MovingDirection = Direction.Down;
                }
                else
                {
                    State.MovingDirection = Direction.None;
                    State.Door = DoorState.Opening;
                    var currentFloor = State.CurrentFloor;

                    if (_currentDirection is Direction.Up)
                    {
                        if (!_internalTasks.Contains(currentFloor) && !_upExternalTasks.Contains(currentFloor) && _downExternalTasks.Contains(currentFloor))
                        {
                            _currentDirection = Direction.Down;
                        }
                    }
                    else if (_currentDirection is Direction.Down)
                    {
                        if (!_internalTasks.Contains(currentFloor) && !_downExternalTasks.Contains(currentFloor) && _upExternalTasks.Contains(currentFloor))
                        {
                            _currentDirection = Direction.Up;
                        }
                    }
                }
            }
            else
            {
                State.MovingDirection = Direction.None;
            }
        }
    }

    /// <summary>
    /// 处理电梯停靠中的逻辑
    /// </summary>
    private void HandleStopped()
    {
        HandleDoorRequests();

        if (State.Door is DoorState.Open)
        {
            _passedIntervalsSinceDoorOpened++;
            if (_passedIntervalsSinceDoorOpened == IntervalsDoorOpen)
            {
                _passedIntervalsSinceDoorOpened = 0;
                State.Door = DoorState.Closing;
                UpdateTarget();
            }
        }
        else if (State.Door is DoorState.Opening)
        {
            State.DoorOpenRatio += DoorOpenCloseRatioPerInterval;
            if (State.DoorOpenRatio >= 1)
            {
                State.DoorOpenRatio = 1;
                State.Door = DoorState.Open;
                CompleteCurrentFloorTask(State.CurrentFloor);
            }
        }
        else if (State.Door is DoorState.Closing)
        {
            State.DoorOpenRatio -= DoorOpenCloseRatioPerInterval;
            if (State.DoorOpenRatio <= 0)
            {
                State.DoorOpenRatio = 0;
                State.Door = DoorState.Closed;
            }

            lock (_tasksLock)
            {
                if (State.CurrentFloor == _currentTargetFloor)
                {
                    State.Door = DoorState.Opening;
                }
            }
        }
        else if (State.Door is DoorState.Closed)
        {
            lock (_tasksLock)
            {
                if (_currentTargetFloor is not null)
                {
                    if (_currentTargetFloor > State.CurrentFloor)
                    {
                        State.MovingDirection = Direction.Up;
                    }
                    else if (_currentTargetFloor < State.CurrentFloor)
                    {
                        State.MovingDirection = Direction.Down;
                    }
                    else
                    {
                        State.Door = DoorState.Opening;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 处理缓存的开关门请求
    /// </summary>
    private void HandleDoorRequests()
    {
        if (_requestDoorOpen)
        {
            _requestDoorOpen = false;
            _requestDoorClose = false;
            if (State.Door is DoorState.Closing)
            {
                State.Door = DoorState.Opening;
            }
            else if (State.Door is DoorState.Closed)
            {
                State.Door = DoorState.Opening;
                State.DoorOpenRatio = 0;
            }
        }
        else if (_requestDoorClose)
        {
            _requestDoorClose = false;
            if (State.Door is DoorState.Open)
            {
                State.Door = DoorState.Closing;
                _passedIntervalsSinceDoorOpened = 0;
                UpdateTarget();
            }
        }
    }
}
