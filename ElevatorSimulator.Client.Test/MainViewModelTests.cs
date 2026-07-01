using ElevatorSimulator.Client.Core.Interfaces;
using ElevatorSimulator.Client.Enums;
using ElevatorSimulator.Client.ViewModels;
using ElevatorSimulator.Share;
using ElevatorSimulator.Share.Messages;
using Microsoft.Extensions.Logging;
using Moq;

namespace ElevatorSimulator.Client.Test;

/// <summary>
/// <see cref="MainViewModel"/> 的单元测试, 验证连接控制,面板切换,电梯进出,门请求等业务逻辑
/// </summary>
public sealed class MainViewModelTests
{
    /// <summary> 连接服务的 Mock </summary>
    private readonly Mock<IConnectionService> _connectionMock;
    /// <summary> 消息发送服务的 Mock </summary>
    private readonly Mock<IMessageService> _senderMock;
    /// <summary> 客户端状态的 Mock </summary>
    private readonly Mock<IClientState> _stateMock;
    /// <summary> 日志记录器的 Mock </summary>
    private readonly Mock<ILogger<MainViewModel>> _loggerMock;
    /// <summary> 被测的视图模型实例 </summary>
    private readonly MainViewModel _vm;

    /// <summary>
    /// 初始化 Mock 与 ViewModel, 设置默认行为
    /// </summary>
    public MainViewModelTests()
    {
        _connectionMock = new Mock<IConnectionService>();
        _senderMock = new Mock<IMessageService>();
        _stateMock = new Mock<IClientState>();
        _ = _stateMock.Setup(s => s.ClientId).Returns("test-client");
        _loggerMock = new Mock<ILogger<MainViewModel>>();
        _vm = new MainViewModel(_connectionMock.Object, _senderMock.Object, _stateMock.Object, _loggerMock.Object);
    }

    /// <summary>
    /// <see cref="MainViewModel.ClientId"/> 应返回 <see cref="IClientState.ClientId"/>
    /// </summary>
    [Fact]
    public void ClientId_ReturnsStateClientId()
    {
        Assert.Equal("test-client", _vm.ClientId);
    }

    /// <summary>
    /// 初始化后 <see cref="MainViewModel.Status"/> 应为 <see cref="ConnectionStatus.Connecting"/>
    /// </summary>
    [Fact]
    public void Status_InitialValue_IsConnecting()
    {
        Assert.Equal(ConnectionStatus.Connecting, _vm.Status);
    }

    /// <summary>
    /// 启用连接时, 状态应变为 Connecting 并触发连接和状态变更事件
    /// </summary>
    [Fact]
    public void SetConnectionEnabled_True_ChangesStatusAndConnects()
    {
        // Arrange
        var changed = false;
        _vm.ConnectionStateChanged += () => changed = true;

        // Act
        _vm.SetConnectionEnabled(true);

        // Assert
        Assert.Equal(ConnectionStatus.Connecting, _vm.Status);
        Assert.True(changed);
        _connectionMock.Verify(c => c.Connect(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
    }

    /// <summary>
    /// 禁用连接时, 状态应变为 Closed 并触发断开和状态变更事件
    /// </summary>
    [Fact]
    public void SetConnectionEnabled_False_ChangesStatusAndDisconnects()
    {
        // Arrange
        var changed = false;
        _vm.ConnectionStateChanged += () => changed = true;

        // Act
        _vm.SetConnectionEnabled(false);

        // Assert
        Assert.Equal(ConnectionStatus.Closed, _vm.Status);
        Assert.True(changed);
        _connectionMock.Verify(c => c.Disconnect(), Times.Once);
    }

    /// <summary>
    /// 确认楼层时, 应更新 CurrentFloor 并请求切换到楼层面板
    /// </summary>
    [Fact]
    public void ConfirmFloor_SetsStateAndRequestsPanelSwitch()
    {
        // Arrange
        Enums.PanelType? panel = null;
        _vm.PanelSwitchRequested += p => panel = p;

        // Act
        _vm.ConfirmFloor(5);

        // Assert
        _stateMock.VerifySet(s => s.CurrentFloor = 5, Times.Once);
        Assert.Equal(Enums.PanelType.Floor, panel);
    }

    /// <summary>
    /// 可进入电梯时, 应设置 CurrentElevatorId 并切换到电梯面板
    /// </summary>
    [Fact]
    public void EnterElevator_WhenCanEnter_EntersAndSwitchesPanel()
    {
        // Arrange
        _ = _stateMock.Setup(s => s.CanEnterElevator(0)).Returns(true);
        Enums.PanelType? panel = null;
        _vm.PanelSwitchRequested += p => panel = p;

        // Act
        _vm.EnterElevator(0);

        // Assert
        _stateMock.VerifySet(s => s.CurrentElevatorId = 0, Times.Once);
        Assert.Equal(Enums.PanelType.Elevator, panel);
    }

    /// <summary>
    /// 不可进入电梯时, 不应设置 CurrentElevatorId 也不切换面板
    /// </summary>
    [Fact]
    public void EnterElevator_WhenCannotEnter_DoesNothing()
    {
        // Arrange
        _ = _stateMock.Setup(s => s.CanEnterElevator(0)).Returns(false);
        var fired = false;
        _vm.PanelSwitchRequested += _ => fired = true;

        // Act
        _vm.EnterElevator(0);

        // Assert
        Assert.False(fired);
    }

    /// <summary>
    /// 可离开电梯时, 应清空 CurrentElevatorId 并切换到楼层面板
    /// </summary>
    [Fact]
    public void ExitElevator_WhenCanExit_ExitsAndSwitchesPanel()
    {
        // Arrange
        _ = _stateMock.Setup(s => s.CurrentElevatorId).Returns(1);
        _ = _stateMock.Setup(s => s.CanExitElevator()).Returns(true);
        _ = _stateMock.Setup(s => s.ElevatorStatuses).Returns(new ElevatorStatusMessage[Constants.ElevatorCount]);
        _stateMock.Object.ElevatorStatuses[1] = new() { Id = 1, CurrentFloor = 10 };
        Enums.PanelType? panel = null;
        _vm.PanelSwitchRequested += p => panel = p;

        // Act
        _vm.ExitElevator();

        // Assert
        _stateMock.VerifySet(s => s.CurrentElevatorId = null, Times.Once);
        Assert.Equal(Enums.PanelType.Floor, panel);
    }

    /// <summary>
    /// 不在电梯内时, 不应触发任何面板切换
    /// </summary>
    [Fact]
    public void ExitElevator_WhenNotInElevator_DoesNothing()
    {
        // Arrange
        _ = _stateMock.Setup(s => s.CurrentElevatorId).Returns((int?)null);
        var fired = false;
        _vm.PanelSwitchRequested += _ => fired = true;

        // Act
        _vm.ExitElevator();

        // Assert
        Assert.False(fired);
    }

    /// <summary>
    /// 在电梯内时, 开门请求应发送 <see cref="RequestDoorOpenMessage"/>
    /// </summary>
    [Fact]
    public async Task RequestDoorOpen_WhenInElevator_SendsMessage()
    {
        // Arrange
        _ = _stateMock.Setup(s => s.CurrentElevatorId).Returns(1);

        // Act
        await _vm.RequestDoorOpen();

        // Assert
        _senderMock.Verify(s => s.SendAsync(It.Is<RequestDoorOpenMessage>(m => m.ElevatorId == 1)), Times.Once);
    }

    /// <summary>
    /// 不在电梯内时, 开门请求不应发送任何消息
    /// </summary>
    [Fact]
    public async Task RequestDoorOpen_WhenNotInElevator_DoesNotSend()
    {
        // Arrange
        _ = _stateMock.Setup(s => s.CurrentElevatorId).Returns((int?)null);

        // Act
        await _vm.RequestDoorOpen();

        // Assert
        _senderMock.Verify(s => s.SendAsync(It.IsAny<Message>()), Times.Never);
    }

    /// <summary>
    /// 在电梯内时, 关门请求应发送 <see cref="RequestDoorCloseMessage"/>
    /// </summary>
    [Fact]
    public async Task RequestDoorClose_WhenInElevator_SendsMessage()
    {
        // Arrange
        _ = _stateMock.Setup(s => s.CurrentElevatorId).Returns(2);

        // Act
        await _vm.RequestDoorClose();

        // Assert
        _senderMock.Verify(s => s.SendAsync(It.Is<RequestDoorCloseMessage>(m => m.ElevatorId == 2)), Times.Once);
    }

    /// <summary>
    /// 返回欢迎页应触发切换到 Welcome 面板
    /// </summary>
    [Fact]
    public void GoToWelcome_FiresPanelSwitch()
    {
        // Arrange
        Enums.PanelType? panel = null;
        _vm.PanelSwitchRequested += p => panel = p;

        // Act
        _vm.GoToWelcome();

        // Assert
        Assert.Equal(Enums.PanelType.Welcome, panel);
    }

    /// <summary>
    /// <see cref="MainViewModel.Dispose"/> 应取消事件订阅并释放连接服务
    /// </summary>
    [Fact]
    public void Dispose_UnsubscribesEvents()
    {
        // Act - should not throw
        _vm.Dispose();

        // Assert
        _connectionMock.Verify(c => c.Dispose(), Times.Once);
    }

    /// <summary>
    /// 构造函数应订阅 <see cref="IConnectionService.OnConnected"/> 和 <see cref="IConnectionService.OnDisconnected"/> 事件
    /// </summary>
    [Fact]
    public void Constructor_SubscribesToConnectionEvents()
    {
        // Verify: test that subscription is set up (by checking the mock setup was performed)
        // The constructor already subscribed, we verify by checking the mock
        _connectionMock.VerifyAdd(c => c.OnConnected += It.IsAny<Action>(), Times.Once);
        _connectionMock.VerifyAdd(c => c.OnDisconnected += It.IsAny<Action>(), Times.Once);
    }
}
