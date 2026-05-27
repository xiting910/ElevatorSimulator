using System.Collections.Concurrent;
using System.ComponentModel;

namespace ElevatorSimulator.Server.Models;

/// <summary>
/// 电梯在服务端的逻辑状态
/// </summary>
internal sealed class ElevatorState : INotifyPropertyChanged
{
    /// <summary>
    /// 电梯 ID
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 电梯当前所在的楼层
    /// </summary>
    public int CurrentFloor
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new(nameof(CurrentFloor)));
            }
        }
    }

    /// <summary>
    /// 电梯当前的移动方向
    /// </summary>
    public Share.Direction MovingDirection
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new(nameof(MovingDirection)));
            }
        }
    }

    /// <summary>
    /// 电梯门的状态
    /// </summary>
    public Share.DoorState Door
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new(nameof(Door)));
            }
        }
    }

    /// <summary>
    /// 电梯门的开度比例
    /// </summary>
    public double DoorOpenRatio
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new(nameof(DoorOpenRatio)));
            }
        }
    }

    /// <summary>
    /// 内部呼叫的目标楼层, 键为目标楼层, 值为占位符
    /// </summary>
    private readonly ConcurrentDictionary<int, byte> _internalCalls = [];

    /// <summary>
    /// 获取内部呼叫的目标楼层的数组快照
    /// </summary>
    public int[] InternalCalls => [.. _internalCalls.Keys];

    /// <summary>
    /// 添加内部呼叫
    /// </summary>
    /// <param name="targetFloor">目标楼层</param>
    /// <returns>如果添加成功返回 <see langword="true"/>, 否则返回 <see langword="false"/></returns>
    public bool AddInternalCall(int targetFloor)
    {
        if (_internalCalls.TryAdd(targetFloor, 0))
        {
            PropertyChanged?.Invoke(this, new(nameof(InternalCalls)));
            return true;
        }
        return false;
    }

    /// <summary>
    /// 删除内部呼叫
    /// </summary>
    /// <param name="targetFloor">目标楼层</param>
    /// <returns>如果删除成功返回 <see langword="true"/>, 否则返回 <see langword="false"/></returns>
    public bool RemoveInternalCall(int targetFloor)
    {
        if (_internalCalls.TryRemove(targetFloor, out _))
        {
            PropertyChanged?.Invoke(this, new(nameof(InternalCalls)));
            return true;
        }
        return false;
    }

    /// <summary>
    /// 电梯状态发生变化时触发的事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}
