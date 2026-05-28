using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ElevatorSimulator.Server.UI;

/// <summary>
/// 主窗体类
/// </summary>
internal sealed class MainForm : Form
{
    /// <summary>
    /// 主窗体的单例
    /// </summary>
    public static MainForm Instance { get; } = new();

    /// <summary>
    /// 电梯状态列表视图
    /// </summary>
    private ListView _lvElevators;

    /// <summary>
    /// 楼层呼叫列表视图
    /// </summary>
    private ListView _lvFloorCalls;

    /// <summary>
    /// 客户端连接列表视图
    /// </summary>
    private ListView _lvClients;

    /// <summary>
    /// 日志显示区域
    /// </summary>
    private RichTextBox _rtbLogs;

    /// <summary>
    /// 日志过滤级别选择框
    /// </summary>
    private ComboBox _cmbLogFilter;

    /// <summary>
    /// 私有构造函数, 初始化主窗体
    /// </summary>
    private MainForm() => InitializeComponent();

    /// <summary>
    /// 初始化 UI 组件
    /// </summary>
    [MemberNotNull(nameof(_lvElevators), nameof(_lvFloorCalls), nameof(_lvClients), nameof(_rtbLogs), nameof(_cmbLogFilter))]
    private void InitializeComponent()
    {
        // 基本窗体设置
        Text = "电梯模拟器服务端控制台";
        Size = new(1200, 1050);
        MinimumSize = new(1200, 1050);
        StartPosition = FormStartPosition.CenterScreen;

        // 使用 TableLayoutPanel 进行布局
        var tlp = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new(10)
        };
        _ = tlp.ColumnStyles.Add(new(SizeType.Percent, 50F));
        _ = tlp.ColumnStyles.Add(new(SizeType.Percent, 50F));
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 20F));
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 30F));
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 50F));

        #region 1. 电梯状态区域
        var grpElevators = new GroupBox { Text = "电梯状态 (双击查看详情)", Dock = DockStyle.Fill };
        tlp.SetColumnSpan(grpElevators, 2);
        _lvElevators = new()
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _ = _lvElevators.Columns.Add("ID", 60);
        _ = _lvElevators.Columns.Add("当前楼层", 200);
        _ = _lvElevators.Columns.Add("运行方向", 200);
        _ = _lvElevators.Columns.Add("门状态", 200);
        _ = _lvElevators.Columns.Add("开门比例", 200);
        _lvElevators.MouseDoubleClick += (s, e) =>
        {
            if (_lvElevators.SelectedItems.Count > 0)
            {
                if (int.TryParse(_lvElevators.SelectedItems[0].Text, out var id))
                {
                    var calls = Core.ElevatorManager.Instance.Elevators[id].InternalCalls;
                    var callsStr = calls.Length == 0 ? "无" : string.Join(", ", calls);
                    _ = MessageBox.Show($"电梯 {id} 的内部呼叫目标层:\n{callsStr}", "内部呼叫明细", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        };
        grpElevators.Controls.Add(_lvElevators);
        tlp.Controls.Add(grpElevators, 0, 0);
        #endregion

        #region 2. 楼层呼叫区域
        var grpFloorCalls = new GroupBox { Text = "激活的楼层呼叫", Dock = DockStyle.Fill };
        _lvFloorCalls = new()
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _ = _lvFloorCalls.Columns.Add("楼层", 120);
        _ = _lvFloorCalls.Columns.Add("方向", 120);
        grpFloorCalls.Controls.Add(_lvFloorCalls);
        tlp.Controls.Add(grpFloorCalls, 0, 1);
        #endregion

        #region 3. 客户端连接管理区域
        var grpClients = new GroupBox { Text = "已连接的客户端 (右键管理)", Dock = DockStyle.Fill };
        _lvClients = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _ = _lvClients.Columns.Add("客户端 ID / 地址", 200);
        _ = _lvClients.Columns.Add("状态", 150);

        // 右键菜单管理客户端
        var ctxClients = new ContextMenuStrip();

        // 强制断开连接菜单项
        var menuDisconnect = new ToolStripMenuItem("强制断开连接");
        menuDisconnect.Click += (s, e) =>
        {
            if (_lvClients.SelectedItems.Count > 0)
            {
                var id = _lvClients.SelectedItems[0].Text;
                Core.PipeServerManager.Instance.DisconnectClient(id);
            }
        };

        _ = ctxClients.Items.Add(menuDisconnect);
        _lvClients.ContextMenuStrip = ctxClients;
        grpClients.Controls.Add(_lvClients);
        tlp.Controls.Add(grpClients, 1, 1);
        #endregion

        #region 4. 实时日志区域
        var grpLogs = new GroupBox { Text = "系统运行日志", Dock = DockStyle.Fill };
        tlp.SetColumnSpan(grpLogs, 2);
        var flpLogTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 60, Padding = new(0, 5, 0, 5) };
        var lblFilter = new Label { Text = "过滤级别:", AutoSize = true, Margin = new(5, 8, 5, 5) };
        _cmbLogFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, Margin = new(5, 5, 5, 5) };
        _cmbLogFilter.Items.AddRange(Utils.EnumItem<Enums.LogLevel>.GetAll());
        _cmbLogFilter.SelectedIndex = 0;
        _cmbLogFilter.SelectedIndexChanged += (s, e) =>
        {
            if (_cmbLogFilter.SelectedItem is Utils.EnumItem<Enums.LogLevel> item)
            {
                Utils.Logger.CurrentLevel = item.Value;
            }
        };
        var btnClearLog = new Button { Text = "清空日志", AutoSize = true, Margin = new(5, 5, 5, 5) };

        flpLogTop.Controls.Add(lblFilter);
        flpLogTop.Controls.Add(_cmbLogFilter);
        flpLogTop.Controls.Add(btnClearLog);

        _rtbLogs = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = System.Drawing.Color.White,
            Margin = new(3, 10, 3, 3)
        };
        btnClearLog.Click += (s, e) => _rtbLogs.Clear();
        grpLogs.Controls.Add(_rtbLogs);
        grpLogs.Controls.Add(flpLogTop);
        tlp.Controls.Add(grpLogs, 0, 2);
        #endregion

        Controls.Add(tlp);
    }

    /// <summary>
    /// 窗体加载完毕时启动命名管道服务
    /// </summary>
    /// <param name="e">事件参数</param>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _ = Core.ElevatorManager.Instance;
        _ = Core.PipeServerManager.Instance;
    }

    /// <summary>
    /// 窗体关闭时停止命名管道服务
    /// </summary>
    /// <param name="e">事件参数</param>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Core.PipeServerManager.Instance.Dispose();
        Core.ElevatorManager.Instance.Dispose();
        base.OnFormClosed(e);
    }

    /// <summary>
    /// 更新电梯状态显示
    /// </summary>
    /// <param name="elevators">电梯状态数组</param>
    public void UpdateElevatorStatus(Models.ElevatorState[] elevators)
    {
        if (InvokeRequired)
        {
            Invoke(new(() => UpdateElevatorStatus(elevators)));
            return;
        }

        _lvElevators.BeginUpdate();

        // 若列表为空则初始化占位行
        if (_lvElevators.Items.Count != elevators.Length)
        {
            _lvElevators.Items.Clear();
            for (var i = 0; i < elevators.Length; i++)
            {
                _ = _lvElevators.Items.Add(new ListViewItem(new string[5]));
            }
        }

        for (var i = 0; i < elevators.Length; i++)
        {
            // 仅对属性进行覆盖更新，避免画面闪烁
            _lvElevators.Items[i].SubItems[0].Text = elevators[i].Id.ToString();
            _lvElevators.Items[i].SubItems[1].Text = elevators[i].CurrentFloor.ToString();
            _lvElevators.Items[i].SubItems[2].Text = elevators[i].MovingDirection.ToString();
            _lvElevators.Items[i].SubItems[3].Text = elevators[i].Door.ToString();
            _lvElevators.Items[i].SubItems[4].Text = elevators[i].DoorOpenRatio.ToString("P0");
        }

        _lvElevators.EndUpdate();
    }

    /// <summary>
    /// 更新楼层呼叫显示
    /// </summary>
    /// <param name="activeCalls">当前激活的楼层呼叫字典</param>
    public void UpdateFloorCalls(Dictionary<int, Share.Direction[]> activeCalls)
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
                    var key = $"{floor}-{dir}";
                    _ = currentKeys.Add(key);

                    if (!existingKeys.Contains(key))
                    {
                        var item = new ListViewItem(floor.ToString());
                        _ = item.SubItems.Add(dir.ToString());
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

        _rtbLogs.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
        _rtbLogs.ScrollToCaret();
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
