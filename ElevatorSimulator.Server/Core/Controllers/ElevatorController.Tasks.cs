using System.Collections.Generic;
using System.Threading;

namespace ElevatorSimulator.Server.Core.Controllers;

// 任务管理
public sealed partial class ElevatorController
{
    /// <summary> 任务列表保护锁 </summary>
    private readonly Lock _tasksLock = new();

    /// <summary> 内部任务列表 </summary>
    private readonly SortedSet<int> _internalTasks = [];

    /// <summary> 向上的外部任务列表 </summary>
    private readonly SortedSet<int> _upExternalTasks = [];

    /// <summary> 向下的外部任务列表 </summary>
    private readonly SortedSet<int> _downExternalTasks = [];

    /// <inheritdoc/>
    public void AddInternalTask(int floor)
    {
        bool needUpdate;
        lock (_tasksLock) { needUpdate = _internalTasks.Add(floor); }
        if (needUpdate) { UpdateTarget(); }
    }

    /// <inheritdoc/>
    public void RemoveInternalTask(int floor)
    {
        bool needUpdate;
        lock (_tasksLock) { needUpdate = _internalTasks.Remove(floor); }
        if (needUpdate) { UpdateTarget(); }
    }

    /// <inheritdoc/>
    public void AddExternalTask(int floor, Direction direction)
    {
        var needUpdate = false;
        if (direction is Direction.Up)
        {
            lock (_tasksLock) { needUpdate = _upExternalTasks.Add(floor); }
        }
        else if (direction is Direction.Down)
        {
            lock (_tasksLock) { needUpdate = _downExternalTasks.Add(floor); }
        }
        if (needUpdate) { UpdateTarget(); }
    }

    /// <inheritdoc/>
    public void RemoveExternalTask(int floor, Direction direction)
    {
        var needUpdate = false;
        if (direction is Direction.Up)
        {
            lock (_tasksLock) { needUpdate = _upExternalTasks.Remove(floor); }
        }
        else if (direction is Direction.Down)
        {
            lock (_tasksLock) { needUpdate = _downExternalTasks.Remove(floor); }
        }
        if (needUpdate) { UpdateTarget(); }
    }

    /// <summary>
    /// 完成当前楼层的任务
    /// </summary>
    /// <param name="currentFloor">当前楼层</param>
    private void CompleteCurrentFloorTask(int currentFloor)
    {
        bool internalCompleted;
        var externalCompleted = false;

        lock (_tasksLock)
        {
            internalCompleted = _internalTasks.Remove(currentFloor);

            if (_currentDirection is Direction.Up)
            {
                externalCompleted = _upExternalTasks.Remove(currentFloor);
            }
            else if (_currentDirection is Direction.Down)
            {
                externalCompleted = _downExternalTasks.Remove(currentFloor);
            }
        }

        if (internalCompleted)
        {
            _ = State.RemoveInternalCall(currentFloor);
            LogInternalCallCompleted(_logger, Id, currentFloor);
        }

        if (externalCompleted)
        {
            _ = _floorCallState.RemoveFloorCall(currentFloor, _currentDirection);
            LogExternalCallCompleted(_logger, Id, currentFloor, _currentDirection);
        }
    }
}
