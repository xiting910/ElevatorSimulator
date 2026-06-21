using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ElevatorSimulator.Server.UI;

// 显示更新方法, 列表绘制, 楼层格式化
public sealed partial class MainForm
{

    /// <summary>
    /// 自定义绘制列标题
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">绘制事件参数</param>
    private static void ListView_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        // 绘制默认背景和边框
        e.DrawBackground();
        e.DrawText(TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }

    /// <summary>
    /// 自定义绘制单元格内容
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">绘制事件参数</param>
    private static void ListView_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        // 跳过无效区域
        if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0) { return; }

        // 确保前景色有效
        var foreColor = e.SubItem?.ForeColor ?? e.Item?.ForeColor ?? SystemColors.WindowText;
        if (foreColor.A == 0) { foreColor = SystemColors.WindowText; }

        // 绘制背景
        TextRenderer.DrawText(e.Graphics, e.SubItem?.Text, e.SubItem?.Font ?? SystemFonts.DefaultFont, e.Bounds, foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }

    /// <summary>
    /// 将楼层数组格式化为区间表示, 例如 [1,2,3,5,7,8,9] → "1-3, 5, 7-9"
    /// </summary>
    /// <param name="floors">楼层数组</param>
    /// <returns>格式化后的字符串</returns>
    private static string FormatFloorList(int[] floors)
    {
        if (floors.Length == 0) { return "无"; }
        if (floors.Length == 1) { return floors[0].ToString(); }

        Array.Sort(floors);
        var parts = new StringBuilder();
        var rangeStart = floors[0];
        var rangeEnd = floors[0];

        for (var i = 1; i < floors.Length; i++)
        {
            if (floors[i] == rangeEnd + 1)
            {
                rangeEnd = floors[i];
            }
            else
            {
                AppendRange(parts, rangeStart, rangeEnd);
                rangeStart = floors[i];
                rangeEnd = floors[i];
            }
        }
        AppendRange(parts, rangeStart, rangeEnd);

        return parts.ToString();
    }

    /// <summary>
    /// 向 StringBuilder 追加一个区间
    /// </summary>
    /// <param name="sb">StringBuilder 实例</param>
    /// <param name="start">区间开始</param>
    /// <param name="end">区间结束</param>
    private static void AppendRange(StringBuilder sb, int start, int end)
    {
        if (sb.Length > 0) { _ = sb.Append(", "); }
        _ = start == end ? sb.Append(start) : sb.Append(start).Append('-').Append(end);
    }

    /// <summary>
    /// 更新电梯状态显示
    /// </summary>
    /// <param name="elevators">电梯状态数组</param>
    public void UpdateElevatorStatus(IEnumerable<Models.Interfaces.IElevatorState> elevators)
    {
        if (InvokeRequired)
        {
            Invoke(new(() => UpdateElevatorStatus(elevators)));
            return;
        }

        _lvElevators.BeginUpdate();

        foreach (var (i, elevator) in elevators.Index())
        {
            // 若行不够则追加占位行
            if (i >= _lvElevators.Items.Count)
            {
                _ = _lvElevators.Items.Add(new ListViewItem(new string[6]));
            }

            // 仅对属性进行覆盖更新, 避免画面闪烁
            _lvElevators.Items[i].SubItems[0].Text = elevator.Id.ToString();
            _lvElevators.Items[i].SubItems[1].Text = elevator.CurrentFloor.ToString();
            _lvElevators.Items[i].SubItems[2].Text = elevator.MovingDirection.ToSymbol();
            _lvElevators.Items[i].SubItems[3].Text = elevator.Door.ToString();
            _lvElevators.Items[i].SubItems[4].Text = elevator.DoorOpenRatio.ToString("P0");
            var calls = elevator.InternalCalls;
            _lvElevators.Items[i].SubItems[5].Text = calls.Length == 0 ? "无" : FormatFloorList(calls);
            _lvElevators.Items[i].ToolTipText = calls.Length == 0 ? null : string.Join(", ", calls);
        }

        _lvElevators.EndUpdate();
    }

    /// <summary>
    /// 更新楼层呼叫显示
    /// </summary>
    /// <param name="activeCalls">当前激活的楼层呼叫字典</param>
    public void UpdateFloorCalls(Dictionary<int, Direction[]> activeCalls)
    {
        if (InvokeRequired)
        {
            Invoke(new(() => UpdateFloorCalls(activeCalls)));
            return;
        }

        _lvFloorCalls.BeginUpdate();

        // UI 中已经存在的呼叫项集合, 格式为 "楼层-方向"
        var existingKeys = new HashSet<string>();
        foreach (ListViewItem item in _lvFloorCalls.Items)
        {
            _ = existingKeys.Add($"{item.SubItems[0].Text}-{item.SubItems[1].Text}");
        }

        // 当前有效的呼叫项集合, 格式为 "楼层-方向"
        var currentKeys = new HashSet<string>();

        // 排序好的楼层列表
        var sortedFloors = activeCalls.Keys.ToList();
        sortedFloors.Sort();

        // 添加新的呼叫项, 保持楼层顺序
        foreach (var floor in sortedFloors)
        {
            if (activeCalls.TryGetValue(floor, out var dirs))
            {
                foreach (var dir in dirs)
                {
                    var key = $"{floor}-{dir.ToSymbol()}";
                    _ = currentKeys.Add(key);

                    if (!existingKeys.Contains(key))
                    {
                        var item = new ListViewItem(floor.ToString());
                        _ = item.SubItems.Add(dir.ToSymbol());
                        _ = _lvFloorCalls.Items.Add(item);
                    }
                }
            }
        }

        // 移除已经处理完毕或取消的呼叫
        for (var i = _lvFloorCalls.Items.Count - 1; i >= 0; i--)
        {
            var item = _lvFloorCalls.Items[i];
            var key = $"{item.SubItems[0].Text}-{item.SubItems[1].Text}";
            if (!currentKeys.Contains(key))
            {
                _lvFloorCalls.Items.RemoveAt(i);
            }
        }

        _lvFloorCalls.EndUpdate();
    }

    /// <summary>
    /// 更新客户端连接状态显示
    /// </summary>
    /// <param name="clientIds">客户端ID集合</param>
    public void UpdateClients(IEnumerable<string> clientIds)
    {
        if (InvokeRequired)
        {
            Invoke(new(() => UpdateClients(clientIds)));
            return;
        }

        _lvClients.BeginUpdate();

        // 当前连接的客户端 ID 集合
        var currentIds = new HashSet<string>(clientIds);

        // 移除已经断开的行
        for (var i = _lvClients.Items.Count - 1; i >= 0; i--)
        {
            if (!currentIds.Contains(_lvClients.Items[i].Text))
            {
                _lvClients.Items.RemoveAt(i);
            }
        }

        // UI 中已经存在的客户端 ID 集合
        var existingIds = new HashSet<string>(_lvClients.Items.Cast<ListViewItem>().Select(x => x.Text));

        // 添加新连接的行
        foreach (var id in currentIds)
        {
            if (!existingIds.Contains(id))
            {
                var item = new ListViewItem(id ?? "Unknown");
                _ = item.SubItems.Add("已连接");
                _ = _lvClients.Items.Add(item);
            }
        }

        _lvClients.EndUpdate();
    }

    /// <summary>
    /// 在日志显示区域追加一条日志
    /// </summary>
    /// <param name="message">日志内容</param>
    public void Log(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new(() => Log(message)));
            return;
        }

        _rtbLogs.AppendText($"{message}\n");
        _rtbLogs.ScrollToCaret();
    }
}
