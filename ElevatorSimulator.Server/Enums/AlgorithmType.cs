using System.ComponentModel;

namespace ElevatorSimulator.Server.Enums;

/// <summary>
/// 调度算法枚举
/// </summary>
internal enum AlgorithmType
{
    [Description("SCAN算法 (电梯模式)")]
    Scan,

    [Description("LOOK算法 (默认扫描)")]
    Look
}
