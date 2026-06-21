using ElevatorSimulator.Share.Logging;
using Microsoft.Extensions.Logging;

namespace ElevatorSimulator.Share.Test;

/// <summary>
/// <see cref="CustomLogger"/> 的单元测试, 验证日志级别过滤,格式化输出,异常信息追加
/// </summary>
public sealed class CustomLoggerTests
{
    /// <summary>
    /// 当委托返回 <see langword="true"/> 时, <see cref="CustomLogger.IsEnabled"/> 应返回 <see langword="true"/>
    /// </summary>
    [Fact]
    public void IsEnabled_ReturnsTrue_WhenDelegateReturnsTrue()
    {
        // Arrange
        var logger = new CustomLogger("Test", false, _ => true, _ => { });

        // Act
        var result = logger.IsEnabled(LogLevel.Information);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 当委托返回 <see langword="false"/> 时, <see cref="CustomLogger.IsEnabled"/> 应返回 <see langword="false"/>
    /// </summary>
    [Fact]
    public void IsEnabled_ReturnsFalse_WhenDelegateReturnsFalse()
    {
        // Arrange
        var logger = new CustomLogger("Test", false, _ => false, _ => { });

        // Act
        var result = logger.IsEnabled(LogLevel.Error);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 日志级别启用时, 应输出包含时间,级别,类别,消息内容的格式化日志
    /// </summary>
    [Fact]
    public void Log_WritesFormattedOutput_WhenEnabled()
    {
        // Arrange
        var outputs = new List<string>();
        var logger = new CustomLogger("TestCat", true, level => level >= LogLevel.Information, outputs.Add);

        // Act
        logger.Log(LogLevel.Information, new EventId(0), "hello world", null, (state, _) => state.ToString()!);

        // Assert
        _ = Assert.Single(outputs);
        Assert.Contains("[INFO]", outputs[0]);
        Assert.Contains("[TestCat]", outputs[0]);
        Assert.Contains("hello world", outputs[0]);
    }

    /// <summary>
    /// 日志级别未启用时, <see cref="CustomLogger.Log"/> 不应触发输出动作
    /// </summary>
    [Fact]
    public void Log_DoesNotWrite_WhenDisabled()
    {
        // Arrange
        var outputs = new List<string>();
        var logger = new CustomLogger("Test", false, level => level >= LogLevel.Error, outputs.Add);

        // Act
        logger.Log(LogLevel.Information, new EventId(0), "should not appear", null, (state, _) => state.ToString()!);

        // Assert
        Assert.Empty(outputs);
    }

    /// <summary>
    /// 当日志包含异常时, 应将异常信息追加到日志输出中
    /// </summary>
    [Fact]
    public void Log_IncludesException_WhenExceptionProvided()
    {
        // Arrange
        var outputs = new List<string>();
        var logger = new CustomLogger("Test", false, _ => true, outputs.Add);

        // Act
        logger.Log(LogLevel.Error, new EventId(0), "error occurred", new InvalidOperationException("test ex"), (state, _) => state.ToString()!);

        // Assert
        _ = Assert.Single(outputs);
        Assert.Contains("[ERROR]", outputs[0]);
        Assert.Contains("test ex", outputs[0]);
    }

    /// <summary>
    /// 各日志级别应正确映射到对应的简写标签 (TRACE/DEBUG/INFO/WARN/ERROR/CRIT)
    /// </summary>
    [Theory]
    [InlineData(LogLevel.Trace, "TRACE")]
    [InlineData(LogLevel.Debug, "DEBUG")]
    [InlineData(LogLevel.Information, "INFO")]
    [InlineData(LogLevel.Warning, "WARN")]
    [InlineData(LogLevel.Error, "ERROR")]
    [InlineData(LogLevel.Critical, "CRIT")]
    public void Log_OutputsCorrectLevelString(LogLevel level, string expectedLabel)
    {
        // Arrange
        var outputs = new List<string>();
        var logger = new CustomLogger("Test", false, _ => true, outputs.Add);

        // Act
        logger.Log(level, new EventId(0), "msg", null, (state, _) => state.ToString()!);

        // Assert
        _ = Assert.Single(outputs);
        Assert.Contains($"[{expectedLabel}]", outputs[0]);
    }

    /// <summary>
    /// <see cref="CustomLogger.BeginScope"/> 应始终返回 <see langword="null"/>
    /// </summary>
    [Fact]
    public void BeginScope_ReturnsNull()
    {
        // Arrange
        var logger = new CustomLogger("Test", false, _ => true, _ => { });

        // Act
        var scope = logger.BeginScope("any state");

        // Assert
        Assert.Null(scope);
    }

    /// <summary>
    /// 当 <c>includeCategory</c> 为 <see langword="false"/> 时, 日志输出中不应出现类别名称
    /// </summary>
    [Fact]
    public void Log_WithoutCategory_OmitsCategoryName()
    {
        // Arrange
        var outputs = new List<string>();
        var logger = new CustomLogger("HiddenCat", false, _ => true, outputs.Add);

        // Act
        logger.Log(LogLevel.Information, new EventId(0), "msg", null, (state, _) => state.ToString()!);

        // Assert
        Assert.DoesNotContain("[HiddenCat]", outputs[0]);
    }
}
