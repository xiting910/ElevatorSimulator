using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

namespace ElevatorSimulator.Server.Models;

/// <summary>
/// 楼层外部呼叫的状态
/// </summary>
public sealed class FloorCallState : Interfaces.IFloorCallState
{
    /// <summary>
    /// 激活的全局楼层外部呼叫集合, 采用嵌套的并发字典结构, 外层键为楼层号, 内层键为呼叫方向, 值为占位符
    /// </summary>
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Direction, byte>> _activeCalls = [];

    /// <inheritdoc/>
    public Dictionary<int, Direction[]> ActiveCalls
    {
        get
        {
            var snapshot = new Dictionary<int, Direction[]>();
            foreach (var kvp in _activeCalls)
            {
                snapshot[kvp.Key] = [.. kvp.Value.Keys];
            }
            return snapshot;
        }
    }

    /// <inheritdoc/>
    public bool AddFloorCall(int floor, Direction direction)
    {
        if (_activeCalls.GetOrAdd(floor, _ => []).TryAdd(direction, 0))
        {
            PropertyChanged?.Invoke(this, new(nameof(ActiveCalls)));
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public bool RemoveFloorCall(int floor, Direction direction)
    {
        if (_activeCalls.TryGetValue(floor, out var dirDict) && dirDict.TryRemove(direction, out _))
        {
            if (dirDict.IsEmpty) { _ = _activeCalls.TryRemove(floor, out _); }
            PropertyChanged?.Invoke(this, new(nameof(ActiveCalls)));
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
}
