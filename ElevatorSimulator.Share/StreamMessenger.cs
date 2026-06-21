using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace ElevatorSimulator.Share;

/// <summary>
/// 基于 <see cref="Stream"/> 的消息传输工具类
/// </summary>
public sealed class StreamMessenger : Interfaces.IStreamMessenger
{
    /// <summary>
    /// 消息长度前缀的字节大小
    /// </summary>
    private const int LengthPrefixSize = sizeof(int);

    /// <summary>
    /// 消息体的最大字节大小, 防止恶意或错误的消息导致内存问题, 单位为字节
    /// </summary>
    private const int MaxMessageSize = 1 * 1024 * 1024;

    /// <summary>
    /// JSON 序列化选项, 通过反射自动收集 <see cref="Messages.Message"/> 的所有子类型以支持多态序列化
    /// </summary>
    private static readonly JsonSerializerOptions _options = CreateOptions();

    /// <summary>
    /// 构建 JSON 序列化选项, 自动扫描程序集中所有 <see cref="Messages.Message"/> 的非抽象子类,
    /// 并利用 <see cref="Enums.MessageType"/> 枚举的值作为多态类型鉴别器
    /// </summary>
    /// <returns>配置好的 <see cref="JsonSerializerOptions"/> 实例</returns>
    private static JsonSerializerOptions CreateOptions()
    {
        // 反射扫描所有非抽象的消息子类
        var derivedTypes = typeof(Messages.Message).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true } && t.IsSubclassOf(typeof(Messages.Message)));

        // 使用 DefaultJsonTypeInfoResolver 动态注册子类型, 替代 [JsonDerivedType] 特性硬编码
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            if (typeInfo.Type != typeof(Messages.Message)) { return; }

            typeInfo.PolymorphismOptions = new()
            {
                // 未知子类型时抛出异常, 避免静默数据丢失
                UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
            };

            foreach (var t in derivedTypes)
            {
                // 从类名推断对应的 MessageType 枚举值 (例如 ExternalCallMessage → ExternalCall → 2)
                var discriminator = GetMessageTypeDiscriminator(t);
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new(t, discriminator));
            }
        });

        return new() { TypeInfoResolver = resolver };
    }

    /// <summary>
    /// 根据消息子类的类名获取对应的 <see cref="Enums.MessageType"/> 整数值
    /// </summary>
    /// <param name="messageType">消息子类的 <see cref="Type"/></param>
    /// <returns>对应的 <see cref="Enums.MessageType"/> 整数值</returns>
    /// <exception cref="InvalidOperationException">当类名无法映射到任何已知枚举值时抛出</exception>
    private static int GetMessageTypeDiscriminator(Type messageType)
    {
        // 去掉类名中的 "Message" 后缀, 得到枚举成员名
        var name = messageType.Name.Replace("Message", string.Empty);
        return Enum.TryParse<Enums.MessageType>(name, true, out var result) ? (int)result : throw new InvalidOperationException($"消息类型 '{messageType.Name}' 无法映射到 {nameof(Enums.MessageType)} 枚举值。请确保枚举中存在对应的成员。");
    }

    /// <inheritdoc/>
    public async Task SendAsync(Stream stream, Messages.Message msg, CancellationToken token)
    {
        // 使用 ArrayBufferWriter 避免每次序列化分配新的 byte[], 减少 GC 压力
        var buffer = new ArrayBufferWriter<byte>();

        // 使用 Utf8JsonWriter 直接写入 ArrayBufferWriter, 避免中间字符串分配, 提高性能
        using var writer = new Utf8JsonWriter(buffer);

        // 将消息对象序列化为 JSON 格式的 UTF-8 字节流, 写入 ArrayBufferWriter
        JsonSerializer.Serialize(writer, msg, _options);

        // 完成写入后获取消息体的字节长度, 并准备长度前缀的缓冲区
        var lenPrefix = new byte[LengthPrefixSize];

        // 在长度前缀中写入消息体的字节长度, 显式指定小端序避免跨平台字节序问题
        BinaryPrimitives.WriteInt32LittleEndian(lenPrefix, buffer.WrittenCount);

        // 写入长度前缀
        await stream.WriteAsync(lenPrefix.AsMemory(), token).ConfigureAwait(false);

        // 写入消息体
        await stream.WriteAsync(buffer.WrittenMemory, token).ConfigureAwait(false);

        // 确保数据被发送出去
        await stream.FlushAsync(token).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Messages.Message?> ReceiveAsync(Stream stream, CancellationToken token)
    {
        // 准备接收长度前缀的缓冲区
        var lenBuf = new byte[LengthPrefixSize];

        // 记录已经读取的字节数, 可能需要多次读取才能收齐完整的长度前缀
        int read;

        // 总共已经读取的长度前缀字节数, 用于判断是否已经收齐完整的长度前缀
        var totalLenRead = 0;

        // 循环读取直到收齐完整的长度前缀
        while (totalLenRead < LengthPrefixSize)
        {
            read = await stream.ReadAsync(lenBuf.AsMemory(totalLenRead, LengthPrefixSize - totalLenRead), token).ConfigureAwait(false);
            if (read == 0) { return null; }
            totalLenRead += read;
        }

        // 从长度前缀解析出消息体的字节长度, 显式指定小端序避免跨平台字节序问题
        var msgLen = BinaryPrimitives.ReadInt32LittleEndian(lenBuf);

        // 如果消息体长度不合法, 可能是恶意或错误的数据, 返回 null
        if (msgLen is <= 0 or > MaxMessageSize) { return null; }

        // 从共享数组池租用缓冲区, 减少 GC 压力
        var msgBuf = ArrayPool<byte>.Shared.Rent(msgLen);
        try
        {
            // 记录已经读取的字节数, 可能需要多次读取才能收齐完整的消息体
            var totalRead = 0;

            // 循环读取直到收齐完整的消息体
            while (totalRead < msgLen)
            {
                read = await stream.ReadAsync(msgBuf.AsMemory(totalRead, msgLen - totalRead), token).ConfigureAwait(false);
                if (read == 0) { break; }
                totalRead += read;
            }

            // 成功收齐完整消息体后, 反序列化为消息对象
            return totalRead == msgLen ? JsonSerializer.Deserialize<Messages.Message>(msgBuf.AsSpan(0, msgLen), _options) : null;
        }
        finally
        {
            // 归还缓冲区到共享池
            ArrayPool<byte>.Shared.Return(msgBuf);
        }
    }
}
