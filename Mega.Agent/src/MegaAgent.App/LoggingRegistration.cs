using System.IO;
using MegaAgent.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Hosting;
// NLog 与 Microsoft.Extensions.Logging 都有 LogLevel 类型，这里固定用后者的语义
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MegaAgent.App;

/// <summary>
/// 日志接线：把 .NET 的 <see cref="ILogger{T}"/> 管道整体交给 NLog。
///
/// 关键点：
/// 1. 先 <c>ClearProviders()</c> 摘掉 Host 默认注入的 Console / Debug / EventSource provider，
///    避免同一行日志被多套管道重复输出；
/// 2. 再 <c>UseNLog()</c>（NLog.Extensions.Hosting）把 NLog 注册成唯一日志提供程序，
///    它会从输出目录自动加载 <c>NLog.config</c>；配置文件缺失时直接抛错，而不是静默丢日志；
/// 3. 退出前调用 <see cref="Shutdown"/> 刷出 AsyncWrapper 队列，防止进程结束时丢日志。
///
/// 应用启动（<see cref="App.OnStartup"/>）与离屏探针共用本方法，保证「测试 == 运行」的日志行为一致。
/// </summary>
public static class LoggingRegistrationExtensions
{
    /// <summary>NLog 配置文件（随生成目录一并输出）。</summary>
    public static string ConfigFilePath { get; } = Path.Combine(AppContext.BaseDirectory, "NLog.config");

    /// <summary>把日志系统切换为 NLog。</summary>
    public static IHostApplicationBuilder AddMegaAgentLogging(this IHostApplicationBuilder builder)
    {
        if (!File.Exists(ConfigFilePath))
            throw new FileNotFoundException($"未找到 NLog 配置文件：{ConfigFilePath}", ConfigFilePath);

        // 1) 摘掉默认 provider（Console / Debug / EventSource），避免一行日志被多套管道重复输出
        builder.Logging.ClearProviders();

        // 2) 日志目录与配置目录一起准备好（配置里写的是 %APPDATA%\MegaAgent\logs）
        LogPaths.EnsureDir();

        // 3) 先显式加载一次 NLog 配置：让 Host 构建完成之前的静态 LogManager 调用也已生效
        LogManager.Setup().LoadConfigurationFromFile(ConfigFilePath);

        // 4) ILogger<T> → NLog（UseNLog 内部会注册 NLogLoggerProvider）
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.UseNLog();

        return builder;
    }

    /// <summary>刷出异步队列并关闭 NLog，应在进程退出前调用。</summary>
    public static void Shutdown() => LogManager.Shutdown();
}
