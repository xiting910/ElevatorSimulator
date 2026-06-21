using ElevatorSimulator.Server.Utils;
using System.ComponentModel;

namespace ElevatorSimulator.Server.Test;

/// <summary>
/// 用于测试的枚举类型, 包含 <see cref="DescriptionAttribute"/> 和无特性的成员
/// </summary>
public enum TestEnum
{
    [Description("第一个选项")]
    First,

    [Description("第二个选项")]
    Second,

    NoDescription
}

/// <summary>
/// <see cref="EnumItem{T}"/> 和 <see cref="EnumItem"/> 的单元测试, 验证枚举项值与描述文本的提取
/// </summary>
public sealed class EnumItemTests
{
    /// <summary>
    /// <see cref="EnumItem{T}.Value"/> 应返回构造时传入的枚举值
    /// </summary>
    [Fact]
    public void EnumItem_T_Value_ReturnsEnumValue()
    {
        var item = new EnumItem<TestEnum>(TestEnum.First);
        Assert.Equal(TestEnum.First, item.Value);
    }

    /// <summary>
    /// Description 应从 <see cref="DescriptionAttribute"/> 读取
    /// </summary>
    [Fact]
    public void EnumItem_T_Description_ReadsFromAttribute()
    {
        var item = new EnumItem<TestEnum>(TestEnum.First);
        Assert.Equal("第一个选项", item.Description);
    }

    /// <summary>
    /// 无 DescriptionAttribute 时, 应回退到枚举成员名称
    /// </summary>
    [Fact]
    public void EnumItem_T_NoDescription_FallsBackToName()
    {
        var item = new EnumItem<TestEnum>(TestEnum.NoDescription);
        Assert.Equal("NoDescription", item.Description);
    }

    /// <summary>
    /// 使用自定义描述构造函数时, 应返回自定义文本
    /// </summary>
    [Fact]
    public void EnumItem_T_CustomDescription_Used()
    {
        var item = new EnumItem<TestEnum>(TestEnum.First, "自定义文本");
        Assert.Equal("自定义文本", item.Description);
    }

    /// <summary>
    /// <see cref="EnumItem{T}.ToString"/> 应返回描述文本
    /// </summary>
    [Fact]
    public void ToString_ReturnsDescription()
    {
        var item = new EnumItem<TestEnum>(TestEnum.Second);
        Assert.Equal("第二个选项", item.ToString());
    }

    /// <summary>
    /// <see cref="EnumItem.GetAll{T}"/> 应返回所有枚举成员
    /// </summary>
    [Fact]
    public void GetAll_ReturnsAllEnumMembers()
    {
        // Act
        var items = EnumItem.GetAll<TestEnum>();

        // Assert
        Assert.Equal(3, items.Length);
    }

    /// <summary>
    /// 连续调用 <see cref="EnumItem.GetAll{T}"/> 应返回缓存的同一数组
    /// </summary>
    [Fact]
    public void GetAll_IsCached()
    {
        // Act
        var items1 = EnumItem.GetAll<TestEnum>();
        var items2 = EnumItem.GetAll<TestEnum>();

        // Assert - same array reference (cached)
        Assert.Same(items1, items2);
    }

    /// <summary>
    /// GetAll 返回的首个元素应有正确的描述文本
    /// </summary>
    [Fact]
    public void GetAll_FirstItem_HasCorrectDescription()
    {
        var items = EnumItem.GetAll<TestEnum>();
        var first = items.First(i => i.Value == TestEnum.First);
        Assert.Equal("第一个选项", first.Description);
    }
}
