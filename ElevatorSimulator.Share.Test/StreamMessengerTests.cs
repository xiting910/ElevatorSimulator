using ElevatorSimulator.Share.Enums;
using ElevatorSimulator.Share.Interfaces;
using ElevatorSimulator.Share.Messages;

namespace ElevatorSimulator.Share.Test;

/// <summary>
/// <see cref="StreamMessenger"/> 的单元测试, 验证基于流的消息收发,多态序列化往返正确性
/// </summary>
public sealed class StreamMessengerTests
{
    /// <summary>
    /// 被测的消息传输器实例
    /// </summary>
    private readonly IStreamMessenger _messenger = new StreamMessenger();

    /// <summary>
    /// 发送并接收一条 <see cref="HeartbeatMessage"/>, 验证空消息体的往返序列化
    /// </summary>
    [Fact]
    public async Task SendAsync_ReceiveAsync_RoundTrip_HeartbeatMessage()
    {
        // Arrange
        using var stream = new MemoryStream();
        var msg = new HeartbeatMessage();
        using var cts = new CancellationTokenSource(5000);

        // Act
        await _messenger.SendAsync(stream, msg, cts.Token);
        stream.Position = 0;
        var result = await _messenger.ReceiveAsync(stream, cts.Token);

        // Assert
        Assert.NotNull(result);
        _ = Assert.IsType<HeartbeatMessage>(result);
    }

    /// <summary>
    /// 发送并接收一条 <see cref="ExternalCallMessage"/>, 验证 Floor 和 Direction 属性正确还原
    /// </summary>
    [Fact]
    public async Task SendAsync_ReceiveAsync_RoundTrip_ExternalCallMessage()
    {
        // Arrange
        using var stream = new MemoryStream();
        var msg = new ExternalCallMessage { Floor = 5, Direction = Direction.Up };
        using var cts = new CancellationTokenSource(5000);

        // Act
        await _messenger.SendAsync(stream, msg, cts.Token);
        stream.Position = 0;
        var result = await _messenger.ReceiveAsync(stream, cts.Token);

        // Assert
        Assert.NotNull(result);
        var external = Assert.IsType<ExternalCallMessage>(result);
        Assert.Equal(5, external.Floor);
        Assert.Equal(Direction.Up, external.Direction);
    }

    /// <summary>
    /// 发送并接收一条 <see cref="InternalCallMessage"/>, 验证 ElevatorId 和 TargetFloor 属性正确还原
    /// </summary>
    [Fact]
    public async Task SendAsync_ReceiveAsync_RoundTrip_InternalCallMessage()
    {
        // Arrange
        using var stream = new MemoryStream();
        var msg = new InternalCallMessage { ElevatorId = 1, TargetFloor = 10 };
        using var cts = new CancellationTokenSource(5000);

        // Act
        await _messenger.SendAsync(stream, msg, cts.Token);
        stream.Position = 0;
        var result = await _messenger.ReceiveAsync(stream, cts.Token);

        // Assert
        Assert.NotNull(result);
        var internalMsg = Assert.IsType<InternalCallMessage>(result);
        Assert.Equal(1, internalMsg.ElevatorId);
        Assert.Equal(10, internalMsg.TargetFloor);
    }

    /// <summary>
    /// 发送并接收一条 <see cref="ElevatorStatusMessage"/>, 验证复杂属性和数组的正确序列化
    /// </summary>
    [Fact]
    public async Task SendAsync_ReceiveAsync_RoundTrip_ElevatorStatusMessage()
    {
        // Arrange
        using var stream = new MemoryStream();
        var msg = new ElevatorStatusMessage
        {
            Id = 2,
            CurrentFloor = 15,
            MovingDirection = Direction.Down,
            Door = DoorState.Open,
            DoorOpenRatio = 1.0,
            InternalCalls = [2, 8, 20]
        };
        using var cts = new CancellationTokenSource(5000);

        // Act
        await _messenger.SendAsync(stream, msg, cts.Token);
        stream.Position = 0;
        var result = await _messenger.ReceiveAsync(stream, cts.Token);

        // Assert
        Assert.NotNull(result);
        var status = Assert.IsType<ElevatorStatusMessage>(result);
        Assert.Equal(2, status.Id);
        Assert.Equal(15, status.CurrentFloor);
        Assert.Equal(Direction.Down, status.MovingDirection);
        Assert.Equal(DoorState.Open, status.Door);
        Assert.Equal(1.0, status.DoorOpenRatio);
        Assert.Equal([2, 8, 20], status.InternalCalls);
    }

    /// <summary>
    /// 发送并接收一条 <see cref="FloorStatusMessage"/>, 验证字典类型属性的序列化往返
    /// </summary>
    [Fact]
    public async Task SendAsync_ReceiveAsync_RoundTrip_FloorStatusMessage()
    {
        // Arrange
        using var stream = new MemoryStream();
        var msg = new FloorStatusMessage
        {
            ActiveCalls = new()
            {
                [1] = [Direction.Up],
                [5] = [Direction.Up, Direction.Down],
                [10] = [Direction.Down]
            }
        };
        using var cts = new CancellationTokenSource(5000);

        // Act
        await _messenger.SendAsync(stream, msg, cts.Token);
        stream.Position = 0;
        var result = await _messenger.ReceiveAsync(stream, cts.Token);

        // Assert
        Assert.NotNull(result);
        var floorStatus = Assert.IsType<FloorStatusMessage>(result);
        Assert.NotEmpty(floorStatus.ActiveCalls);
        Assert.Contains(Direction.Up, floorStatus.ActiveCalls[1]);
        Assert.Contains(Direction.Up, floorStatus.ActiveCalls[5]);
        Assert.Contains(Direction.Down, floorStatus.ActiveCalls[5]);
    }

    /// <summary>
    /// 当流为空时, <see cref="StreamMessenger.ReceiveAsync"/> 应返回 <see langword="null"/>
    /// </summary>
    [Fact]
    public async Task ReceiveAsync_EmptyStream_ReturnsNull()
    {
        // Arrange
        using var stream = new MemoryStream(); // position=0, length=0
        using var cts = new CancellationTokenSource(1000);

        // Act
        var result = await _messenger.ReceiveAsync(stream, cts.Token);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 连续发送多条消息后依次接收, 验证消息顺序保持不变
    /// </summary>
    [Fact]
    public async Task SendAsync_ReceiveAsync_MultipleMessages_PreservesOrder()
    {
        // Arrange
        using var stream = new MemoryStream();
        var msg1 = new ExternalCallMessage { Floor = 1, Direction = Direction.Up };
        var msg2 = new ExternalCallMessage { Floor = 3, Direction = Direction.Down };
        using var cts = new CancellationTokenSource(5000);

        // Act
        await _messenger.SendAsync(stream, msg1, cts.Token);
        await _messenger.SendAsync(stream, msg2, cts.Token);
        stream.Position = 0;
        var result1 = await _messenger.ReceiveAsync(stream, cts.Token);
        var result2 = await _messenger.ReceiveAsync(stream, cts.Token);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(1, Assert.IsType<ExternalCallMessage>(result1).Floor);

        Assert.NotNull(result2);
        Assert.Equal(3, Assert.IsType<ExternalCallMessage>(result2).Floor);
    }

    /// <summary>
    /// 发送并接收一条取消消息, 验证取消类消息的多态序列化
    /// </summary>
    [Fact]
    public async Task SendAsync_ReceiveAsync_CancelMessages_RoundTrip()
    {
        // Arrange
        using var stream = new MemoryStream();
        var msg = new CancelExternalCallMessage { Floor = 7, Direction = Direction.Up };
        using var cts = new CancellationTokenSource(5000);

        // Act
        await _messenger.SendAsync(stream, msg, cts.Token);
        stream.Position = 0;
        var result = await _messenger.ReceiveAsync(stream, cts.Token);

        // Assert
        _ = Assert.IsType<CancelExternalCallMessage>(result);
    }

    /// <summary>
    /// 发送并接收一条 <see cref="ClientIdentityMessage"/>, 验证客户端身份消息的往返序列化
    /// </summary>
    [Fact]
    public async Task SendAsync_ReceiveAsync_ClientIdentityMessage_RoundTrip()
    {
        // Arrange
        using var stream = new MemoryStream();
        var msg = new ClientIdentityMessage { ClientId = "test-client-123" };
        using var cts = new CancellationTokenSource(5000);

        // Act
        await _messenger.SendAsync(stream, msg, cts.Token);
        stream.Position = 0;
        var result = await _messenger.ReceiveAsync(stream, cts.Token);

        // Assert
        Assert.NotNull(result);
        var identity = Assert.IsType<ClientIdentityMessage>(result);
        Assert.Equal("test-client-123", identity.ClientId);
    }
}
