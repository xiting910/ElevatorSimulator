using ElevatorSimulator.Client.Core;
using ElevatorSimulator.Client.Core.Interfaces;
using ElevatorSimulator.Share.Enums;
using ElevatorSimulator.Share.Interfaces;
using ElevatorSimulator.Share.Messages;
using Microsoft.Extensions.Logging;
using Moq;

namespace ElevatorSimulator.Client.Test;

/// <summary>
/// <see cref="MessageService"/> 的单元测试, 验证 Stream 为 null 时的静默处理,正常发送和异常捕获
/// </summary>
public sealed class MessageServiceTests
{
    /// <summary> 消息收发器的 Mock </summary>
    private readonly Mock<IStreamMessenger> _messengerMock;
    /// <summary> 流访问器的 Mock </summary>
    private readonly Mock<IStreamAccessor> _streamAccessorMock;
    /// <summary> 日志记录器的 Mock </summary>
    private readonly Mock<ILogger<MessageService>> _loggerMock;

    /// <summary>
    /// 初始化 Mock 对象
    /// </summary>
    public MessageServiceTests()
    {
        _messengerMock = new Mock<IStreamMessenger>();
        _streamAccessorMock = new Mock<IStreamAccessor>();
        _loggerMock = new Mock<ILogger<MessageService>>();
    }

    /// <summary>
    /// 使用 Mock 依赖创建 <see cref="MessageService"/> 实例
    /// </summary>
    private MessageService CreateService()
    {
        return new(
        _messengerMock.Object,
        _streamAccessorMock.Object,
        _loggerMock.Object);
    }

    /// <summary>
    /// 当 Stream 为 <see langword="null"/> 时, 发送操作应静默返回不抛异常
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenStreamIsNull_DoesNotThrow()
    {
        // Arrange
        _ = _streamAccessorMock.Setup(s => s.Stream).Returns((Stream?)null);
        var service = CreateService();
        var msg = new ExternalCallMessage { Floor = 1, Direction = Direction.Up };

        // Act - should not throw
        await service.SendAsync(msg);

        // Assert
        _messengerMock.Verify(m => m.SendAsync(It.IsAny<Stream>(), It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 当 Stream 可用时, 应正确调用 <see cref="IStreamMessenger.SendAsync"/> 发送消息
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenStreamIsNotNull_SendsMessage()
    {
        // Arrange
        using var stream = new MemoryStream();
        _ = _streamAccessorMock.Setup(s => s.Stream).Returns(stream);
        _ = _messengerMock.Setup(m => m.SendAsync(stream, It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreateService();
        var msg = new ExternalCallMessage { Floor = 3, Direction = Direction.Down };

        // Act
        await service.SendAsync(msg);

        // Assert
        _messengerMock.Verify(m => m.SendAsync(stream, msg, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 当底层发送抛出异常时, <see cref="MessageService.SendAsync"/> 应捕获异常而不向上传播
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenSendThrows_DoesNotPropagateException()
    {
        // Arrange
        using var stream = new MemoryStream();
        _ = _streamAccessorMock.Setup(s => s.Stream).Returns(stream);
        _ = _messengerMock.Setup(m => m.SendAsync(stream, It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("network error"));
        var service = CreateService();

        // Act - should not throw
        await service.SendAsync(new ExternalCallMessage { Floor = 1, Direction = Direction.Up });

        // Assert - exception is caught and logged internally
    }
}
