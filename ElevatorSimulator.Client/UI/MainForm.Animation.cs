using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ElevatorSimulator.Client.UI;

// 动画定时器, 方向符号, 窗口拖拽限制
public sealed partial class MainForm
{
    /// <summary>
    /// 动画定时器 <see cref="Timer.Tick"/> 事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnAnimationTimerTick(object? sender, EventArgs e) => _panelRegistry[_currentPanel].OnAnimate?.Invoke();

    /// <summary>
    /// 用于处理 WM_MOVING 消息的结构体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        /// <summary> 左边界 </summary>
        public int Left;

        /// <summary> 上边界 </summary>
        public int Top;

        /// <summary> 右边界 </summary>
        public int Right;

        /// <summary> 下边界 </summary>
        public int Bottom;
    }

    /// <summary>
    /// 重写 <see cref="Form.WndProc"/> 方法, 处理 WM_MOVING 消息, 用于使窗口保持在可见区域内
    /// </summary>
    /// <param name="m">Windows 消息</param>
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0216)
        {
            var rect = Marshal.PtrToStructure<RECT>(m.LParam);
            var workingArea = Screen.GetWorkingArea(this);

            if (rect.Left < workingArea.Left)
            {
                rect.Right = workingArea.Left + (rect.Right - rect.Left);
                rect.Left = workingArea.Left;
            }
            if (rect.Top < workingArea.Top)
            {
                rect.Bottom = workingArea.Top + (rect.Bottom - rect.Top);
                rect.Top = workingArea.Top;
            }
            if (rect.Right > workingArea.Right)
            {
                rect.Left = workingArea.Right - (rect.Right - rect.Left);
                rect.Right = workingArea.Right;
            }
            if (rect.Bottom > workingArea.Bottom)
            {
                rect.Top = workingArea.Bottom - (rect.Bottom - rect.Top);
                rect.Bottom = workingArea.Bottom;
            }

            Marshal.StructureToPtr(rect, m.LParam, true);
        }

        base.WndProc(ref m);
    }
}
