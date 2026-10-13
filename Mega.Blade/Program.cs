using Mega.Platform.Core.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WindowsFormsLifetime;

namespace Mega.Blade;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        var host = Host.CreateDefaultBuilder(args)
              .ConfigureAppConfiguration((hostBuilderContext, builder) =>
              {
                  builder.AddJsonFile($"./appsettings.json");
              })
              .ConfigureServices((hostBuilderContext, service) =>
              {
                  service.AddMqttCapability(hostBuilderContext.Configuration);
                  service.AddSingleton<IDevice, AOIDevice>();
              })
              .ConfigureLogging((hostBuilderContext, loggingBuilder) =>
              {
                  // NLog: Setup NLog for Dependency injection
                  //loggingBuilder.cle
                  //loggingBuilder.AddNLog();
              })
              .UseWindowsFormsLifetime<Form1>()
              .Build();

        host.Run();
    }
}