
using Mega.Platform.Core.Host;
using NLog.Web;

namespace Mega.Platform.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddHost(builder.Configuration);


        // NLog: Setup NLog for Dependency injection
        builder.Logging.ClearProviders();
        //builder.Logging.AddConsole();
        builder.Services.AddLogging(logging =>
        {
            logging.AddNLogWeb("./NLog.config");
        });
        builder.Host.UseNLog();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // wwwroot 下的 html/css/js 可直接通过 URL 打开，根路径默认打开 index.html
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
