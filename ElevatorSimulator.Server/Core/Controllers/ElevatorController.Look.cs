using System;
using System.Collections.Generic;

namespace ElevatorSimulator.Server.Core.Controllers;

// Look 算法 — 目标更新与方向决策
public sealed partial class ElevatorController
{
    /// <summary> 当前电梯的逻辑移动方向 </summary>
    private Direction _currentDirection;

    /// <summary> 当前电梯的目标楼层 </summary>
    private int? _currentTargetFloor;

    /// <summary>
    /// Look 算法更新电梯的目标楼层和移动方向
    /// </summary>
    private void UpdateTarget()
    {
        lock (_tasksLock)
        {
            // 获取所有任务列表是否为空
            var internalEmpty = _internalTasks.Count == 0;
            var upExternalEmpty = _upExternalTasks.Count == 0;
            var downExternalEmpty = _downExternalTasks.Count == 0;

            // 当前楼层
            var currentFloor = State.CurrentFloor;

            // 如果所有任务列表都为空, 则电梯空闲, 方向设为 None , 目标楼层设为 null
            if (internalEmpty && upExternalEmpty && downExternalEmpty)
            {
                _currentDirection = Direction.None;
                _currentTargetFloor = null;
                return;
            }

            // 如果当前方向为 None, 则根据任务分布决定新的方向和目标楼层
            if (_currentDirection is Direction.None)
            {
                // 验证状态合法性: 当方向为 None 时, 应该且仅应该有一个任务列表非空
                if ((internalEmpty ? 0 : 1) + (upExternalEmpty ? 0 : 1) + (downExternalEmpty ? 0 : 1) != 1)
                {
                    throw new InvalidOperationException("Invalid state: exactly one task list should be non-empty when current direction is None");
                }

                // 根据非空的任务列表决定新的方向和目标楼层
                if (!internalEmpty)
                {
                    // 当内部任务列表非空时, 最多只能有两个任务
                    if (_internalTasks.Count > 2)
                    {
                        throw new InvalidOperationException("Invalid state: at most two internal tasks should be present when current direction is None");
                    }

                    // 根据任务数量和任务楼层决定新的方向和目标楼层
                    if (_internalTasks.Count == 2)
                    {
                        // 如果有两个内部任务, 则当前楼层必须是其中一个
                        if (!_internalTasks.Contains(currentFloor))
                        {
                            throw new InvalidOperationException("Invalid state: one internal task should be at the current floor when there are two internal tasks and current direction is None");
                        }

                        // 目标楼层设为当前楼层, 方向保持为 None
                        _currentTargetFloor = currentFloor;
                    }
                    else
                    {
                        // 只有一个内部任务时, 目标楼层设为该任务的楼层, 方向根据目标楼层与当前楼层的关系决定
                        _currentTargetFloor = _internalTasks.Min;
                        if (_currentTargetFloor > currentFloor)
                        {
                            _currentDirection = Direction.Up;
                        }
                        else if (_currentTargetFloor < currentFloor)
                        {
                            _currentDirection = Direction.Down;
                        }
                    }
                }
                else if (!upExternalEmpty)
                {
                    // 当向上外部任务列表非空时, 应该是只有一个任务
                    if (_upExternalTasks.Count != 1)
                    {
                        throw new InvalidOperationException("Invalid state: exactly one up external task should be present when current direction is None");
                    }

                    // 目标楼层设为该向上外部任务的楼层, 方向根据目标楼层与当前楼层的关系决定
                    _currentTargetFloor = _upExternalTasks.Min;
                    if (_currentTargetFloor >= currentFloor)
                    {
                        _currentDirection = Direction.Up;
                    }
                    else if (_currentTargetFloor < currentFloor)
                    {
                        _currentDirection = Direction.Down;
                    }
                }
                else if (!downExternalEmpty)
                {
                    // 当向下外部任务列表非空时, 应该是只有一个任务
                    if (_downExternalTasks.Count != 1)
                    {
                        throw new InvalidOperationException("Invalid state: exactly one down external task should be present when current direction is None");
                    }

                    // 目标楼层设为该向下外部任务的楼层, 方向根据目标楼层与当前楼层的关系决定
                    _currentTargetFloor = _downExternalTasks.Max;
                    if (_currentTargetFloor <= currentFloor)
                    {
                        _currentDirection = Direction.Down;
                    }
                    else if (_currentTargetFloor > currentFloor)
                    {
                        _currentDirection = Direction.Up;
                    }
                }
            }
            // 如果当前方向为向上
            else if (_currentDirection is Direction.Up)
            {
                // 首先尝试在当前方向上寻找下一个停靠楼层
                _currentTargetFloor = GetNextStopUp(_internalTasks, _upExternalTasks, _downExternalTasks, currentFloor);
                if (_currentTargetFloor is null)
                {
                    // 如果当前方向上没有停靠楼层了, 则尝试在反方向寻找下一个停靠楼层
                    _currentDirection = Direction.Down;
                    _currentTargetFloor = GetNextStopDown(_internalTasks, _upExternalTasks, _downExternalTasks, currentFloor);
                }
            }
            // 如果当前方向为向下
            else if (_currentDirection is Direction.Down)
            {
                // 首先尝试在当前方向上寻找下一个停靠楼层
                _currentTargetFloor = GetNextStopDown(_internalTasks, _upExternalTasks, _downExternalTasks, currentFloor);
                if (_currentTargetFloor is null)
                {
                    // 如果当前方向上没有停靠楼层了, 则尝试在反方向寻找下一个停靠楼层
                    _currentDirection = Direction.Up;
                    _currentTargetFloor = GetNextStopUp(_internalTasks, _upExternalTasks, _downExternalTasks, currentFloor);
                }
            }
        }
    }

    /// <summary>
    /// 获取向上方向的下一个停靠楼层
    /// </summary>
    /// <param name="internalTasks">内部任务列表</param>
    /// <param name="upExternal">向上方向的外部任务列表</param>
    /// <param name="downExternal">向下方向的外部任务列表</param>
    /// <param name="currentFloor">当前楼层</param>
    /// <returns>下一个停靠楼层, 如果没有则返回 <see langword="null"/></returns>
    private static int? GetNextStopUp(SortedSet<int> internalTasks, SortedSet<int> upExternal, SortedSet<int> downExternal, int currentFloor)
    {
        // 下一个停靠楼层
        int? next = null;

        // 判断有没有在当前楼层以上的内部任务
        if (GetMinInRange(internalTasks, currentFloor, Constants.MaxFloor) is int intAbove) { next = intAbove; }

        // 判断有没有在当前楼层以上的向上外部任务
        if (GetMinInRange(upExternal, currentFloor, Constants.MaxFloor - 1) is int upAbove)
        {
            if (next is null || upAbove < next)
            {
                next = upAbove;
            }
        }

        // 如果 next 为 null , 返回在当前楼层以上的向下外部任务中最高的一个, 否则返回 next
        return next ?? GetMaxInRange(downExternal, currentFloor + 1, Constants.MaxFloor);
    }

    /// <summary>
    /// 获取向下方向的下一个停靠楼层
    /// </summary>
    /// <param name="internalTasks">内部任务列表</param>
    /// <param name="upExternal">向上方向的外部任务列表</param>
    /// <param name="downExternal">向下方向的外部任务列表</param>
    /// <param name="currentFloor">当前楼层</param>
    /// <returns>下一个停靠楼层, 如果没有则返回 <see langword="null"/></returns>
    private static int? GetNextStopDown(SortedSet<int> internalTasks, SortedSet<int> upExternal, SortedSet<int> downExternal, int currentFloor)
    {
        // 下一个停靠楼层
        int? next = null;

        // 判断有没有在当前楼层以下的内部任务
        if (GetMaxInRange(internalTasks, Constants.MinFloor, currentFloor) is int intBelow) { next = intBelow; }

        // 判断有没有在当前楼层以下的向下外部任务
        if (GetMaxInRange(downExternal, Constants.MinFloor + 1, currentFloor) is int downBelow)
        {
            if (next is null || downBelow > next)
            {
                next = downBelow;
            }
        }

        // 如果 next 为 null , 返回在当前楼层以下的向上外部任务中最低的一个, 否则返回 next
        return next ?? GetMinInRange(upExternal, Constants.MinFloor, currentFloor - 1);
    }

    /// <summary>
    /// 判断 <see cref="SortedSet{T}"/> 在 [min, max] 范围内是否包含元素
    /// </summary>
    /// <param name="set">要检查的 <see cref="SortedSet{T}"/></param>
    /// <param name="min">范围下界</param>
    /// <param name="max">范围上界</param>
    /// <returns><see langword="true"/> 如果范围内包含元素, 否则为 <see langword="false"/></returns>
    private static bool HasAnyInRange(SortedSet<int> set, int min, int max)
    {
        return min <= max && set.GetViewBetween(min, max).Count > 0;
    }

    /// <summary>
    /// 获取 <see cref="SortedSet{T}"/> 在 [min, max] 范围内的最小元素
    /// </summary>
    /// <param name="set">要检查的 <see cref="SortedSet{T}"/></param>
    /// <param name="min">范围下界</param>
    /// <param name="max">范围上界</param>
    /// <returns>范围内的最小元素, 如果范围内无元素则返回 <see langword="null"/></returns>
    private static int? GetMinInRange(SortedSet<int> set, int min, int max)
    {
        if (min > max) { return null; }
        var view = set.GetViewBetween(min, max);
        return view.Count > 0 ? view.Min : null;
    }

    /// <summary>
    /// 获取 <see cref="SortedSet{T}"/> 在 [min, max] 范围内的最大元素
    /// </summary>
    /// <param name="set">要检查的 <see cref="SortedSet{T}"/></param>
    /// <param name="min">范围下界</param>
    /// <param name="max">范围上界</param>
    /// <returns>范围内的最大元素, 如果范围内无元素则返回 <see langword="null"/></returns>
    private static int? GetMaxInRange(SortedSet<int> set, int min, int max)
    {
        if (min > max) { return null; }
        var view = set.GetViewBetween(min, max);
        return view.Count > 0 ? view.Max : null;
    }
}
