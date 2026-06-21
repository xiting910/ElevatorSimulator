using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace ElevatorSimulator.Client.UI;

// 三个面板的构建方法
public sealed partial class MainForm
{
    /// <summary>
    /// 构建欢迎界面面板
    /// </summary>
    [MemberNotNull(nameof(_pnlWelcome), nameof(_txtServerAddress), nameof(_txtServerPort), nameof(_lblConnStatus), nameof(_nudFloor), nameof(_btnRandomFloor), nameof(_btnConfirmFloor), nameof(_btnToggleConnection))]
    private void BuildWelcomePanel()
    {
        _pnlWelcome = new() { Dock = DockStyle.Fill, Visible = false };

        var tlp = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new(40)
        };
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 30F));
        _ = tlp.RowStyles.Add(new(SizeType.Absolute, 60F));
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 20F));
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 20F));
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 25F));
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 5F));

        var lblTitle = new Label
        {
            Text = $"欢迎使用 {Constants.AppName}",
            Font = new("Microsoft YaHei UI", 20F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        tlp.Controls.Add(lblTitle, 0, 0);

        var pnlServerInput = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Anchor = AnchorStyles.None,
            Padding = new(0, 5, 0, 0)
        };
        var lblAddr = new Label
        {
            Text = "服务器:",
            Font = new("Microsoft YaHei UI", 10F),
            AutoSize = true,
            Margin = new(0, 5, 5, 0)
        };
        _txtServerAddress = new()
        {
            Width = 300,
            Font = new("Microsoft YaHei UI", 10F)
        };
        var lblPort = new Label
        {
            Text = " 端口:",
            Font = new("Microsoft YaHei UI", 10F),
            AutoSize = true,
            Margin = new(10, 5, 5, 0)
        };
        _txtServerPort = new()
        {
            Width = 150,
            Font = new("Microsoft YaHei UI", 10F)
        };
        _txtServerAddress.TextChanged += OnServerInputChanged;
        _txtServerPort.TextChanged += OnServerInputChanged;
        pnlServerInput.Controls.Add(lblAddr);
        pnlServerInput.Controls.Add(_txtServerAddress);
        pnlServerInput.Controls.Add(lblPort);
        pnlServerInput.Controls.Add(_txtServerPort);
        tlp.Controls.Add(pnlServerInput, 0, 1);

        _lblConnStatus = new()
        {
            Text = "正在连接...",
            Font = new("Microsoft YaHei UI", 12F),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            ForeColor = Color.DodgerBlue
        };
        tlp.Controls.Add(_lblConnStatus, 0, 2);

        var pnlFloorSelect = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Anchor = AnchorStyles.None,
            Padding = new(0, 10, 0, 0)
        };
        var lblFloor = new Label
        {
            Text = "请选择您要进入的楼层:",
            Font = new("Microsoft YaHei UI", 11F),
            AutoSize = true,
            Margin = new(0, 5, 10, 0)
        };
        _nudFloor = new()
        {
            Minimum = Constants.MinFloor,
            Maximum = Constants.MaxFloor,
            Width = 80,
            Font = new("Microsoft YaHei UI", 11F)
        };
        _btnRandomFloor = new()
        {
            Text = "随机",
            AutoSize = true,
            Font = new("Microsoft YaHei UI", 10F),
            Margin = new(10, 0, 0, 0)
        };
        _btnRandomFloor.Click += (s, e) => _nudFloor.Value = Random.Shared.Next(Constants.MinFloor, Constants.MaxFloor + 1);
        pnlFloorSelect.Controls.Add(lblFloor);
        pnlFloorSelect.Controls.Add(_nudFloor);
        pnlFloorSelect.Controls.Add(_btnRandomFloor);
        tlp.Controls.Add(pnlFloorSelect, 0, 3);

        _btnToggleConnection = new()
        {
            Text = "连接: 已开启",
            Font = new("Microsoft YaHei UI", 12F, FontStyle.Bold),
            BackColor = Color.LightGreen,
            AutoSize = true,
            Anchor = AnchorStyles.None,
            Appearance = Appearance.Button,
            Checked = true
        };
        _btnToggleConnection.CheckedChanged += (s, e) =>
        {
            _btnToggleConnection.Text = _btnToggleConnection.Checked ? "连接: 已开启" : "连接: 已关闭";
            _btnToggleConnection.BackColor = _btnToggleConnection.Checked ? Color.LightGreen : Color.LightGray;
            _viewModel.SetConnectionEnabled(_btnToggleConnection.Checked);
        };

        _btnConfirmFloor = new()
        {
            Text = "进入楼层",
            Font = new("Microsoft YaHei UI", 12F, FontStyle.Bold),
            AutoSize = true,
            Enabled = false,
            Margin = new(30, 0, 0, 0),
            Anchor = AnchorStyles.None
        };
        _btnConfirmFloor.Click += (s, e) => _viewModel.ConfirmFloor((int)_nudFloor.Value);

        var pnlActionButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            Anchor = AnchorStyles.None,
            FlowDirection = FlowDirection.LeftToRight
        };
        pnlActionButtons.Controls.Add(_btnToggleConnection);
        pnlActionButtons.Controls.Add(_btnConfirmFloor);
        tlp.Controls.Add(pnlActionButtons, 0, 4);

        _pnlWelcome.Controls.Add(tlp);
    }

    /// <summary>
    /// 构建楼层等待界面面板
    /// </summary>
    [MemberNotNull(nameof(_pnlFloor), nameof(_lblCurrentFloor), nameof(_btnCallUp), nameof(_btnCallDown), nameof(_elevatorPanels), nameof(_btnEnterElevators))]
    private void BuildFloorPanel()
    {
        _pnlFloor = new() { Dock = DockStyle.Fill, Visible = false };

        var tlp = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new(10)
        };
        _ = tlp.RowStyles.Add(new(SizeType.Absolute, 70F));
        _ = tlp.RowStyles.Add(new(SizeType.Absolute, 90F));
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 100F));
        _ = tlp.RowStyles.Add(new(SizeType.AutoSize));

        var pnlTop = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new(5)
        };
        _ = pnlTop.ColumnStyles.Add(new(SizeType.Percent, 100F));
        _ = pnlTop.ColumnStyles.Add(new(SizeType.AutoSize));
        _lblCurrentFloor = new()
        {
            Text = "当前楼层: --",
            Font = new("Microsoft YaHei UI", 14F, FontStyle.Bold),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new(0, 5, 0, 0)
        };
        var btnBackToWelcome = new Button
        {
            Text = "返回选择楼层",
            AutoSize = true,
            Font = new("Microsoft YaHei UI", 9F),
            Anchor = AnchorStyles.Right
        };
        btnBackToWelcome.Click += (s, e) => _viewModel.GoToWelcome();
        pnlTop.Controls.Add(_lblCurrentFloor, 0, 0);
        pnlTop.Controls.Add(btnBackToWelcome, 1, 0);
        tlp.Controls.Add(pnlTop, 0, 0);

        var pnlStatus = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = Constants.ElevatorCount,
            RowCount = 1,
            Margin = new(3)
        };
        _ = pnlStatus.ColumnStyles.Add(new(SizeType.Percent, 33.33F));
        _ = pnlStatus.ColumnStyles.Add(new(SizeType.Percent, 33.33F));
        _ = pnlStatus.ColumnStyles.Add(new(SizeType.Percent, 33.34F));
        var lblStatuses = new Label[Constants.ElevatorCount];
        for (var i = 0; i < Constants.ElevatorCount; i++)
        {
            lblStatuses[i] = new()
            {
                Name = $"lblStatus_{i}",
                Text = $"电梯 {i + 1}: --",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new("Microsoft YaHei UI", 11F, FontStyle.Bold),
                ForeColor = Color.Black,
                Margin = new(3)
            };
            pnlStatus.Controls.Add(lblStatuses[i], i, 0);
        }
        tlp.Controls.Add(pnlStatus, 0, 1);

        var tlpDoors = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = Constants.ElevatorCount,
            RowCount = 1,
            Margin = new(0)
        };
        _ = tlpDoors.ColumnStyles.Add(new(SizeType.Percent, 33.33F));
        _ = tlpDoors.ColumnStyles.Add(new(SizeType.Percent, 33.33F));
        _ = tlpDoors.ColumnStyles.Add(new(SizeType.Percent, 33.34F));
        _elevatorPanels = new ElevatorDrawPanel[Constants.ElevatorCount];
        _btnEnterElevators = new Button[Constants.ElevatorCount];
        for (var i = 0; i < Constants.ElevatorCount; i++)
        {
            var elevatorId = i;

            var pnlDoorGroup = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new(5)
            };
            _ = pnlDoorGroup.RowStyles.Add(new(SizeType.Percent, 100F));
            _ = pnlDoorGroup.RowStyles.Add(new(SizeType.Absolute, 50F));

            _elevatorPanels[elevatorId] = new()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 240, 240)
            };
            pnlDoorGroup.Controls.Add(_elevatorPanels[elevatorId], 0, 0);

            _btnEnterElevators[elevatorId] = new()
            {
                Text = "进入电梯",
                Dock = DockStyle.Fill,
                Font = new("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Enabled = false,
                Margin = new(5, 2, 5, 5)
            };
            var capturedId = elevatorId;
            _btnEnterElevators[elevatorId].Click += (s, e) => _viewModel.EnterElevator(capturedId);
            pnlDoorGroup.Controls.Add(_btnEnterElevators[elevatorId], 0, 1);

            tlpDoors.Controls.Add(pnlDoorGroup, i, 0);
        }
        tlp.Controls.Add(tlpDoors, 0, 2);

        var pnlButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            Anchor = AnchorStyles.None,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new(5)
        };

        _btnCallUp = new()
        {
            Text = "↑",
            Size = new(70, 70),
            Font = new("Microsoft YaHei UI", 18F, FontStyle.Bold),
            ForeColor = Color.Black,
            Margin = new(8),
            TextAlign = ContentAlignment.MiddleCenter,
            Enabled = false,
            Appearance = Appearance.Button,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        _btnCallUp.FlatAppearance.BorderSize = 1;
        _btnCallUp.CheckedChanged += async (s, e) =>
        {
            _btnCallUp.BackColor = _btnCallUp.Checked ? Color.LimeGreen : SystemColors.Control;
            if (_btnCallUp.Checked == _viewModel.State.HasActiveCall(Direction.Up)) { return; }
            if (_btnCallUp.Checked)
            {
                await _viewModel.Sender.SendAsync(new Messages.ExternalCallMessage { Floor = _viewModel.State.CurrentFloor, Direction = Direction.Up });
            }
            else
            {
                await _viewModel.Sender.SendAsync(new Messages.CancelExternalCallMessage { Floor = _viewModel.State.CurrentFloor, Direction = Direction.Up });
            }
        };

        _btnCallDown = new()
        {
            Text = "↓",
            Size = new(70, 70),
            Font = new("Microsoft YaHei UI", 18F, FontStyle.Bold),
            ForeColor = Color.Black,
            Margin = new(8),
            TextAlign = ContentAlignment.MiddleCenter,
            Enabled = false,
            Appearance = Appearance.Button,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        _btnCallDown.FlatAppearance.BorderSize = 1;
        _btnCallDown.CheckedChanged += async (s, e) =>
        {
            _btnCallDown.BackColor = _btnCallDown.Checked ? Color.LimeGreen : SystemColors.Control;
            if (_btnCallDown.Checked == _viewModel.State.HasActiveCall(Direction.Down)) { return; }
            if (_btnCallDown.Checked)
            {
                await _viewModel.Sender.SendAsync(new Messages.ExternalCallMessage { Floor = _viewModel.State.CurrentFloor, Direction = Direction.Down });
            }
            else
            {
                await _viewModel.Sender.SendAsync(new Messages.CancelExternalCallMessage { Floor = _viewModel.State.CurrentFloor, Direction = Direction.Down });
            }
        };

        pnlButtons.Controls.Add(_btnCallUp);
        pnlButtons.Controls.Add(_btnCallDown);
        tlp.Controls.Add(pnlButtons, 0, 3);

        _pnlFloor.Controls.Add(tlp);
    }

    /// <summary>
    /// 构建电梯内部界面面板
    /// </summary>
    [MemberNotNull(nameof(_pnlElevator), nameof(_lblElevatorInfo), nameof(_pnlInsideDoor), nameof(_flpFloorButtons), nameof(_btnExitElevator), nameof(_btnDoorOpen), nameof(_btnDoorClose))]
    private void BuildElevatorPanel()
    {
        _pnlElevator = new() { Dock = DockStyle.Fill, Visible = false };

        var tlp = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new(10)
        };
        _ = tlp.RowStyles.Add(new(SizeType.Absolute, 55F));
        _ = tlp.RowStyles.Add(new(SizeType.Percent, 100F));

        var pnlTop = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        _ = pnlTop.ColumnStyles.Add(new(SizeType.Percent, 100F));
        _ = pnlTop.ColumnStyles.Add(new(SizeType.AutoSize));

        _lblElevatorInfo = new()
        {
            Text = "电梯信息: --",
            Dock = DockStyle.Fill,
            Font = new("Microsoft YaHei UI", 14F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlTop.Controls.Add(_lblElevatorInfo, 0, 0);

        _btnExitElevator = new()
        {
            Text = "退出电梯",
            AutoSize = true,
            Font = new("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = Color.Gray,
            Anchor = AnchorStyles.Right,
            Enabled = false
        };
        _btnExitElevator.Click += (s, e) => _viewModel.ExitElevator();
        pnlTop.Controls.Add(_btnExitElevator, 1, 0);

        tlp.Controls.Add(pnlTop, 0, 0);

        var pnlMiddle = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        _ = pnlMiddle.ColumnStyles.Add(new(SizeType.Absolute, 265F));
        _ = pnlMiddle.ColumnStyles.Add(new(SizeType.Percent, 100F));

        var pnlLeft = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new(0)
        };
        _ = pnlLeft.RowStyles.Add(new(SizeType.Percent, 100F));
        _ = pnlLeft.RowStyles.Add(new(SizeType.Absolute, 65F));

        var grpFloors = new GroupBox { Text = "", Dock = DockStyle.Fill, Margin = new(3) };
        _flpFloorButtons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoScroll = true,
            Padding = new(3),
            WrapContents = true
        };

        for (var f = Constants.MaxFloor; f >= Constants.MinFloor; f--)
        {
            var floor = f;
            var btn = new CheckBox
            {
                Text = f.ToString(),
                Width = 55,
                Height = 36,
                Font = new("Microsoft YaHei UI", 9F),
                Margin = new(2),
                Tag = floor,
                Appearance = Appearance.Button,
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            btn.CheckedChanged += async (s, e) =>
            {
                btn.BackColor = btn.Checked ? Color.LimeGreen : SystemColors.Control;
                if (_viewModel.State.CurrentElevatorId is int eid)
                {
                    var status = _viewModel.State.ElevatorStatuses[eid];
                    if (btn.Checked == status.InternalCalls.Contains(floor)) { return; }
                    if (btn.Checked)
                    {
                        await _viewModel.Sender.SendAsync(new Messages.InternalCallMessage { ElevatorId = eid, TargetFloor = floor });
                    }
                    else
                    {
                        await _viewModel.Sender.SendAsync(new Messages.CancelInternalCallMessage { ElevatorId = eid, TargetFloor = floor });
                    }
                }
            };
            _flpFloorButtons.Controls.Add(btn);
        }
        grpFloors.Controls.Add(_flpFloorButtons);
        pnlLeft.Controls.Add(grpFloors, 0, 0);

        var pnlDoorControl = new FlowLayoutPanel
        {
            AutoSize = true,
            Anchor = AnchorStyles.None,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new(3)
        };
        _btnDoorOpen = new()
        {
            Text = "◀▶",
            AutoSize = true,
            Font = new("Microsoft YaHei UI", 9F, FontStyle.Bold),
            Margin = new(5),
            Padding = new(6, 3, 6, 3),
            Enabled = false,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = true
        };
        _btnDoorOpen.Click += async (s, e) => await _viewModel.RequestDoorOpen();
        _btnDoorClose = new()
        {
            Text = "▶◀",
            AutoSize = true,
            Font = new("Microsoft YaHei UI", 9F, FontStyle.Bold),
            Margin = new(5),
            Padding = new(6, 3, 6, 3),
            Enabled = false,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = true
        };
        _btnDoorClose.Click += async (s, e) => await _viewModel.RequestDoorClose();
        pnlDoorControl.Controls.Add(_btnDoorOpen);
        pnlDoorControl.Controls.Add(_btnDoorClose);
        pnlLeft.Controls.Add(pnlDoorControl, 0, 1);

        pnlMiddle.Controls.Add(pnlLeft, 0, 0);

        _pnlInsideDoor = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 240, 240),
            Margin = new(3)
        };
        pnlMiddle.Controls.Add(_pnlInsideDoor, 1, 0);

        tlp.Controls.Add(pnlMiddle, 0, 1);

        _pnlElevator.Controls.Add(tlp);
    }
}
