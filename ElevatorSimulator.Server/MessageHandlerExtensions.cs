using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace ElevatorSimulator.Server;

/// <summary>
/// 消息处理器扩展方法, 用于自动扫描注册所有消息处理器
/// </summary>
public static class MessageHandlerExtensions
{
    /// <summary>
    /// 扫描当前程序集中所有实现 <see cref="Core.Interfaces.IMessageHandler"/> 的非抽象类,
    /// 并将它们注册为 Singleton 服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMessageHandlers(this IServiceCollection services)
    {
        // 获取 IMessageHandler 的 Type, 用于筛选所有实现类
        var handlerInterface = typeof(Core.Interfaces.IMessageHandler);

        // 扫描当前程序集中所有实现了 IMessageHandler 的非抽象类
        var handlerTypes = handlerInterface.Assembly.GetTypes().Where(t => t is { IsAbstract: false, IsClass: true } && handlerInterface.IsAssignableFrom(t));

        // 将每个 Handler 类注册为 Singleton
        foreach (var handlerType in handlerTypes)
        {
            _ = services.AddSingleton(handlerInterface, handlerType);
        }

        // 返回服务集合以支持链式调用
        return services;
    }
}
