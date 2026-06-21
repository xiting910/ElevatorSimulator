using System.Collections.Generic;
using System.ComponentModel;

namespace ElevatorSimulator.Server.Models.Interfaces;

/// <summary>
/// 楼层外部呼叫状态接口
/// </summary>
public interface IFloorCallState : INotifyPropertyChanged
{
    /// <summary>
    /// 获取当前激活的楼层外部呼叫集合的字典快照
    /// </summary>
    Dictionary<int, Direction[]> ActiveCalls { get; }

    /// <summary>
    /// 添加楼层外部呼叫
    /// </summary>
    /// <param name="floor">呼叫所在的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    /// <returns>如果添加成功返回 <see langword="true"/>, 否则返回 <see langword="false"/></returns>
    bool AddFloorCall(int floor, Direction direction);

    /// <summary>
    /// 删除楼层外部呼叫
    /// </summary>
    /// <param name="floor">呼叫所在的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    /// <returns>如果删除成功返回 <see langword="true"/>, 否则返回 <see langword="false"/></returns>
    bool RemoveFloorCall(int floor, Direction direction);
}
