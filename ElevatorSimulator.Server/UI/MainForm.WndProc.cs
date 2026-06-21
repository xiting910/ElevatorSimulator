using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ElevatorSimulator.Server.UI;

// WM_MOVING 窗口拖拽限制
public sealed partial class MainForm
{
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
            // 获取窗口的当前位置
            var rect = Marshal.PtrToStructure<RECT>(m.LParam);

            // 获取当前屏幕的工作区域
            var workingArea = Screen.GetWorkingArea(this);

            // 调整位置
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

            // 将调整后的位置写回消息
            Marshal.StructureToPtr(rect, m.LParam, true);
        }

        base.WndProc(ref m);
    }
}
