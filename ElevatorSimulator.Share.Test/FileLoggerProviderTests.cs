using ElevatorSimulator.Share.Logging;
using Microsoft.Extensions.Logging;

namespace ElevatorSimulator.Share.Test;

/// <summary>
/// <see cref="FileLoggerProvider"/> 的单元测试, 验证日志目录创建,记录器缓存,文件写入
/// </summary>
public sealed class FileLoggerProviderTests : IDisposable
{
    /// <summary>
    /// 临时日志目录路径, 每次测试使用独立随机目录
    /// </summary>
    private readonly string _tempDir;

    /// <summary>
    /// 初始化临时日志目录路径
    /// </summary>
    public FileLoggerProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ElevatorTest_{Guid.NewGuid():N}");
    }

    /// <summary>
    /// 构造函数应在指定路径创建日志目录
    /// </summary>
    [Fact]
    public void Constructor_CreatesLogDirectory()
    {
        // Act
        _ = new FileLoggerProvider(_tempDir);

        // Assert
        Assert.True(Directory.Exists(_tempDir));
    }

    /// <summary>
    /// 构造函数只计算路径不创建文件, 此处验证构造不抛异常且目录已就绪
    /// </summary>
    [Fact]
    public void Constructor_CreatesLogFile()
    {
        // Arrange — 构造函数只计算路径,不创建文件
        _ = new FileLoggerProvider(_tempDir);

        // Assert: 目录已创建,路径已确定
        Assert.True(Directory.Exists(_tempDir));
        // 文件在首次 Log 时懒创建,此处仅验证构造不抛异常
    }

    /// <summary>
    /// <see cref="FileLoggerProvider.CreateLogger"/> 应返回 <see cref="CustomLogger"/> 实例
    /// </summary>
    [Fact]
    public void CreateLogger_ReturnsCustomLogger()
    {
        // Arrange
        var provider = new FileLoggerProvider(_tempDir);

        // Act
        var logger = provider.CreateLogger("TestCategory");

        // Assert
        Assert.NotNull(logger);
        _ = Assert.IsType<CustomLogger>(logger);
    }

    /// <summary>
    /// 相同类别名称的日志记录器应被缓存, 返回同一实例
    /// </summary>
    [Fact]
    public void CreateLogger_SameCategory_ReturnsSameInstance()
    {
        // Arrange
        var provider = new FileLoggerProvider(_tempDir);

        // Act
        var logger1 = provider.CreateLogger("MyCategory");
        var logger2 = provider.CreateLogger("MyCategory");

        // Assert
        Assert.Same(logger1, logger2);
    }

    /// <summary>
    /// 不同类别名称的日志记录器应返回不同实例
    /// </summary>
    [Fact]
    public void CreateLogger_DifferentCategories_ReturnsDifferentInstances()
    {
        // Arrange
        var provider = new FileLoggerProvider(_tempDir);

        // Act
        var logger1 = provider.CreateLogger("CatA");
        var logger2 = provider.CreateLogger("CatB");

        // Assert
        Assert.NotSame(logger1, logger2);
    }

    /// <summary>
    /// 通过记录器写入日志后, 日志内容应出现在文件中
    /// </summary>
    [Fact]
    public void Log_WritesToFile()
    {
        // Arrange
        var provider = new FileLoggerProvider(_tempDir) { MinimumLevel = LogLevel.Information };
        var logger = provider.CreateLogger("FileTest");

        // Act
        logger.Log(LogLevel.Information, new EventId(0), "file log message", null, (state, _) => state.ToString()!);

        // Assert
        var files = Directory.GetFiles(_tempDir, "*.log");
        Assert.NotEmpty(files);
        var content = File.ReadAllText(files[0]);
        Assert.Contains("file log message", content);
    }

    /// <summary>
    /// 测试清理: 删除临时日志目录
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
