using System.Collections.Concurrent;
using System.ComponentModel;

namespace ElevatorSimulator.Server.Models;

/// <summary>
/// 电梯在服务端的逻辑状态
/// </summary>
public sealed class ElevatorState : Interfaces.IElevatorState
{
    /// <inheritdoc/>
    public int Id { get; init; }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public Direction MovingDirection
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

    /// <inheritdoc/>
    public DoorState Door
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public int[] InternalCalls => [.. _internalCalls.Keys];

    /// <inheritdoc/>
    public bool AddInternalCall(int targetFloor)
    {
        if (_internalCalls.TryAdd(targetFloor, 0))
        {
            PropertyChanged?.Invoke(this, new(nameof(InternalCalls)));
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public bool RemoveInternalCall(int targetFloor)
    {
        if (_internalCalls.TryRemove(targetFloor, out _))
        {
            PropertyChanged?.Invoke(this, new(nameof(InternalCalls)));
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
}
