namespace ElevatorSimulator.Server.Core;

/// <summary>
/// 电梯控制器, 只负责处理单个电梯的移动和门操作
/// </summary>
internal sealed class ElevatorController(int elevatorId)
{
    /// <summary>
    /// 管理的电梯的 Id
    /// </summary>
    public int ElevatorId { get; } = elevatorId;
}
