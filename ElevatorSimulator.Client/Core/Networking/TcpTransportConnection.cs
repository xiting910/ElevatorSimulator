using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ElevatorSimulator.Client.Core.Networking;

/// <summary>
/// <see cref="Interfaces.ITransportConnection"/> 的 TCP 实现, 包装 <see cref="TcpClient"/>
/// </summary>
public sealed class TcpTransportConnection : Interfaces.ITransportConnection
{
    /// <summary>
    /// TCP 客户端
    /// </summary>
    private TcpClient? _tcpClient;

    /// <inheritdoc/>
    public bool IsConnected => _tcpClient?.Connected ?? false;

    /// <inheritdoc/>
    public Stream? GetStream()
    {
        return _tcpClient?.GetStream();
    }

    /// <inheritdoc/>
    public async Task ConnectAsync(string serverAddress, int serverPort, CancellationToken token)
    {
        _tcpClient = new();
        await _tcpClient.ConnectAsync(serverAddress, serverPort, token).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Close()
    {
        try { _tcpClient?.Close(); } catch (Exception) { }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        try { _tcpClient?.Close(); } catch (Exception) { }
        try { _tcpClient?.Dispose(); } catch (Exception) { }
        _tcpClient = null;
    }
}
