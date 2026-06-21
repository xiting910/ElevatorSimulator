using System;
using System.ComponentModel;

namespace ElevatorSimulator.Share.Enums;

/// <summary>
/// <see cref="Direction"/> 枚举的扩展方法
/// </summary>
public static class DirectionExtensions
{
    /// <summary>
    /// 获取方向的显示符号, 通过 <see cref="DescriptionAttribute"/> 读取
    /// </summary>
    /// <param name="direction">方向枚举值</param>
    /// <returns>方向的符号字符串</returns>
    public static string ToSymbol(this Direction direction)
    {
        var type = typeof(Direction);
        var name = Enum.GetName(type, direction);
        if (name is null) { return "—"; }

        var field = type.GetField(name);
        if (field is null) { return "—"; }

        var attr = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attr?.Description ?? "—";
    }
}
