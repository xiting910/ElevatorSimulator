using ElevatorSimulator.Server.Core;
using ElevatorSimulator.Server.Core.Interfaces;
using ElevatorSimulator.Server.Core.Networking;
using ElevatorSimulator.Share.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace ElevatorSimulator.Server.Test;

/// <summary>
/// <see cref="TcpServerManager"/> 的单元测试, 覆盖广播, 强制断开, 黑名单和资源释放
/// </summary>
public sealed class TcpServerManagerTests : IDisposable
{
    /// <summary>
    /// 空释放方法
    /// </summary>
    public void Dispose() { }

    /// <summary>
    /// 使用 Mock 依赖创建 <see cref="TcpServerManager"/> 实例
    /// </summary>
    private static TcpServerManager CreateManager() => new(
        Mock.Of<IElevatorManager>(),
        Mock.Of<ILogger<TcpServerManager>>(),
        Mock.Of<IStreamMessenger>(),
        new MessageRouter(Mock.Of<ILogger<MessageRouter>>(), []));

    /// <summary>
    /// 创建一个已连接的 TcpClient 并封装为 ClientContext, 注入到管理器的客户端字典中
    /// </summary>
    private static void InjectClient(TcpServerManager manager, string clientId)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var tcpClient = new TcpClient();
        tcpClient.Connect(IPAddress.Loopback, port);

        var contextType = typeof(TcpServerManager).GetNestedType("ClientContext",
            BindingFlags.NonPublic)!;
        var ctor = contextType.GetConstructor([typeof(TcpClient)])!;
        var ctx = ctor.Invoke([tcpClient]);

        var clientsField = typeof(TcpServerManager).GetField("_clients",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var clients = (System.Collections.IDictionary)clientsField.GetValue(manager)!;
        clients[clientId] = ctx;
    }

    /// <summary>
    /// 获取管理器的黑名单字典
    /// </summary>
    private static ConcurrentDictionary<string, DateTime> GetBlacklist(TcpServerManager manager)
    {
        var field = typeof(TcpServerManager).GetField("_blacklist",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (ConcurrentDictionary<string, DateTime>)field.GetValue(manager)!;
    }

    /// <summary>
    /// 获取管理器的客户端数量
    /// </summary>
    private static int GetClientCount(TcpServerManager manager)
    {
        var field = typeof(TcpServerManager).GetField("_clients",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var clients = (System.Collections.IDictionary)field.GetValue(manager)!;
        return clients.Count;
    }

    /// <summary>
    /// 检查客户端字典是否包含指定 ID
    /// </summary>
    private static bool ClientExists(TcpServerManager manager, string clientId)
    {
        var field = typeof(TcpServerManager).GetField("_clients",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var clients = (System.Collections.IDictionary)field.GetValue(manager)!;
        return clients.Contains(clientId);
    }

    /// <summary>
    /// 构造函数不应抛出异常且初始客户端数为零
    /// </summary>
    [Fact]
    public void Constructor_HasZeroClients()
    {
        var manager = CreateManager();
        Assert.Equal(0, GetClientCount(manager));
    }

    /// <summary>
    /// <see cref="TcpServerManager.Dispose"/> 应可安全重复调用且客户端数为零
    /// </summary>
    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_AndClearsClients()
    {
        var manager = CreateManager();
        InjectClient(manager, "client-1");
        manager.Dispose();
        manager.Dispose();
        Assert.Equal(0, GetClientCount(manager));
    }

    /// <summary>
    /// <see cref="TcpServerManager.Dispose"/> 应清空客户端字典
    /// </summary>
    [Fact]
    public void Dispose_ClearsClients()
    {
        var manager = CreateManager();
        InjectClient(manager, "client-1");
        InjectClient(manager, "client-2");

        manager.Dispose();

        Assert.Equal(0, GetClientCount(manager));
    }

    /// <summary>
    /// <see cref="TcpServerManager.DisconnectClient"/> 应将客户端 ID 加入黑名单
    /// </summary>
    [Fact]
    public void DisconnectClient_AddsToBlacklist()
    {
        var manager = CreateManager();
        InjectClient(manager, "bad-client");

        manager.DisconnectClient("bad-client");

        var blacklist = GetBlacklist(manager);
        Assert.True(blacklist.ContainsKey("bad-client"));
    }

    /// <summary>
    /// 黑名单过期时间应在冷却时长范围内
    /// </summary>
    [Fact]
    public void DisconnectClient_SetsBlacklistExpiry_WithinCooldown()
    {
        var manager = CreateManager();
        InjectClient(manager, "bad-client");

        manager.DisconnectClient("bad-client");

        var blacklist = GetBlacklist(manager);
        Assert.True(blacklist.TryGetValue("bad-client", out var expiry));
        Assert.True(expiry > DateTime.UtcNow, "Blacklist expiry should be in the future");
        Assert.True(expiry <= DateTime.UtcNow.AddSeconds(TcpServerManager.BlacklistDurationSeconds + 1),
            "Blacklist expiry should be within the cooldown duration");
    }

    /// <summary>
    /// 强制断开不存在的客户端时不应触发事件且客户端字典保持为空
    /// </summary>
    [Fact]
    public void DisconnectClient_NonExisting_NoEventFired()
    {
        var manager = CreateManager();
        var fired = false;
        manager.ClientListChanged += _ => fired = true;

        manager.DisconnectClient("non-existing");

        Assert.False(fired);
        Assert.Equal(0, GetClientCount(manager));
    }

    /// <summary>
    /// 强制断开后客户端应从字典中移除
    /// </summary>
    [Fact]
    public void DisconnectClient_RemovesClientFromDictionary()
    {
        var manager = CreateManager();
        InjectClient(manager, "remove-me");

        manager.DisconnectClient("remove-me");

        Assert.False(ClientExists(manager, "remove-me"));
    }

    /// <summary>
    /// 强制断开应触发 ClientListChanged 事件
    /// </summary>
    [Fact]
    public void DisconnectClient_FiresClientListChanged()
    {
        var manager = CreateManager();
        InjectClient(manager, "fire-event");
        var fired = false;
        manager.ClientListChanged += _ => fired = true;

        manager.DisconnectClient("fire-event");

        Assert.True(fired);
    }
}
