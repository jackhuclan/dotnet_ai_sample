using MegaAgent.App.ViewModels;
using MegaAgent.Core.Abstractions;
using MegaAgent.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegaAgent.App;

/// <summary>
/// 应用组合根：集中注册 Core 服务、View 层服务、ViewModel 与 View。
/// 应用启动（<see cref="App.OnStartup"/>）与离屏探针共用这一份注册，
/// 保证「测试环境 == 真实运行环境」的依赖图完全一致。
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>注册 MegaAgent 的全部服务。</summary>
    public static IServiceCollection AddMegaAgentServices(this IServiceCollection services)
    {
        // ---------- Core 服务（单例：全局共享同一份内存态与文件访问） ----------
        services.AddSingleton<ISettingsStore, SettingsStore>();
        services.AddSingleton<ISkillStore, SkillStore>();
        services.AddSingleton<IConversationStore, ConversationStore>();
        services.AddSingleton<IAgentService, AgentService>();
        services.AddSingleton<IStorageInitializer, StorageInitializer>();

        // ---------- View 层服务 ----------
        services.AddSingleton<IDialogService, DialogService>();

        // ---------- ViewModel ----------
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsDialogViewModel>();
        services.AddTransient<SkillsDialogViewModel>();
        // 说明：SkillEditorDialogViewModel 需要运行期参数（Skill），
        //      由 DialogService 通过 ActivatorUtilities 组合「容器依赖 + 运行期参数」构造。

        // ---------- View ----------
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsDialog>();
        services.AddTransient<SkillsDialog>();

        return services;
    }
}
