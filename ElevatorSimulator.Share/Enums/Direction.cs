using System.ComponentModel;

namespace ElevatorSimulator.Share.Enums;

/// <summary>
/// 电梯运行方向枚举, 通过 <see cref="DescriptionAttribute"/> 定义显示符号
/// </summary>
public enum Direction
{
    /// <summary> 无 </summary>
    [Description("—")]
    None,

    /// <summary> 向上 </summary>
    [Description("↑")]
    Up,

    /// <summary> 向下 </summary>
    [Description("↓")]
    Down
}
