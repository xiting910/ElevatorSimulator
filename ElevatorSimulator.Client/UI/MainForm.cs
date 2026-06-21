using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace ElevatorSimulator.Client.UI;

/// <summary>
/// 主窗口类
/// </summary>
public sealed partial class MainForm : Form
{
    /// <summary> 主视图模型, 封装客户端管理器的交互逻辑 </summary>
    private readonly ViewModels.Interfaces.IMainViewModel _viewModel;

    /// <summary> 欢迎界面面板 </summary>
    private Panel _pnlWelcome;

    /// <summary> 服务端地址输入框 </summary>
    private TextBox _txtServerAddress;

    /// <summary> 服务端端口输入框 </summary>
    private TextBox _txtServerPort;

    /// <summary> 连接状态显示标签 </summary>
    private Label _lblConnStatus;

    /// <summary> 楼层选择数值框 </summary>
    private NumericUpDown _nudFloor;

    /// <summary> 随机楼层按钮 </summary>
    private Button _btnRandomFloor;

    /// <summary> 确认进入按钮 </summary>
    private Button _btnConfirmFloor;

    /// <summary> 关闭/开启连接切换按钮 </summary>
    private CheckBox _btnToggleConnection;

    /// <summary> 楼层等待界面面板 </summary>
    private Panel _pnlFloor;

    /// <summary> 当前楼层标签 </summary>
    private Label _lblCurrentFloor;

    /// <summary> 向上呼叫/取消切换按钮</summary>
    private CheckBox _btnCallUp;

    /// <summary> 向下呼叫/取消切换按钮</summary>
    private CheckBox _btnCallDown;

    /// <summary> 电梯状态绘制面板数组 </summary>
    private ElevatorDrawPanel[] _elevatorPanels;

    /// <summary> 进入电梯按钮数组 </summary>
    private Button[] _btnEnterElevators;

    /// <summary> 电梯内部界面面板 </summary>
    private Panel _pnlElevator;

    /// <summary> 电梯信息标签 </summary>
    private Label _lblElevatorInfo;

    /// <summary> 门状态动画绘制面板 </summary>
    private ElevatorDrawPanel _pnlInsideDoor;

    /// <summary> 楼层按钮面板 </summary>
    private FlowLayoutPanel _flpFloorButtons;

    /// <summary> 退出电梯按钮 </summary>
    private Button _btnExitElevator;

    /// <summary> 开门按钮 </summary>
    private Button _btnDoorOpen;

    /// <summary> 关门按钮 </summary>
    private Button _btnDoorClose;

    /// <summary>
    /// 当前显示的面板类型
    /// </summary>
    private Enums.PanelType _currentPanel;

    /// <summary>
    /// 面板注册字典, 将 <see cref="Enums.PanelType"/> 映射到对应的面板控件及刷新/动画回调
    /// </summary>
    private readonly Dictionary<Enums.PanelType, (Panel Panel, Action? OnShow, Action? OnAnimate)> _panelRegistry;

    /// <summary>
    /// 用于绘制电梯门的定时器
    /// </summary>
    private readonly Timer _animationTimer = new() { Interval = Constants.UpdateInterval };

    /// <summary>
    /// 构造函数, 通过依赖注入接收主视图模型接口
    /// </summary>
    /// <param name="viewModel">主视图模型</param>
    public MainForm(ViewModels.Interfaces.IMainViewModel viewModel)
    {
        _viewModel = viewModel;
        _panelRegistry = [];
        InitializeComponent();
    }

    /// <summary>
    /// 初始化 UI 组件
    /// </summary>
    [MemberNotNull(
        nameof(_pnlWelcome), nameof(_txtServerAddress), nameof(_txtServerPort), nameof(_lblConnStatus), nameof(_nudFloor), nameof(_btnRandomFloor), nameof(_btnConfirmFloor), nameof(_btnToggleConnection),
        nameof(_pnlFloor), nameof(_lblCurrentFloor), nameof(_btnCallUp), nameof(_btnCallDown), nameof(_elevatorPanels), nameof(_btnEnterElevators),
        nameof(_pnlElevator), nameof(_lblElevatorInfo), nameof(_pnlInsideDoor), nameof(_flpFloorButtons), nameof(_btnExitElevator), nameof(_btnDoorOpen), nameof(_btnDoorClose)
        )]
    private void InitializeComponent()
    {
        // 窗体基本属性
        Size = new(900, 750);
        MinimumSize = new(900, 750);
        StartPosition = FormStartPosition.CenterScreen;

        // 构建各个面板
        BuildWelcomePanel();
        BuildFloorPanel();
        BuildElevatorPanel();

        // 注册面板到字典, 用于统一的切换和动画调度
        _panelRegistry[Enums.PanelType.Welcome] = (_pnlWelcome, null, null);
        _panelRegistry[Enums.PanelType.Floor] = (_pnlFloor, UpdateFloorPanelState,
            () => { foreach (var p in _elevatorPanels) { p.Invalidate(); } }
        );
        _panelRegistry[Enums.PanelType.Elevator] = (_pnlElevator, UpdateElevatorInsidePanel, _pnlInsideDoor.Invalidate);

        // 将所有面板添加到窗体
        Controls.Add(_pnlWelcome);
        Controls.Add(_pnlFloor);
        Controls.Add(_pnlElevator);

        // 默认显示欢迎界面
        ShowPanel(Enums.PanelType.Welcome);
    }

    /// <summary>
    /// 重写 <see cref="Form.OnLoad"/> 方法, 在窗体加载时订阅 ViewModel 事件, 初始化面板并自动连接服务器
    /// </summary>
    /// <param name="e">事件参数</param>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        _animationTimer.Tick += OnAnimationTimerTick;
        _animationTimer.Start();

        // 订阅 ViewModel 事件, 将数据变更转发到 UI 更新方法
        _viewModel.ConnectionStateChanged += UpdateConnectionState;
        _viewModel.FloorPanelStateChanged += UpdateFloorPanelState;
        _viewModel.ElevatorPanelStateChanged += UpdateElevatorInsidePanel;
        _viewModel.PanelSwitchRequested += ShowPanel;

        for (var i = 0; i < Constants.ElevatorCount; i++)
        {
            _elevatorPanels[i].ElevatorId = i;
        }
        _pnlInsideDoor.ElevatorId = -1;

        Text = $"电梯模拟器客户端 (ID: {_viewModel.ClientId[..8]})";

        _txtServerAddress.Text = Constants.DefaultServerAddress;
        _txtServerPort.Text = Constants.DefaultServerPort.ToString();
    }

    /// <summary>
    /// 重写 <see cref="Form.OnFormClosed"/> 方法, 在窗体关闭时停止定时器并释放 ViewModel 资源
    /// </summary>
    /// <param name="e">事件参数</param>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _animationTimer.Stop();
        _viewModel.Dispose();
        base.OnFormClosed(e);
    }
}
