using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ElevatorSimulator.Client.UI;

// UI 状态更新, 面板切换, 连接状态刷新
public sealed partial class MainForm
{
    /// <summary>
    /// 连接状态到 UI 显示信息的映射表
    /// </summary>
    private static readonly Dictionary<Enums.ConnectionStatus, (string Text, Color Color, bool ConfirmEnabled)> _connectionStateMap = new()
    {
        [Enums.ConnectionStatus.Connecting] = ("正在连接...", Color.DodgerBlue, false),
        [Enums.ConnectionStatus.Connected] = ("✓ 已连接到服务器", Color.Green, true),
        [Enums.ConnectionStatus.Reconnecting] = ("连接已断开, 正在重连...", Color.OrangeRed, false),
        [Enums.ConnectionStatus.Closed] = ("连接已关闭", Color.Gray, false),
    };

    /// <summary>
    /// 显示指定类型的面板, 通过注册字典驱动切换和刷新, 新增面板类型无需修改本方法
    /// </summary>
    /// <param name="panel">面板类型</param>
    private void ShowPanel(Enums.PanelType panel)
    {
        if (InvokeRequired) { Invoke(() => ShowPanel(panel)); return; }

        _currentPanel = panel;

        // 遍历注册字典, 仅显示目标面板, 其余全部隐藏
        foreach (var (type, (p, _, _)) in _panelRegistry)
        {
            p.Visible = type == panel;
        }

        // 调用面板对应的刷新回调
        _panelRegistry[panel].OnShow?.Invoke();
    }

    /// <summary>
    /// 服务端地址或端口输入变化时的防抖处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnServerInputChanged(object? sender, EventArgs e)
    {
        if (int.TryParse(_txtServerPort.Text, out var port))
        {
            _viewModel.OnServerInputChanged(_txtServerAddress.Text.Trim(), port);
        }
    }

    /// <summary>
    /// 更新连接相关的 UI 控件
    /// </summary>
    private void UpdateConnectionState()
    {
        if (InvokeRequired) { Invoke(UpdateConnectionState); return; }

        (_lblConnStatus.Text, _lblConnStatus.ForeColor, _btnConfirmFloor.Enabled) =
        _connectionStateMap.TryGetValue(_viewModel.Status, out var v) ? v : ("未知状态", Color.Gray, false);
    }

    /// <summary>
    /// 更新楼层等待界面
    /// </summary>
    private void UpdateFloorPanelState()
    {
        if (InvokeRequired) { Invoke(UpdateFloorPanelState); return; }

        var cm = _viewModel.State;
        _lblCurrentFloor.Text = $"当前楼层: {cm.CurrentFloor} F";

        _btnCallUp.Visible = cm.CurrentFloor < Constants.MaxFloor;
        _btnCallUp.Checked = cm.HasActiveCall(Direction.Up);
        _btnCallUp.Enabled = true;

        _btnCallDown.Visible = cm.CurrentFloor > Constants.MinFloor;
        _btnCallDown.Checked = cm.HasActiveCall(Direction.Down);
        _btnCallDown.Enabled = true;

        for (var i = 0; i < Constants.ElevatorCount; i++)
        {
            var status = cm.ElevatorStatuses[i];
            if (status is null) { continue; }

            var isAtCurrentFloor = status.CurrentFloor == cm.CurrentFloor;
            _elevatorPanels[i].UpdateStatus(status, isAtCurrentFloor);
            _btnEnterElevators[i].Enabled = cm.CanEnterElevator(i);

            var dirSymbol = status.MovingDirection.ToSymbol();
            var atFloor = isAtCurrentFloor ? " ●" : "";
            var lblStatus = (Label)_pnlFloor.Controls.Find($"lblStatus_{i}", true).FirstOrDefault()!;
            lblStatus.Text = $"电梯 {i + 1}: {status.CurrentFloor}F {dirSymbol}{atFloor}";
            lblStatus.ForeColor = isAtCurrentFloor ? Color.DarkBlue : Color.Black;
        }
    }

    /// <summary>
    /// 更新电梯内部界面
    /// </summary>
    private void UpdateElevatorInsidePanel()
    {
        if (InvokeRequired) { Invoke(UpdateElevatorInsidePanel); return; }

        var cm = _viewModel.State;
        if (cm.CurrentElevatorId is not int eid || eid < 0 || eid >= cm.ElevatorStatuses.Length) { return; }

        var status = cm.ElevatorStatuses[eid];

        _lblElevatorInfo.Text = $"电梯 {eid + 1}  |  楼层: {status.CurrentFloor}F  |  运行: {status.MovingDirection.ToSymbol()}";

        var canExit = status.Door is DoorState.Open;
        _btnExitElevator.Enabled = canExit;
        _btnExitElevator.ForeColor = canExit ? Color.DarkRed : Color.Gray;

        _pnlInsideDoor.UpdateStatus(status, true);

        var canControlDoor = status.MovingDirection is Direction.None;
        _btnDoorOpen.Enabled = canControlDoor;
        _btnDoorClose.Enabled = canControlDoor;

        var internalCalls = status.InternalCalls;
        foreach (Control ctrl in _flpFloorButtons.Controls)
        {
            if (ctrl is CheckBox cb && cb.Tag is int floor)
            {
                cb.Checked = internalCalls.Contains(floor);
            }
        }
    }
}
