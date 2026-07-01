using System;

namespace ElevatorSimulator.Client.Core;

/// <summary>
/// 客户端状态存储, 实现 <see cref="Interfaces.IClientState"/>
/// </summary>
public sealed class ClientState : Interfaces.IClientState
{
    /// <inheritdoc/>
    public string ClientId { get; } = Guid.NewGuid().ToString("N");

    /// <inheritdoc/>
    public int CurrentFloor { get; set; }

    /// <inheritdoc/>
    public int? CurrentElevatorId { get; set; }

    /// <inheritdoc/>
    public Messages.ElevatorStatusMessage[] ElevatorStatuses { get; } = new Messages.ElevatorStatusMessage[Constants.ElevatorCount];

    /// <inheritdoc/>
    public Messages.FloorStatusMessage FloorStatus { get; private set; } = new();

    /// <inheritdoc/>
    public event Action<Messages.ElevatorStatusMessage>? OnElevatorStatusUpdated;

    /// <inheritdoc/>
    public event Action<Messages.FloorStatusMessage>? OnFloorStatusUpdated;

    /// <inheritdoc/>
    public bool CanEnterElevator(int elevatorId)
    {
        if (elevatorId < 0 || elevatorId >= ElevatorStatuses.Length) { return false; }
        var s = ElevatorStatuses[elevatorId];
        return s.CurrentFloor == CurrentFloor && s.Door is DoorState.Open;
    }

    /// <inheritdoc/>
    public bool CanExitElevator()
    {
        return CurrentElevatorId is int elevatorId && elevatorId >= 0 && elevatorId < ElevatorStatuses.Length && ElevatorStatuses[elevatorId].Door is DoorState.Open;
    }

    /// <inheritdoc/>
    public bool HasActiveCall(Direction direction)
    {
        return FloorStatus.ActiveCalls.TryGetValue(CurrentFloor, out var dirs) && dirs.Contains(direction);
    }

    /// <inheritdoc/>
    public void UpdateElevatorStatus(Messages.ElevatorStatusMessage status)
    {
        if (status.Id >= 0 && status.Id < ElevatorStatuses.Length)
        {
            ElevatorStatuses[status.Id] = status;
            OnElevatorStatusUpdated?.Invoke(status);
        }
    }

    /// <inheritdoc/>
    public void UpdateFloorStatus(Messages.FloorStatusMessage status)
    {
        FloorStatus = status;
        OnFloorStatusUpdated?.Invoke(status);
    }
}
