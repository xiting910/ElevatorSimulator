using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Threading;

namespace ElevatorSimulator.Share;

/// <summary>
/// 命名管道消息传输工具类
/// </summary>
public static class NamedPipeMessenger
{
    /// <summary>
    /// 消息长度前缀的字节大小
    /// </summary>
    private const int LengthPrefixSize = sizeof(int);

    /// <summary>
    /// 消息体的最大字节大小, 防止恶意或错误的消息导致内存问题, 单位为字节
    /// </summary>
    private const int MaxMessageSize = 1024 * 1024;

    /// <summary>
    /// JSON 序列化选项
    /// </summary>
    private static readonly JsonSerializerOptions _options = new()
    {
        // 不使用缩进以减少消息体积
        WriteIndented = false
    };

    /// <summary>
    /// 异步写入命名管道
    /// </summary>
    /// <param name="pipe">命名管道</param>
    /// <param name="msg">要发送的消息对象</param>
    /// <param name="token">取消令牌</param>
    public static async Task SendAsync(PipeStream pipe, Message msg, CancellationToken token = default)
    {
        // 先将消息对象序列化为字节数组
        var data = JsonSerializer.SerializeToUtf8Bytes(msg, _options);

        // 获取消息体长度的字节表示
        var lenPrefix = BitConverter.GetBytes(data.Length);

        // 写入长度前缀
        await pipe.WriteAsync(lenPrefix.AsMemory(0, LengthPrefixSize), token);

        // 写入消息体
        await pipe.WriteAsync(data, token);

        // 确保数据被发送出去
        await pipe.FlushAsync(token);
    }

    /// <summary>
    /// 异步从命名管道读取一条消息
    /// </summary>
    /// <param name="pipe">命名管道</param>
    /// <param name="token">取消令牌</param>
    /// <returns>读取到的消息对象, 读取失败或管道关闭时返回 <c>null</c></returns>
    public static async Task<Message?> ReceiveAsync(PipeStream pipe, CancellationToken token = default)
    {
        // 准备接收长度前缀的缓冲区
        var lenBuf = new byte[LengthPrefixSize];

        // 先读取长度前缀
        var read = await pipe.ReadAsync(lenBuf.AsMemory(0, LengthPrefixSize), token);

        // 如果读取的字节数不等于长度前缀的大小, 说明管道被关闭或数据不完整, 返回 null
        if (read != LengthPrefixSize) { return null; }

        // 从长度前缀解析出消息体的字节长度
        var msgLen = BitConverter.ToInt32(lenBuf, 0);

        // 如果消息体长度不合法, 可能是恶意或错误的数据, 返回 null
        if (msgLen is <= 0 or > MaxMessageSize) { return null; }

        // 分配存放消息体的缓冲区
        var msgBuf = new byte[msgLen];

        // 记录已经读取的字节数, 可能需要多次读取才能收齐完整的消息体
        var totalRead = 0;

        // 循环读取直到收齐完整的消息体
        while (totalRead < msgLen)
        {
            read = await pipe.ReadAsync(msgBuf.AsMemory(totalRead, msgLen - totalRead), token);
            if (read == 0) { break; }
            totalRead += read;
        }

        // 成功收齐完整消息体后，反序列化为消息对象
        return totalRead == msgLen ? JsonSerializer.Deserialize<Message>(msgBuf, _options) : null;
    }
}
