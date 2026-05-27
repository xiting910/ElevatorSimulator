using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

namespace ElevatorSimulator.Server.Models;

/// <summary>
/// 楼层外部呼叫的状态
/// </summary>
internal sealed class FloorCallState : INotifyPropertyChanged
{
    /// <summary>
    /// 激活的全局楼层外部呼叫集合, 采用嵌套的并发字典结构, 外层键为楼层号, 内层键为呼叫方向, 值为占位符
    /// </summary>
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Share.Direction, byte>> _activeCalls = [];

    /// <summary>
    /// 获取当前激活的楼层外部呼叫集合的字典快照
    /// </summary>
    public Dictionary<int, Share.Direction[]> ActiveCalls
    {
        get
        {
            var snapshot = new Dictionary<int, Share.Direction[]>();
            foreach (var kvp in _activeCalls)
            {
                snapshot[kvp.Key] = [.. kvp.Value.Keys];
            }
            return snapshot;
        }
    }

    /// <summary>
    /// 添加楼层外部呼叫
    /// </summary>
    /// <param name="floor">呼叫所在的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    /// <returns>如果添加成功返回 <see langword="true"/>, 否则返回 <see langword="false"/></returns>
    public bool AddFloorCall(int floor, Share.Direction direction)
    {
        if (_activeCalls.GetOrAdd(floor, _ => []).TryAdd(direction, 0))
        {
            PropertyChanged?.Invoke(this, new(nameof(ActiveCalls)));
            return true;
        }
        return false;
    }

    /// <summary>
    /// 删除楼层外部呼叫
    /// </summary>
    /// <param name="floor">呼叫所在的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    /// <returns>如果删除成功返回 <see langword="true"/>, 否则返回 <see langword="false"/></returns>
    public bool RemoveFloorCall(int floor, Share.Direction direction)
    {
        if (_activeCalls.TryGetValue(floor, out var dirDict) && dirDict.TryRemove(direction, out _))
        {
            if (dirDict.IsEmpty) { _ = _activeCalls.TryRemove(floor, out _); }
            PropertyChanged?.Invoke(this, new(nameof(ActiveCalls)));
            return true;
        }
        return false;
    }

    /// <summary>
    /// 楼层外部呼叫发生变化时触发的事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}
