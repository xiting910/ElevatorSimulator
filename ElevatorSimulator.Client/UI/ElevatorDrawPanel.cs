using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ElevatorSimulator.Client.UI;

/// <summary>
/// 电梯门动画绘制面板, 使用 GDI+ 绘制电梯门开关动画
/// </summary>
public sealed class ElevatorDrawPanel : Panel
{
    /// <summary>
    /// 电梯 ID, 用于外部布局区分, -1 表示内部电梯视图
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ElevatorId { get; set; } = -1;

    /// <summary>
    /// 当前的电梯状态
    /// </summary>
    private Messages.ElevatorStatusMessage? _status;

    /// <summary>
    /// 电梯是否在当前用户所在楼层(决定是否显示门动画)
    /// </summary>
    private bool _showDoor;

    /// <summary>
    /// 电梯井道背景画刷
    /// </summary>
    private readonly SolidBrush _shaftBrush = new(Color.FromArgb(200, 200, 210));

    /// <summary>
    /// 门板画刷
    /// </summary>
    private readonly SolidBrush _doorBrush = new(Color.FromArgb(140, 155, 170));

    /// <summary>
    /// 门内边缘画笔 (4px)
    /// </summary>
    private readonly Pen _innerEdgePen = new(Color.FromArgb(50, 50, 65), 4f);

    /// <summary>
    /// 外层门框画笔 (10px)
    /// </summary>
    private readonly Pen _outerFramePen = new(Color.FromArgb(50, 50, 65), 10f);

    /// <summary>
    /// 构造函数, 启用双缓冲以减少闪烁
    /// </summary>
    public ElevatorDrawPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    /// <summary>
    /// 更新电梯状态数据
    /// </summary>
    /// <param name="status">电梯状态消息</param>
    /// <param name="showDoor">是否显示门动画</param>
    public void UpdateStatus(Messages.ElevatorStatusMessage status, bool showDoor)
    {
        _status = status;
        _showDoor = showDoor;
        Invalidate();
    }

    /// <summary>
    /// 重写 <see cref="Control.OnPaint"/> 方法, 绘制电梯门动画
    /// </summary>
    /// <param name="e">绘制事件参数</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_status is null) { return; }

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var w = (float)ClientSize.Width;
        var h = (float)ClientSize.Height;

        // 绘制电梯井道背景
        g.FillRectangle(_shaftBrush, 0, 0, w, h);

        if (_showDoor)
        {
            var ratio = Math.Clamp(_status.DoorOpenRatio, 0.0, 1.0);
            var halfWidth = w / 2f;
            var openOffset = halfWidth * (float)ratio;
            var leftWidth = halfWidth - openOffset;
            var rightWidth = halfWidth - openOffset;

            // 左门门板
            g.FillRectangle(_doorBrush, 0, 0, leftWidth, h);
            g.DrawLine(_innerEdgePen, leftWidth, 0, leftWidth, h);

            // 右门门板
            var rightX = w - rightWidth;
            g.FillRectangle(_doorBrush, rightX, 0, rightWidth, h);
            g.DrawLine(_innerEdgePen, rightX, 0, rightX, h);
        }
        else
        {
            // 不在当前楼层, 门始终关闭
            var halfWidth = w / 2f;

            g.FillRectangle(_doorBrush, 0, 0, halfWidth, h);
            g.FillRectangle(_doorBrush, halfWidth, 0, halfWidth, h);
            g.DrawLine(_innerEdgePen, halfWidth, 0, halfWidth, h);
        }

        // 固定的外层门框
        g.DrawRectangle(_outerFramePen, 0, 0, w, h);
    }

    /// <summary>
    /// 重写 <see cref="Control.Dispose(bool)"/> 方法, 释放 GDI+ 资源
    /// </summary>
    /// <param name="disposing">是否正在释放托管资源</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _shaftBrush.Dispose();
            _doorBrush.Dispose();
            _innerEdgePen.Dispose();
            _outerFramePen.Dispose();
        }
        base.Dispose(disposing);
    }
}
