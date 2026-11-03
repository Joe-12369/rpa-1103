using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonServiceLocator;
using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Owin;

namespace II.RPA.Core.Provider.ZBrainProxyProvider
{
    public class Startup
    {
        private IZBrainController ZBrainController =>
            ServiceLocator.Current.GetInstance<IZBrainController>();
        private ILogger Logger =>
            ServiceLocator.Current.GetInstance<ILogger>();

        /// <summary>
        /// 插件化版本插件加载
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task PackagePluginLoad(IOwinContext context)
        {
            var packagePath = await context.Request.GetFromRequest("packagePath");

            if (!Directory.Exists(packagePath))
            {
                await context.Response.WriteResponse(new ZBrainResponse<string>
                {
                    Message = "流程包地址不存在",
                    Code = "010300200010002",
                });
                return;
            }

            if (!await CheckZBrain(context))
            {
                return;
            }

            var result = await ZBrainController.SwitchPackageAsync(
                packagePath,
                scan: true,
                cancellationToken: CancellationToken.None);
            if (!result.IsSuccess)
            {
                await context.Response.WriteResponse(new ZBrainResponse<string>
                {
                    Code = result.Code,
                    Message = result.Message,
                });
                return;
            }

            await context.Response.WriteResponse(new ZBrainResponse<string>
            {
                Message = result.Message,
            });
        }

        /// <summary>
        /// 检查ZBrain是否已配置插件路径
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task PluginLoadCheck(IOwinContext context)
        {
            var worker = ZBrainController.GetCurrentWorker();
            if (worker == null)
            {
                await context.Response.WriteResponse(new
                {
                    Message = "无Z-Brain工作进程",
                    Code = "-1",
                });
                return;
            }

            if (worker.PluginLoaded)
            {
                await context.Response.WriteResponse(new
                {
                    Message = "",
                    Code = "0",
                });
            }
            else
            {
                await context.Response.WriteResponse(new
                {
                    Message = "未配置插件路径",
                    Code = "-2",
                });
            }
        }

        private async Task<bool> CheckZBrain(IOwinContext context)
        {
            var worker = ZBrainController.GetCurrentWorker();
            if (worker == null)
            {
                await context.Response.WriteErrorResponse("无Z-Brain工作进程");
                return false;
            }

            if (string.Equals(worker.State, "Terminated", StringComparison.OrdinalIgnoreCase))
            {
                await context.Response.WriteErrorResponse("当前Z-Brain工作进程已退出");
                return false;
            }
            return true;
        }

        private async Task ProxyHandle(IOwinContext context, bool waitLoaded = false)
        {
            try
            {
                var req = context.Request;
                string body = await req.ReadFromRequest();

                if (!await CheckZBrain(context))
                {
                    return;
                }

                var request = new ZBrainProxyRequestData
                {
                    Method = req.Method,
                    Path = req.Uri.PathAndQuery,
                    ContentType = req.ContentType ?? string.Empty,
                    Headers = req.Headers.ToDictionary(p => p.Key, p => p.Value.Length > 0 ? p.Value[0] : string.Empty),
                    Body = body,
                };
                var responseBody = await ZBrainController.ProxyAsync(
                    request,
                    cancellationToken: CancellationToken.None);
                if (responseBody != null)
                {
                    await context.Response.WriteAsync(responseBody);
                }
                else
                {
                    await context.Response.WriteAsync($"{new JObject { { "code", "0" } }}");
                }
            }
            catch (Exception ex) 
            {
                Logger.LogError("ProxyHandle",ex);
            }
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            appBuilder.Use<LoggingMiddleware>();

            appBuilder.Map("/plugin/init", config => config.Run(PackagePluginLoad));
            appBuilder.Map("/plugin/configured/check", config => config.Run(PluginLoadCheck));

            appBuilder.Map("/component/recommender", config => config.Run(ctx => ProxyHandle(ctx, waitLoaded: true)));

            appBuilder.Map("/workers/zbrain", config => {
                config.Map("/current", sub => sub.SafeRun(WorkerApiController.CurrentWorker));
                config.Map("/stop", sub => sub.SafeRun(WorkerApiController.StopZBrainWorker));
                config.Map("", sub => sub.SafeRun(WorkerApiController.GetZBrainWorkers));
            });

            appBuilder.Map("/workers/host", config => {
                config.Map("/create", sub => sub.SafeRun(WorkerApiController.CreateHostWorker));
                config.Map("/run", sub => sub.SafeRun(WorkerApiController.HostRun));
                config.Map("/stop", sub => sub.SafeRun(WorkerApiController.HostStop));
                config.Map("", sub => sub.SafeRun(ctx => WorkerApiController.GetWorkers(ctx, "host")));
            });

            appBuilder.Map("/workers/monitor", config => {
                config.Map("/start", sub => sub.SafeRun(WorkerApiController.StartMonitor));
            });

            appBuilder.Map("/workers/engine", config => {
                config.Map("/action", sub => sub.SafeRun(WorkerApiController.EngineAction));
                config.Map("", sub => sub.SafeRun(ctx => WorkerApiController.GetWorkers(ctx, "engine")));
            });

            appBuilder.Map("/workers/httpserver", config => {
                config.Map("/engine/restart", sub => sub.SafeRun(WorkerApiController.EngineProxyRestart));
            });

            appBuilder.Map("/workers", config => {
                config.Map("/stop", sub => sub.SafeRun(WorkerApiController.StopWorker));
                config.Map("/meta", sub => sub.SafeRun(WorkerApiController.SetMeta));
                config.Map("/share/set", sub => sub.SafeRun(WorkerApiController.SetShare));
                config.Map("/topshare", sub => sub.SafeRun(WorkerApiController.GetTopShare));
                config.Map("", sub => sub.SafeRun(ctx => WorkerApiController.GetWorkerDetail(ctx)));
            });

            appBuilder.Run(ctx => ProxyHandle(ctx));
        }
    }
}
