using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace ElevatorSimulator.Share.Logging;

/// <summary>
/// 文件日志提供程序, 实现 <see cref="ILoggerProvider"/>, 每次运行创建独立日志文件, 按最近修改时间清理过期文件
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider, IDisposable
{
    /// <summary>
    /// 日志文件名中日期部分的格式
    /// </summary>
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// 日志文件名后缀
    /// </summary>
    private const string LogFileSuffix = ".log";

    /// <summary>
    /// 日志文件保留天数
    /// </summary>
    private const int RetainDays = 7;

    /// <summary>
    /// 日志目录
    /// </summary>
    private readonly DirectoryInfo _logDir;

    /// <summary>
    /// 当前日志文件路径
    /// </summary>
    private readonly string _currentFilePath;

    /// <summary>
    /// 文件写入锁, 保证线程安全
    /// </summary>
    private readonly Lock _writeLock = new();

    /// <summary>
    /// 日志记录器缓存, 键为类别名称
    /// </summary>
    private readonly ConcurrentDictionary<string, CustomLogger> _loggers = new();

    /// <summary>
    /// 最低日志级别, 低于此级别的日志将被忽略
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// 构造函数, 初始化日志目录、确定本次日志文件名并清理过期文件
    /// </summary>
    /// <param name="logDir">日志目录路径</param>
    public FileLoggerProvider(string logDir)
    {
        // 初始化日志目录, 如果不存在则创建
        _logDir = new(logDir);
        _logDir.Create();

        // 获取本次运行的日志文件路径
        _currentFilePath = GetNextLogFilePath();

        // 启动时清理过期日志
        CleanOldLogs();
    }

    /// <summary>
    /// 获取本次运行的日志文件路径, 格式为 yyyy-MM-dd-序号.log
    /// </summary>
    /// <returns>新的日志文件完整路径</returns>
    private string GetNextLogFilePath()
    {
        // 获取今天的日期前缀
        var todayPrefix = DateTime.Now.ToString(DateFormat);

        // 查找今天已有的日志文件, 获取最大序号
        var maxSeq = 0;
        try
        {
            // 遍历目录中符合今天前缀的日志文件
            foreach (var fileInfo in _logDir.GetFiles($"{todayPrefix}-*{LogFileSuffix}", SearchOption.TopDirectoryOnly))
            {
                // 获取不带扩展名的文件名部分, 解析出序号
                var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);

                // 获取前缀后面的序号部分
                var seqStr = fileName[(DateFormat.Length + 1)..];

                // 解析序号, 如果成功且大于当前最大值则更新最大值
                if (int.TryParse(seqStr, out var seq) && seq > maxSeq)
                {
                    maxSeq = seq;
                }
            }
        }
        catch (Exception) { }

        // 新序号 = 最大序号 + 1
        var nextSeq = (maxSeq + 1).ToString();

        // 构建新的日志文件路径
        return Path.Combine(_logDir.FullName, $"{todayPrefix}-{nextSeq}{LogFileSuffix}");
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, new CustomLogger(categoryName, true, level => level >= MinimumLevel, WriteLog));

    /// <summary>
    /// 将日志消息写入当前文件
    /// </summary>
    /// <param name="message">格式化后的日志消息</param>
    private void WriteLog(string message)
    {
        lock (_writeLock)
        {
            // 追加写入文件
            try
            {
                File.AppendAllText(_currentFilePath, message + Environment.NewLine, System.Text.Encoding.UTF8);
            }
            catch (Exception) { /* 写入失败时静默忽略, 避免日志系统干扰主业务 */ }
        }
    }

    /// <summary>
    /// 清理最近修改时间超过保留天数的旧日志文件
    /// </summary>
    private void CleanOldLogs()
    {
        try
        {
            // 计算过期时间点
            var cutoff = DateTime.Now.AddDays(-RetainDays);

            // 遍历目录中所有日志文件
            foreach (var fileInfo in _logDir.GetFiles($"*{LogFileSuffix}", SearchOption.TopDirectoryOnly))
            {
                // 如果文件的最后修改时间早于过期时间点, 则删除
                if (fileInfo.LastWriteTime < cutoff)
                {
                    try { fileInfo.Delete(); } catch (Exception) { }
                }
            }
        }
        catch (Exception) { }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}
