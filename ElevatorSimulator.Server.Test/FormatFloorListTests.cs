namespace ElevatorSimulator.Server.Test;

/// <summary>
/// 楼层数组格式化逻辑的单元测试, 通过反射调用 <c>MainForm.FormatFloorList</c> 私有静态方法
/// </summary>
public sealed class FormatFloorListTests
{
    /// <summary>
    /// 单个楼层应直接输出数字
    /// </summary>
    [Fact]
    public void Format_SingleFloor()
    {
        var result = InvokeFormatFloorList([5]);
        Assert.Equal("5", result);
    }

    /// <summary>
    /// 连续楼层应合并为区间格式 (如 "1-3")
    /// </summary>
    [Fact]
    public void Format_ConsecutiveFloors()
    {
        var result = InvokeFormatFloorList([1, 2, 3]);
        Assert.Equal("1-3", result);
    }

    /// <summary>
    /// 混合连续与离散楼层应正确格式化 (如 "1-3, 5, 7-9")
    /// </summary>
    [Fact]
    public void Format_Mixed()
    {
        var result = InvokeFormatFloorList([1, 2, 3, 5, 7, 8, 9]);
        Assert.Equal("1-3, 5, 7-9", result);
    }

    /// <summary>
    /// 空数组应返回 "无"
    /// </summary>
    [Fact]
    public void Format_Empty()
    {
        var result = InvokeFormatFloorList([]);
        Assert.Equal("无", result);
    }

    /// <summary>
    /// 乱序输入应自动排序后输出
    /// </summary>
    [Fact]
    public void Format_ReverseOrder_StillSorted()
    {
        var result = InvokeFormatFloorList([9, 1, 3, 2]);
        Assert.Equal("1-3, 9", result);
    }

    /// <summary>
    /// 包含负数的最低楼层也应正确格式化
    /// </summary>
    [Fact]
    public void Format_NonConsecutivePairs()
    {
        var result = InvokeFormatFloorList([-2, 0, 10, 33]);
        Assert.Equal("-2, 0, 10, 33", result);
    }

    /// <summary>
    /// 通过反射调用 MainForm.FormatFloorList 私有静态方法
    /// </summary>
    private static string InvokeFormatFloorList(int[] floors)
    {
        var method = typeof(UI.MainForm).GetMethod(
            "FormatFloorList",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);
        return (string)method!.Invoke(null, [floors])!;
    }
}
