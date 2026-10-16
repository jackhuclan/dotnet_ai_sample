using System.Windows;
using MegaAgent.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MegaAgent.App;

/// <summary>
/// 应用入口：用 .NET 通用主机（Microsoft.Extensions.Hosting）承载依赖注入与生命周期。
/// 启动顺序 = 建主机 → 注册服务 → 启动主机 → 加载持久化数据 → 容器解析主窗口。
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    /// <summary>应用级服务容器（组合根）：供少数无法构造注入的场景取用。</summary>
    public static IServiceProvider Services => ((App)Current)._host!.Services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1) 通用主机：HostApplicationBuilder 提供 DI 容器、配置与日志
        var builder = Host.CreateApplicationBuilder(e.Args);
        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();
        builder.Services.AddMegaAgentServices();

        _host = builder.Build();
        _host.Start();

        // 2) 加载持久化数据（必须早于 MainViewModel 的解析：其构造依赖已加载的会话列表）
        _host.Services.GetRequiredService<IStorageInitializer>().Initialize();

        // 3) 主窗口交给容器解析，ViewModel/服务依赖自动注入
        var main = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = main;
        main.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
            _host = null;
        }

        base.OnExit(e);
    }
}
