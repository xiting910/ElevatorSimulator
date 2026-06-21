using System.ComponentModel;

namespace ElevatorSimulator.Server.Models.Interfaces;

/// <summary>
/// 电梯逻辑状态接口
/// </summary>
public interface IElevatorState : INotifyPropertyChanged
{
    /// <summary>
    /// 电梯 ID
    /// </summary>
    int Id { get; init; }

    /// <summary>
    /// 电梯当前所在的楼层
    /// </summary>
    int CurrentFloor { get; set; }

    /// <summary>
    /// 电梯当前的移动方向
    /// </summary>
    Direction MovingDirection { get; set; }

    /// <summary>
    /// 电梯门的状态
    /// </summary>
    DoorState Door { get; set; }

    /// <summary>
    /// 电梯门的开度比例
    /// </summary>
    double DoorOpenRatio { get; set; }

    /// <summary>
    /// 获取内部呼叫的目标楼层的数组快照
    /// </summary>
    int[] InternalCalls { get; }

    /// <summary>
    /// 添加内部呼叫
    /// </summary>
    /// <param name="targetFloor">目标楼层</param>
    /// <returns>如果添加成功返回 <see langword="true"/>, 否则返回 <see langword="false"/></returns>
    bool AddInternalCall(int targetFloor);

    /// <summary>
    /// 移除内部呼叫
    /// </summary>
    /// <param name="targetFloor">目标楼层</param>
    /// <returns>如果移除成功返回 <see langword="true"/>, 否则返回 <see langword="false"/></returns>
    bool RemoveInternalCall(int targetFloor);
}
