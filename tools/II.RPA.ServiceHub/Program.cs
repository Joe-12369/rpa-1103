using II.RPA.Logging;
using II.RPA.Logging.Definitions;
using II.RPA.ServiceHub.Transfer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace II.RPA.ServiceHub
{
    class Program
    {
        private static ILogger logger = Logger.OfContext();

        static void Main(string[] args)
        {
            try
            {
                var programData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "II.RPA");
                Logger.ConfigWithRole(LoggingRoles.Default, programData);

                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddSignalR();
                builder.Services.AddCors();
                builder.Services.AddSingleton<TransferMessageExecutor>();
                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = "II.RPA.ServiceHub";
                });
                builder.Logging.ClearProviders();

                var app = builder.Build();
                app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
                app.MapHub<TransferHub>("/rpa");

                logger.LogInformation($"Server Running On {string.Join(",", app.Urls)}");
                app.Run();
            }
            catch (Exception ex)
            {
                logger.LogError($"启动服务出现异常：{ex.Message}");
            }
        }
    }
}
