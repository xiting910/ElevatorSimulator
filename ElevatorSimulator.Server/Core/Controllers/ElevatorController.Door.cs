namespace ElevatorSimulator.Server.Core.Controllers;

// 门控信号
public sealed partial class ElevatorController
{
    /// <summary> 缓存的开门请求标志 </summary>
    private volatile bool _requestDoorOpen;

    /// <summary> 缓存的关门请求标志 </summary>
    private volatile bool _requestDoorClose;

    /// <inheritdoc/>
    public void SignalDoorOpen()
    {
        if (State.MovingDirection is Direction.None)
        {
            _requestDoorOpen = true;
        }
    }

    /// <inheritdoc/>
    public void SignalDoorClose()
    {
        if (State.MovingDirection is Direction.None)
        {
            _requestDoorClose = true;
        }
    }
}
