using System.Windows;
using System.Windows.Threading;
using MegaAgent.Core.Abstractions;
using MegaAgent.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MegaAgent.App;

/// <summary>
/// 应用入口：用 .NET 通用主机（Microsoft.Extensions.Hosting）承载依赖注入与生命周期。
/// 启动顺序 = 建主机 → 接线 NLog → 注册服务 → 启动主机 → 加载持久化数据 → 容器解析主窗口。
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private ILogger<App>? _logger;

    /// <summary>应用级服务容器（组合根）：供少数无法构造注入的场景取用。</summary>
    public static IServiceProvider Services => ((App)Current)._host!.Services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1) 通用主机：HostApplicationBuilder 提供 DI 容器、配置与日志
        var builder = Host.CreateApplicationBuilder(e.Args);
        builder.AddMegaAgentLogging();          // 日志统一交给 NLog（NLog.config）
        builder.Services.AddMegaAgentServices();

        _host = builder.Build();
        _logger = _host.Services.GetRequiredService<ILogger<App>>();
        HookGlobalExceptionHandlers();

        _logger.LogInformation("MegaAgent 正在启动（.NET {Version} / {Framework}）",
            Environment.Version, AppContext.TargetFrameworkName);
        _logger.LogInformation("日志目录：{LogDir}", LogPaths.Directory);

        _host.Start();

        // 2) 加载持久化数据（必须早于 MainViewModel 的解析：其构造依赖已加载的会话列表）
        _host.Services.GetRequiredService<IStorageInitializer>().Initialize();

        // 3) 主窗口交给容器解析，ViewModel/服务依赖自动注入
        var main = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = main;
        main.Show();

        _logger.LogInformation("主窗口已显示，MegaAgent 启动完成");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("MegaAgent 正在退出（ExitCode={ExitCode}）", e.ApplicationExitCode);

        if (_host is not null)
        {
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
            _host = null;
        }

        // 刷出 NLog 异步队列中尚未落盘的日志，然后关闭日志系统
        LoggingRegistrationExtensions.Shutdown();

        base.OnExit(e);
    }

    /// <summary>
    /// 兜住三个层面的未处理异常，保证「异常一定进日志」，而不是只弹一个对话框就消失：
    /// UI 线程（Dispatcher）/ 后台线程与进程级（AppDomain）/ 未观察的 Task（TaskScheduler）。
    /// </summary>
    private void HookGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "UI 线程发生未处理异常");
        MessageBox.Show(
            $"程序遇到一个问题，已记录到日志：\n\n{e.Exception.Message}\n\n日志目录：{LogPaths.Directory}",
            "MegaAgent",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        e.Handled = true;   // 保留现场，避免用户正在进行的会话被直接中断
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger?.LogCritical(e.ExceptionObject as Exception, "后台线程发生未处理异常，进程即将终止");
        LoggingRegistrationExtensions.Shutdown();
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "存在未观察的 Task 异常");
        e.SetObserved();
    }
}
