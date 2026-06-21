namespace ElevatorSimulator.Server.Core.Interfaces;

/// <summary>
/// 电梯控制器接口
/// </summary>
public interface IElevatorController
{
    /// <summary>
    /// 电梯 ID
    /// </summary>
    int Id { get; }

    /// <summary>
    /// 当前电梯的逻辑状态
    /// </summary>
    Models.Interfaces.IElevatorState State { get; }

    /// <summary>
    /// 添加外部任务
    /// </summary>
    /// <param name="floor">呼叫的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    void AddExternalTask(int floor, Direction direction);

    /// <summary>
    /// 移除外部任务
    /// </summary>
    /// <param name="floor">呼叫的楼层</param>
    /// <param name="direction">呼叫的方向</param>
    void RemoveExternalTask(int floor, Direction direction);

    /// <summary>
    /// 添加内部任务
    /// </summary>
    /// <param name="floor">目标楼层</param>
    void AddInternalTask(int floor);

    /// <summary>
    /// 移除内部任务
    /// </summary>
    /// <param name="floor">目标楼层</param>
    void RemoveInternalTask(int floor);

    /// <summary>
    /// 设置开门信号
    /// </summary>
    void SignalDoorOpen();

    /// <summary>
    /// 设置关门信号
    /// </summary>
    void SignalDoorClose();

    /// <summary>
    /// 预测完成一个外部呼叫所需的预估秒数
    /// </summary>
    /// <param name="floor">外部呼叫的楼层</param>
    /// <param name="direction">外部呼叫的方向</param>
    /// <returns>完成呼叫的预估秒数</returns>
    double PredictTimeToServeExternalCall(int floor, Direction direction);
}
