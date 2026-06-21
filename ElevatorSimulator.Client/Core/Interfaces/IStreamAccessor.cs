using System.IO;

namespace ElevatorSimulator.Client.Core.Interfaces;

/// <summary>
/// 网络流访问器接口
/// </summary>
public interface IStreamAccessor
{
    /// <summary>
    /// 当前网络流, 连接期间可用, 断开时为 <see langword="null"/>
    /// </summary>
    Stream? Stream { get; }
}
