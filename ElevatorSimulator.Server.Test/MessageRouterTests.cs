using ElevatorSimulator.Server.Core;
using ElevatorSimulator.Server.Core.Interfaces;
using ElevatorSimulator.Share.Enums;
using ElevatorSimulator.Share.Messages;
using Microsoft.Extensions.Logging;
using Moq;

namespace ElevatorSimulator.Server.Test;

/// <summary>
/// <see cref="MessageRouter"/> 的单元测试, 验证消息类型到处理器的路由分发
/// </summary>
public sealed class MessageRouterTests
{
    /// <summary> 日志记录器的 Mock </summary>
    private readonly Mock<ILogger<MessageRouter>> _loggerMock;
    /// <summary> 消息处理器的 Mock (基接口) </summary>
    private readonly Mock<IMessageHandler> _handlerMock;
    /// <summary> 电梯管理器的 Mock </summary>
    private readonly Mock<IElevatorManager> _managerMock;

    /// <summary>
    /// 初始化 Mock 对象
    /// </summary>
    public MessageRouterTests()
    {
        _loggerMock = new Mock<ILogger<MessageRouter>>();
        _handlerMock = new Mock<IMessageHandler>();
        _managerMock = new Mock<IElevatorManager>();
    }

    /// <summary>
    /// 已注册的消息类型应被路由到对应处理器
    /// </summary>
    [Fact]
    public void Route_RegisteredHandler_IsCalled()
    {
        // Arrange - create a handler that implements IMessageHandler<ExternalCallMessage>
        var handler = new Mock<IMessageHandler<ExternalCallMessage>>();
        _ = handler.As<IMessageHandler>(); // expose the non-generic interface
        var router = new MessageRouter(_loggerMock.Object, [handler.Object]);
        var msg = new ExternalCallMessage { Floor = 1, Direction = Direction.Up };

        // Act
        router.Route(msg, _managerMock.Object);

        // Assert - the non-generic Handle is called
        handler.As<IMessageHandler>().Verify(h => h.Handle(msg, _managerMock.Object), Times.Once);
    }

    /// <summary>
    /// 未注册的消息类型路由后不应调用管理器任何方法
    /// </summary>
    [Fact]
    public void Route_UnregisteredMessage_DoesNotCallManager()
    {
        var router = new MessageRouter(_loggerMock.Object, []);
        var msg = new ExternalCallMessage { Floor = 1, Direction = Direction.Up };

        router.Route(msg, _managerMock.Object);

        // 管理器的所有方法都不应被调用
        _managerMock.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 多个处理器共存时, 各消息应路由到对应的处理器
    /// </summary>
    [Fact]
    public void Route_MultipleHandlers_EachRoutesCorrectly()
    {
        // Arrange
        var externalHandler = new Mock<IMessageHandler<ExternalCallMessage>>();
        var internalHandler = new Mock<IMessageHandler<InternalCallMessage>>();
        var router = new MessageRouter(_loggerMock.Object, [externalHandler.Object, internalHandler.Object]);

        var externalMsg = new ExternalCallMessage { Floor = 1, Direction = Direction.Up };
        var internalMsg = new InternalCallMessage { ElevatorId = 0, TargetFloor = 5 };

        // Act
        router.Route(externalMsg, _managerMock.Object);
        router.Route(internalMsg, _managerMock.Object);

        // Assert
        externalHandler.As<IMessageHandler>().Verify(h => h.Handle(externalMsg, _managerMock.Object), Times.Once);
        internalHandler.As<IMessageHandler>().Verify(h => h.Handle(internalMsg, _managerMock.Object), Times.Once);
    }
}
