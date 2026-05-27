using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ElevatorSimulator.Client.UI;

/// <summary>
/// 主窗口类, 采用单例模式
/// </summary>
internal sealed class MainForm : Form
{
    /// <summary>
    /// 获取主窗体的单例实例
    /// </summary>
    public static MainForm Instance { get; } = new();

    /// <summary>
    /// 私有构造函数
    /// </summary>
    private MainForm()
    {

    }

    /// <summary>
    /// 窗体加载完毕时订阅客户端管理器的事件
    /// </summary>
    /// <param name="e">事件参数</param>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Core.ClientManager.Instance.OnElevatorStatusReceived += OnElevatorStatusReceived;
        Core.ClientManager.Instance.OnFloorStatusReceived += OnFloorStatusReceived;
        Core.ClientManager.Instance.OnDisconnected += OnDisconnected;
    }

    /// <summary>
    /// 窗体关闭时释放客户端管理器资源
    /// </summary>
    /// <param name="e">事件参数</param>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Core.ClientManager.Instance.Dispose();
        base.OnFormClosed(e);
    }

    /// <summary>
    /// 处理电梯状态更新事件, 在 UI 线程上执行
    /// </summary>
    /// <param name="msg">电梯状态消息</param>
    private void OnElevatorStatusReceived(Share.ElevatorStatusMessage msg)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnElevatorStatusReceived(msg));
            return;
        }
    }

    /// <summary>
    /// 处理楼层状态更新事件, 在 UI 线程上执行
    /// </summary>
    /// <param name="msg">楼层状态消息</param>
    private void OnFloorStatusReceived(Share.FloorStatusMessage msg)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnFloorStatusReceived(msg));
            return;
        }
    }

    /// <summary>
    /// 处理连接断开事件, 在 UI 线程上执行
    /// </summary>
    private void OnDisconnected()
    {
        if (InvokeRequired)
        {
            Invoke(OnDisconnected);
            return;
        }
    }

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
