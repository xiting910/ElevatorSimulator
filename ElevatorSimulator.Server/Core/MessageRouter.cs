using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ElevatorSimulator.Server.Core;

/// <summary>
/// 消息路由器, 负责将接收到的消息分发给对应的处理器
/// </summary>
public sealed partial class MessageRouter
{
    /// <summary>
    /// 消息类型到处理器的映射字典
    /// </summary>
    private readonly Dictionary<Type, Interfaces.IMessageHandler> _handlers;

    /// <summary>
    /// 未识别消息时的日志记录器
    /// </summary>
    private readonly ILogger<MessageRouter> _logger;

    /// <summary>
    /// 构造函数, 通过 DI 收集所有已注册的消息处理器并自动构建类型映射
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="handlers">所有已注册的 <see cref="Interfaces.IMessageHandler"/> 实现</param>
    public MessageRouter(ILogger<MessageRouter> logger, IEnumerable<Interfaces.IMessageHandler> handlers)
    {
        _logger = logger;
        _handlers = [];

        // 遍历所有已注册的 Handler, 通过泛型接口推断其处理的消息类型
        foreach (var handler in handlers)
        {
            // 获取 Handler 的类型
            var handlerType = handler.GetType();

            // 查找 IMessageHandler<TMessage> 泛型接口以获取 TMessage
            var genericInterface = handlerType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Interfaces.IMessageHandler<>));

            // 如果找到了对应的泛型接口, 则将 TMessage 类型与 Handler 实例添加到字典中
            if (genericInterface is not null)
            {
                var messageType = genericInterface.GetGenericArguments()[0];
                _handlers[messageType] = handler;
            }
        }
    }

    /// <summary>
    /// 路由消息到对应的处理器
    /// </summary>
    /// <param name="msg">接收到的消息</param>
    /// <param name="elevatorManager">电梯管理器</param>
    public void Route(Messages.Message msg, Interfaces.IElevatorManager elevatorManager)
    {
        if (_handlers.TryGetValue(msg.GetType(), out var handler))
        {
            handler.Handle(msg, elevatorManager);
        }
        else
        {
            LogUnknownMessage(_logger, msg.GetType().Name);
        }
    }
}
