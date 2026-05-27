using System;
using System.ComponentModel;

namespace ElevatorSimulator.Server.Utils;

/// <summary>
/// 泛型枚举辅助类, 用于UI下拉框数据绑定
/// </summary>
internal sealed class EnumItem<T> where T : struct, Enum
{
    /// <summary>
    /// 枚举值
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// 枚举值的描述文本, 通过 <see cref="DescriptionAttribute"/> 获取, 如果没有则使用枚举值的字符串表示
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 构造函数, 根据枚举值获取描述文本
    /// </summary>
    /// <param name="value">枚举值</param>
    public EnumItem(T value)
    {
        Value = value;
        var fi = value.GetType().GetField(value.ToString());
        if (fi is not null)
        {
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length > 0)
            {
                Description = attributes[0].Description;
                return;
            }
        }
        Description = value.ToString();
    }

    /// <summary>
    /// 获取枚举类型的所有值和描述文本, 用于 UI 下拉框数据绑定
    /// </summary>
    /// <returns>枚举项数组</returns>
    public static EnumItem<T>[] GetAll()
    {
        var values = Enum.GetValues<T>();
        var items = new EnumItem<T>[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            items[i] = new EnumItem<T>(values[i]);
        }
        return items;
    }

    /// <summary>
    /// 重写 <see cref="object.ToString"/> 方法, 返回描述文本, 以便在 UI 中显示
    /// </summary>
    /// <returns>描述文本</returns>
    public override string ToString() => Description;
}
