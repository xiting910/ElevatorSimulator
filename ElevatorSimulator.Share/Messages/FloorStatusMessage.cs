using System.Collections.Generic;

namespace ElevatorSimulator.Share.Messages;

/// <summary>
/// 楼层状态消息类, 表示楼层的当前状态
/// </summary>
public sealed class FloorStatusMessage : Message
{
    /// <summary>
    /// 当前所有激活的呼叫请求字典, 键为楼层号, 值为该楼层的呼叫方向集合
    /// </summary>
    public Dictionary<int, Enums.Direction[]> ActiveCalls { get; init; } = [];
}
