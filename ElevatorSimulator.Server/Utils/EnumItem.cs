using System;
using System.Collections.Concurrent;

namespace ElevatorSimulator.Server.Utils;

/// <summary>
/// 非泛型枚举辅助类
/// </summary>
public static class EnumItem
{
    /// <summary>
    /// 缓存的 GetAll 结果, 按类型缓存避免重复反射
    /// </summary>
    private static readonly ConcurrentDictionary<Type, object> _cache = new();

    /// <summary>
    /// 获取枚举类型的所有值和描述文本, 用于 UI 下拉框数据绑定
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <returns>枚举项数组</returns>
    public static EnumItem<T>[] GetAll<T>() where T : struct, Enum =>
        (EnumItem<T>[])_cache.GetOrAdd(typeof(T), _ =>
        {
            var values = Enum.GetValues<T>();
            var items = new EnumItem<T>[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                items[i] = new(values[i]);
            }
            return items;
        });
}
