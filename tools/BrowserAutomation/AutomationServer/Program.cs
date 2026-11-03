using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace II.BrowserAutomation
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddSignalR();
                builder.Services.AddCors();
                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = "II.RPA.BrowserAutomationServer";
                });
                builder.Logging.ClearProviders();

                var app = builder.Build();
                app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
                app.MapHub<BrowserHub>("/browser");

                Log.Info($"Server Running On {string.Join(",", app.Urls)}");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Info($"启动服务出现异常：{ex.Message}");
            }
        }
    }
}
