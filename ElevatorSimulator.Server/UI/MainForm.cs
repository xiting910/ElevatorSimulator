using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace ElevatorSimulator.Server.UI;

/// <summary>
/// 主窗体类
/// </summary>
public sealed partial class MainForm : Form
{
    /// <summary>
    /// 主视图模型, 作为服务层与 UI 层之间的桥梁
    /// </summary>
    private readonly ViewModels.Interfaces.IMainViewModel _viewModel;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="viewModel">主视图模型</param>
    public MainForm(ViewModels.Interfaces.IMainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
    }

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
        _ = tlp.ColumnStyles.Add(new(SizeType.Percent, 25F));
        _ = tlp.ColumnStyles.Add(new(SizeType.Percent, 75F));
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 20F));
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 30F));
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 50F));

        #region 1. 电梯状态区域
        var grpElevators = new GroupBox { Text = "电梯状态", Dock = DockStyle.Fill };
        tlp.SetColumnSpan(grpElevators, 2);
        _lvElevators = new()
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            ShowItemToolTips = true,
            OwnerDraw = true
        };
        _lvElevators.DrawColumnHeader += ListView_DrawColumnHeader;
        _lvElevators.DrawSubItem += ListView_DrawSubItem;
        _ = _lvElevators.Columns.Add("ID", 100);
        _ = _lvElevators.Columns.Add("当前楼层", 160);
        _ = _lvElevators.Columns.Add("运行方向", 160);
        _ = _lvElevators.Columns.Add("门状态", 140);
        _ = _lvElevators.Columns.Add("开门比例", 160);
        _ = _lvElevators.Columns.Add("内部呼叫", 300);
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
            GridLines = true,
            OwnerDraw = true
        };
        _lvFloorCalls.DrawColumnHeader += ListView_DrawColumnHeader;
        _lvFloorCalls.DrawSubItem += ListView_DrawSubItem;
        _ = _lvFloorCalls.Columns.Add("楼层", 132);
        _ = _lvFloorCalls.Columns.Add("方向", 132);
        grpFloorCalls.Controls.Add(_lvFloorCalls);
        tlp.Controls.Add(grpFloorCalls, 0, 1);
        #endregion

        #region 3. 客户端连接管理区域
        var grpClients = new GroupBox { Text = "已连接的客户端", Dock = DockStyle.Fill };
        _lvClients = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            OwnerDraw = true
        };
        _lvClients.DrawColumnHeader += ListView_DrawColumnHeader;
        _lvClients.DrawSubItem += ListView_DrawSubItem;
        _ = _lvClients.Columns.Add("客户端 ID", 600);
        _ = _lvClients.Columns.Add("状态", 150);

        // 右键菜单管理客户端
        var ctxClients = new ContextMenuStrip();

        // 强制断开连接菜单项
        var menuDisconnect = new ToolStripMenuItem($"强制断开连接, 并加入黑名单 {Core.Networking.TcpServerManager.BlacklistDurationSeconds} 秒");
        menuDisconnect.Click += (s, e) =>
        {
            if (_lvClients.SelectedItems.Count > 0)
            {
                var id = _lvClients.SelectedItems[0].Text;
                _viewModel.DisconnectClient(id);
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
        _cmbLogFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180, Margin = new(5, 5, 5, 5) };
        _cmbLogFilter.Items.AddRange([
            new Utils.EnumItem<LogLevel>(LogLevel.Information, "信息 (Info)"),
            new Utils.EnumItem<LogLevel>(LogLevel.Warning, "警告 (Warn)"),
            new Utils.EnumItem<LogLevel>(LogLevel.Error, "错误 (Error)"),
        ]);
        _cmbLogFilter.SelectedIndex = 0;
        _cmbLogFilter.SelectedIndexChanged += (s, e) =>
        {
            if (_cmbLogFilter.SelectedItem is Utils.EnumItem<LogLevel> item)
            {
                _viewModel.SetLogLevel(item.Value);
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
            BackColor = Color.White,
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
    /// 重写 <see cref="Form.OnLoad"/> 方法, 在窗体加载时订阅事件并启动服务
    /// </summary>
    /// <param name="e">事件参数</param>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // 订阅 ViewModel 事件, 将服务层数据变更转发到 UI 更新方法
        _viewModel.ElevatorStatusChanged += UpdateElevatorStatus;
        _viewModel.FloorCallsChanged += UpdateFloorCalls;
        _viewModel.ClientListChanged += UpdateClients;
        _viewModel.LogReceived += Log;

        // 启动服务
        _viewModel.Start();
    }

    /// <summary>
    /// 重写 <see cref="Form.OnFormClosed"/> 方法, 在窗体关闭时进行资源清理
    /// </summary>
    /// <param name="e">事件参数</param>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _viewModel.Dispose();
        base.OnFormClosed(e);
    }
}
