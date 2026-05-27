using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ElevatorSimulator.Server;

/// <summary>
/// 主程序类
/// </summary>
file static class Program
{
    /// <summary>
    /// 程序唯一标识符
    /// </summary>
    private const string AppId = $"Local\\ElevatorServer_{Share.Constants.Author}";

    /// <summary>
    /// 全局互斥体, 保证单实例
    /// </summary>
    private static Mutex? _mutex;

    /// <summary>
    /// 未知异常类
    /// </summary>
    /// <param name="message">异常消息</param>
    private sealed class UnknownException(string message) : Exception(message);

    /// <summary>
    /// 程序入口点
    /// </summary>
    [STAThread]
    private static void Main()
    {
        // 设置未处理异常模式为捕获异常
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        // 绑定未捕获异常事件
        Application.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // 初始化应用程序配置
        ApplicationConfiguration.Initialize();

        try
        {
            // 保证只运行一个实例
            _mutex = new(true, AppId, out var isNewInstance);
            if (!isNewInstance)
            {
                _ = MessageBox.Show("程序已在运行中！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 显示主窗口
            Application.Run(UI.MainForm.Instance);
        }
        finally
        {
            // 只有获得互斥体时才释放
            if (_mutex is not null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch (ApplicationException) { }
                _mutex.Dispose();
                _mutex = null;
            }
        }
    }

    /// <summary>
    /// 处理未处理的线程异常
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">线程异常事件参数</param>
    private static void OnThreadException(object sender, ThreadExceptionEventArgs e) => _ = MessageBox.Show($"发生未处理的线程异常: {e.Exception.Message}", "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);

    /// <summary>
    /// 处理未处理的应用程序异常
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">未处理异常事件参数</param>
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception ?? new UnknownException("未知异常");
        _ = MessageBox.Show($"发生未处理的应用程序异常: {ex.Message}", "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// 处理未观察到的任务异常
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">未观察到的任务异常事件参数</param>
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // 弹窗提示错误信息
        _ = MessageBox.Show($"发生未观察到的任务异常: {e.Exception.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);

        // 标记异常已处理, 以防止程序崩溃
        e.SetObserved();
    }
}
