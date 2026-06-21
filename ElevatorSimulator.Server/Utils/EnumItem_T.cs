using System;
using System.ComponentModel;
using System.Reflection;

namespace ElevatorSimulator.Server.Utils;

/// <summary>
/// 泛型枚举辅助类, 用于 UI 下拉框数据绑定
/// </summary>
/// <typeparam name="T">枚举类型</typeparam>
public sealed class EnumItem<T> where T : struct, Enum
{
    /// <summary>
    /// 枚举值
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// 枚举值的描述文本
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 构造函数, 根据枚举值获取 <see cref="DescriptionAttribute"/> 描述文本, 若无则回退到值名称
    /// </summary>
    /// <param name="value">枚举值</param>
    public EnumItem(T value)
    {
        Value = value;
        var fi = value.GetType().GetField(value.ToString());
        Description = fi?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
    }

    /// <summary>
    /// 构造函数, 使用自定义描述文本
    /// </summary>
    /// <param name="value">枚举值</param>
    /// <param name="description">自定义描述文本</param>
    public EnumItem(T value, string description)
    {
        Value = value;
        Description = description;
    }

    /// <summary>
    /// 重写 <see cref="object.ToString"/> 方法, 返回描述文本, 以便在 UI 中显示
    /// </summary>
    /// <returns>描述文本</returns>
    public override string ToString() => Description;
}
