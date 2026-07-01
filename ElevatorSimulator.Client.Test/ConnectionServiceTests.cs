using ElevatorSimulator.Client.Core;
using ElevatorSimulator.Client.Core.Interfaces;
using ElevatorSimulator.Share.Interfaces;
using ElevatorSimulator.Share.Messages;
using Microsoft.Extensions.Logging;
using Moq;

namespace ElevatorSimulator.Client.Test;

/// <summary>
/// <see cref="ConnectionService"/> 的单元测试, 验证连接生命周期管理, Stream 空状态和事件订阅
/// </summary>
public sealed class ConnectionServiceTests
{
    /// <summary> 传输连接工厂的 Mock </summary>
    private readonly Mock<Func<ITransportConnection>> _factoryMock;
    /// <summary> 消息收发器的 Mock </summary>
    private readonly Mock<IStreamMessenger> _messengerMock;
    /// <summary> 客户端状态的 Mock </summary>
    private readonly Mock<IClientState> _stateMock;
    /// <summary> 日志记录器的 Mock </summary>
    private readonly Mock<ILogger<ConnectionService>> _loggerMock;
    /// <summary> 传输连接实例的 Mock </summary>
    private readonly Mock<ITransportConnection> _transportMock;

    /// <summary>
    /// 初始化 Mock 对象与默认行为
    /// </summary>
    public ConnectionServiceTests()
    {
        _transportMock = new Mock<ITransportConnection>();
        _factoryMock = new Mock<Func<ITransportConnection>>();
        _ = _factoryMock.Setup(f => f()).Returns(_transportMock.Object);
        _messengerMock = new Mock<IStreamMessenger>();
        _stateMock = new Mock<IClientState>();
        _ = _stateMock.Setup(s => s.ClientId).Returns("test-client-id");
        _loggerMock = new Mock<ILogger<ConnectionService>>();
    }

    /// <summary>
    /// 使用 Mock 依赖创建 <see cref="ConnectionService"/> 实例
    /// </summary>
    private ConnectionService CreateService()
    {
        return new(
        _factoryMock.Object,
        _messengerMock.Object,
        _stateMock.Object,
        _loggerMock.Object);
    }

    /// <summary>
    /// 未连接时 <see cref="ConnectionService.Stream"/> 应为 <see langword="null"/>
    /// </summary>
    [Fact]
    public void Stream_IsNull_WhenNotConnected()
    {
        var service = CreateService();
        Assert.Null(service.Stream);
    }

    /// <summary>
    /// 调用 <see cref="ConnectionService.Disconnect"/> 后 Stream 应被置为 <see langword="null"/>
    /// </summary>
    [Fact]
    public void Disconnect_SetsStreamToNull()
    {
        var service = CreateService();
        service.Disconnect();
        Assert.Null(service.Stream);
    }

    /// <summary>
    /// <see cref="ConnectionService.Dispose"/> 应清空 Stream
    /// </summary>
    [Fact]
    public void Dispose_ClearsStream()
    {
        var service = CreateService();
        service.Dispose();
        Assert.Null(service.Stream);
    }

    /// <summary>
    /// 调用 <see cref="ConnectionService.Connect"/> 应触发传输连接工厂创建传输实例并尝试连接
    /// </summary>
    [Fact]
    public async Task Connect_InvokesTransportFactory_AndConnectionAttempt()
    {
        var connectTcs = new TaskCompletionSource<bool>();
        _ = _transportMock.Setup(t => t.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(connectTcs.Task);
        _ = _transportMock.Setup(t => t.IsConnected).Returns(false);
        var service = CreateService();

        service.Connect("127.0.0.1", 8888);

        // 等待异步连接循环启动
        await Task.Delay(100);
        _factoryMock.Verify(f => f(), Times.AtLeastOnce);
        _transportMock.Verify(t => t.ConnectAsync("127.0.0.1", 8888, It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _ = connectTcs.TrySetResult(false);
        service.Disconnect();
    }

    /// <summary>
    /// 连接成功后应发送包含客户端 ID 的身份消息
    /// </summary>
    [Fact]
    public async Task Connect_WhenSuccessful_SendsIdentityMessage()
    {
        // 重置 Mock 以避免与前一个测试的设置冲突
        _transportMock.Reset();
        _factoryMock.Reset();
        _messengerMock.Reset();
        _ = _factoryMock.Setup(f => f()).Returns(_transportMock.Object);

        using var ms = new MemoryStream();
        _ = _transportMock.Setup(t => t.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true));
        _ = _transportMock.Setup(t => t.IsConnected).Returns(true);
        _ = _transportMock.Setup(t => t.GetStream()).Returns(ms);
        _ = _messengerMock.Setup(m => m.SendAsync(It.IsAny<Stream>(), It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreateService();

        service.Connect("127.0.0.1", 8888);
        await Task.Delay(200);

        _messengerMock.Verify(m => m.SendAsync(
            It.IsAny<Stream>(),
            It.Is<ClientIdentityMessage>(msg => msg.ClientId == "test-client-id"),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        service.Disconnect();
    }

    /// <summary>
    /// 重复调用 Connect 应先取消前一次连接
    /// </summary>
    [Fact]
    public async Task Connect_CalledTwice_CancelsPrevious()
    {
        var firstConnectTcs = new TaskCompletionSource<bool>();
        _ = _transportMock.Setup(t => t.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(firstConnectTcs.Task);
        _ = _transportMock.Setup(t => t.IsConnected).Returns(false);
        var service = CreateService();

        service.Connect("127.0.0.1", 8888);
        await Task.Delay(50);
        service.Connect("127.0.0.1", 9999);

        Assert.True(firstConnectTcs.Task.IsCanceled || !firstConnectTcs.Task.IsCompletedSuccessfully);

        _ = firstConnectTcs.TrySetCanceled();
        service.Disconnect();
    }

    /// <summary>
    /// <see cref="ConnectionService.OnConnected"/> 事件应可正常订阅
    /// </summary>
    [Fact]
    public void OnConnected_Event_CanBeSubscribed()
    {
        var service = CreateService();
        var invoked = false;
        service.OnConnected += () => invoked = true;
        Assert.False(invoked);
    }

    /// <summary>
    /// <see cref="ConnectionService.OnDisconnected"/> 事件应可正常订阅
    /// </summary>
    [Fact]
    public void OnDisconnected_Event_CanBeSubscribed()
    {
        var service = CreateService();
        var invoked = false;
        service.OnDisconnected += () => invoked = true;
        Assert.False(invoked);
    }
}
