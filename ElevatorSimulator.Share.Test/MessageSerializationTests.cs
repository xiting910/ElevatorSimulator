using ElevatorSimulator.Share.Enums;
using ElevatorSimulator.Share.Messages;

namespace ElevatorSimulator.Share.Test;

/// <summary>
/// 消息序列化的集成测试, 验证所有消息子类型的多态 JSON 往返, 以及时间戳保留和损坏数据处理
/// </summary>
public sealed class MessageSerializationTests
{
    /// <summary>
    /// 被测的消息传输器, 使用反射自动发现所有消息子类型
    /// </summary>
    private readonly StreamMessenger _messenger = new();

    /// <summary>
    /// 遍历所有消息子类型进行序列化往返, 确保多态鉴别器正确工作
    /// </summary>
    [Fact]
    public async Task PolyMorphic_RoundTrip_AllMessageTypes()
    {
        await AssertRoundTrip(new HeartbeatMessage());
        await AssertRoundTrip(new ExternalCallMessage { Floor = 1, Direction = Direction.Up });
        await AssertRoundTrip(new InternalCallMessage { ElevatorId = 0, TargetFloor = 5 });
        await AssertRoundTrip(new CancelExternalCallMessage { Floor = 3, Direction = Direction.Down });
        await AssertRoundTrip(new CancelInternalCallMessage { ElevatorId = 1, TargetFloor = 10 });
        await AssertRoundTrip(new RequestDoorOpenMessage { ElevatorId = 2 });
        await AssertRoundTrip(new RequestDoorCloseMessage { ElevatorId = 0 });
        await AssertRoundTrip(new ClientIdentityMessage { ClientId = "unit-test" });
        await AssertRoundTrip(new ElevatorStatusMessage
        {
            Id = 1,
            CurrentFloor = 0,
            MovingDirection = Direction.None,
            Door = DoorState.Closed,
            DoorOpenRatio = 0,
            InternalCalls = []
        });
        await AssertRoundTrip(new FloorStatusMessage
        {
            ActiveCalls = new Dictionary<int, Direction[]> { [1] = [Direction.Up] }
        });
    }

    /// <summary>
    /// 序列化往返后, <see cref="Message.Timestamp"/> 应保持原始值不变
    /// </summary>
    [Fact]
    public async Task Message_Timestamp_IsPreserved()
    {
        // Arrange
        using var stream = new MemoryStream();
        var original = new ExternalCallMessage { Floor = 1, Direction = Direction.Up };
        using var cts = new CancellationTokenSource(5000);

        // Act
        await _messenger.SendAsync(stream, original, cts.Token);
        stream.Position = 0;
        var result = await _messenger.ReceiveAsync(stream, cts.Token);

        // Assert
        Assert.NotNull(result);
        var deserialized = Assert.IsType<ExternalCallMessage>(result);
        Assert.Equal(original.Timestamp, deserialized.Timestamp);
    }

    /// <summary>
    /// 当长度前缀为负值时, <see cref="StreamMessenger.ReceiveAsync"/> 应返回 <see langword="null"/>
    /// </summary>
    [Fact]
    public async Task ReceiveAsync_CorruptedLengthPrefix_ReturnsNull()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Write a negative length prefix
        stream.Write(System.BitConverter.GetBytes(-1));
        stream.Position = 0;
        using var cts = new CancellationTokenSource(1000);

        // Act
        var result = await _messenger.ReceiveAsync(stream, cts.Token);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 辅助方法: 序列化消息到流再反序列化, 断言类型一致
    /// </summary>
    /// <param name="msg">要测试的消息实例</param>
    private async Task AssertRoundTrip(Message msg)
    {
        using var stream = new MemoryStream();
        using var cts = new CancellationTokenSource(5000);

        await _messenger.SendAsync(stream, msg, cts.Token);
        stream.Position = 0;
        var result = await _messenger.ReceiveAsync(stream, cts.Token);

        Assert.NotNull(result);
        Assert.IsType(msg.GetType(), result);
    }
}
