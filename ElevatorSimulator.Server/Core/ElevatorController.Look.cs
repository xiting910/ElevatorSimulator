using System;
using System.Collections.Generic;
using System.Threading;

namespace ElevatorSimulator.Server.Core;

// 电梯控制器的 Look 算法实现部分
internal sealed partial class ElevatorController
{
    /// <summary>
    /// 任务列表保护锁
    /// </summary>
    private readonly Lock _tasksLock = new();

    /// <summary>
    /// 内部任务列表
    /// </summary>
    private readonly SortedSet<int> _internalTasks = [];

    /// <summary>
    /// 向上的外部任务列表
    /// </summary>
    private readonly SortedSet<int> _upExternalTasks = [];

    /// <summary>
    /// 向下的外部任务列表
    /// </summary>
    private readonly SortedSet<int> _downExternalTasks = [];

    /// <summary>
    /// 当前电梯的移动方向, 由 Look 算法维护
    /// </summary>
    private Share.Direction _currentDirection;

    /// <summary>
    /// 当前电梯的目标楼层
    /// </summary>
    private int? _currentTargetFloor;

    /// <summary>
    /// 添加一个内部任务
    /// </summary>
    /// <param name="floor">目标楼层</param>
    public void AddInternalTask(int floor)
    {
        lock (_tasksLock)
        {
            _ = _internalTasks.Add(floor);
        }
        UpdateTarget();
    }

    /// <summary>
    /// 取消一个内部任务
    /// </summary>
    /// <param name="floor">目标楼层</param>
    public void RemoveInternalTask(int floor)
    {
        lock (_tasksLock)
        {
            _ = _internalTasks.Remove(floor);
        }
        UpdateTarget();
    }

    /// <summary>
    /// 添加一个外部任务
    /// </summary>
    /// <param name="floor">呼叫所在的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    /// <exception cref="ArgumentException">当 direction 不是 Up 或 Down 时抛出</exception>
    public void AddExternalTask(int floor, Share.Direction direction)
    {
        if (direction is Share.Direction.Up)
        {
            lock (_tasksLock)
            {
                _ = _upExternalTasks.Add(floor);
            }
        }
        else if (direction is Share.Direction.Down)
        {
            lock (_tasksLock)
            {
                _ = _downExternalTasks.Add(floor);
            }
        }
        else
        {
            throw new ArgumentException("Invalid direction for external task", nameof(direction));
        }
        UpdateTarget();
    }

    /// <summary>
    /// 取消一个外部任务
    /// </summary>
    /// <param name="floor">呼叫所在的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    /// <exception cref="ArgumentException">当 direction 不是 Up 或 Down 时抛出</exception>
    public void RemoveExternalTask(int floor, Share.Direction direction)
    {
        if (direction is Share.Direction.Up)
        {
            lock (_tasksLock)
            {
                _ = _upExternalTasks.Remove(floor);
            }
        }
        else if (direction is Share.Direction.Down)
        {
            lock (_tasksLock)
            {
                _ = _downExternalTasks.Remove(floor);
            }
        }
        else
        {
            throw new ArgumentException("Invalid direction for external task", nameof(direction));
        }
        UpdateTarget();
    }

    /// <summary>
    /// Look 算法更新电梯的目标楼层和移动方向
    /// </summary>
    private void UpdateTarget()
    {
        // 加锁保护任务列表的访问和修改
        lock (_tasksLock)
        {
            // 内部列表是否为空
            var internalEmpty = _internalTasks.Count == 0;

            // 向上的外部任务列表是否为空
            var upExternalEmpty = _upExternalTasks.Count == 0;

            // 向下的外部任务列表是否为空
            var downExternalEmpty = _downExternalTasks.Count == 0;

            // 当前楼层
            var currentFloor = ElevatorManager.Instance.Elevators[ElevatorId].CurrentFloor;

            // 如果所有任务列表都为空, 则将当前方向设置为 None, 目标楼层设置为 null, 并返回
            if (internalEmpty && upExternalEmpty && downExternalEmpty)
            {
                _currentDirection = Share.Direction.None;
                _currentTargetFloor = null;
                return;
            }

            // 如果当前方向为空, 说明是新加入第一个任务, 则直接以该任务为目标楼层, 并设置方向
            if (_currentDirection is Share.Direction.None)
            {
                // 内部任务列表不可能有任务
                if (!internalEmpty)
                {
                    throw new InvalidOperationException("Internal task cannot exist when current direction is None");
                }

                // 所有外部任务应该有且只有一个
                if (_upExternalTasks.Count + _downExternalTasks.Count != 1)
                {
                    throw new InvalidOperationException("There should be exactly one external task when current direction is None");
                }

                // 如果是向上的外部任务
                if (!upExternalEmpty)
                {
                    _currentTargetFloor = _upExternalTasks.Min;
                    if (_currentTargetFloor >= currentFloor)
                    {
                        _currentDirection = Share.Direction.Up;
                    }
                    else if (_currentTargetFloor < currentFloor)
                    {
                        _currentDirection = Share.Direction.Down;
                    }
                }
                // 如果是向下的外部任务
                else if (!downExternalEmpty)
                {
                    _currentTargetFloor = _downExternalTasks.Max;
                    if (_currentTargetFloor > currentFloor)
                    {
                        _currentDirection = Share.Direction.Up;
                    }
                    else if (_currentTargetFloor <= currentFloor)
                    {
                        _currentDirection = Share.Direction.Down;
                    }
                }

            }
            // 如果当前方向为向上, 则优先处理向上的任务, 如果没有向上的任务了, 则切换到向下
            else if (_currentDirection is Share.Direction.Up)
            {
                var nextUpwardTask = GetNextUpwardTaskFloor(currentFloor);
                if (nextUpwardTask is null)
                {
                    _currentDirection = Share.Direction.Down;
                    _currentTargetFloor = GetNextDownwardTaskFloor(currentFloor);
                }
                else
                {
                    _currentTargetFloor = nextUpwardTask;
                }
            }
            // 如果当前方向为向下, 则优先处理向下的任务, 如果没有向下的任务了, 则切换到向上
            else if (_currentDirection is Share.Direction.Down)
            {
                var nextDownwardTask = GetNextDownwardTaskFloor(currentFloor);
                if (nextDownwardTask is null)
                {
                    _currentDirection = Share.Direction.Up;
                    _currentTargetFloor = GetNextUpwardTaskFloor(currentFloor);
                }
                else
                {
                    _currentTargetFloor = nextDownwardTask;
                }
            }
        }
    }

    /// <summary>
    /// 获取方向为向上的下一个任务楼层
    /// </summary>
    /// <param name="currentFloor">当前楼层</param>
    /// <returns>下一个任务楼层, 如果没有则返回 null</returns>
    private int? GetNextUpwardTaskFloor(int currentFloor)
    {
        // 下一个内部任务
        var nextInternalTask = GetMinInRange(_internalTasks, currentFloor, Share.Constants.MaxFloor);

        // 下一个向上的外部任务
        var nextUpExternalTask = GetMinInRange(_upExternalTasks, currentFloor, Share.Constants.MaxFloor);

        return nextInternalTask is null ? nextUpExternalTask is null ? GetMaxInRange(_downExternalTasks, currentFloor + 1, Share.Constants.MaxFloor) : nextUpExternalTask : nextUpExternalTask is null ? nextInternalTask : Math.Min(nextInternalTask.Value, nextUpExternalTask.Value);
    }

    /// <summary>
    /// 获取方向为向下的下一个任务楼层
    /// </summary>
    /// <param name="currentFloor">当前楼层</param>
    /// <returns>下一个任务楼层, 如果没有则返回 null</returns>
    private int? GetNextDownwardTaskFloor(int currentFloor)
    {
        // 下一个内部任务
        var nextInternalTask = GetMaxInRange(_internalTasks, Share.Constants.MinFloor, currentFloor);

        // 下一个向下的外部任务
        var nextDownExternalTask = GetMaxInRange(_downExternalTasks, Share.Constants.MinFloor, currentFloor);

        return nextInternalTask is null ? nextDownExternalTask is null ? GetMinInRange(_upExternalTasks, Share.Constants.MinFloor, currentFloor - 1) : nextDownExternalTask : nextDownExternalTask is null ? nextInternalTask : Math.Max(nextInternalTask.Value, nextDownExternalTask.Value);
    }

    /// <summary>
    /// 获取指定集合中在指定范围内的最小值, 如果不存在则返回 null
    /// </summary>
    /// <param name="set">要查询的集合</param>
    /// <param name="min">范围的最小值</param>
    /// <param name="max">范围的最大值</param>
    /// <returns>如果存在则返回最小值, 否则返回 null</returns>
    private static int? GetMinInRange(SortedSet<int> set, int min, int max)
    {
        var view = set.GetViewBetween(min, max);
        return view.Count == 0 ? null : view.Min;
    }

    /// <summary>
    /// 获取指定集合中在指定范围内的最大值, 如果不存在则返回 null
    /// </summary>
    /// <param name="set">要查询的集合</param>
    /// <param name="min">范围的最小值</param>
    /// <param name="max">范围的最大值</param>
    /// <returns>如果存在则返回最大值, 否则返回 null</returns>
    private static int? GetMaxInRange(SortedSet<int> set, int min, int max)
    {
        var view = set.GetViewBetween(min, max);
        return view.Count == 0 ? null : view.Max;
    }
}
