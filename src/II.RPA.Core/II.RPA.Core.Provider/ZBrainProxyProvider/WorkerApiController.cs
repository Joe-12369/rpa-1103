using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonServiceLocator;
using Microsoft.Owin;
using Newtonsoft.Json.Linq;
using II.RPA.Core.Runtime;

namespace II.RPA.Core.Provider.ZBrainProxyProvider
{
    public static class WorkerApiController
    {
        private static IWorkerControlController WorkerControlController =>
            ServiceLocator.Current.GetInstance<IWorkerControlController>();
        private static IZBrainController ZBrainController =>
            ServiceLocator.Current.GetInstance<IZBrainController>();

        public static Task CurrentWorker(IOwinContext context)
            => WriteCurrentWorker(context, childSession: false);

        public static Task CurrentChildWorker(IOwinContext context)
            => WriteCurrentWorker(context, childSession: true);

        private static async Task WriteCurrentWorker(IOwinContext context, bool childSession)
        {
            var worker = ZBrainController.GetCurrentWorker(childSession);

            await context.Response.WriteResponse(new ZBrainResponse<dynamic>
            {
                Result = worker,
            });
        }

        public static Task StopZBrainWorker(IOwinContext context)
            => StopZBrainWorker(context, childSession: false);

        public static Task StopChildZBrainWorker(IOwinContext context)
            => StopZBrainWorker(context, childSession: true);

        private static async Task StopZBrainWorker(IOwinContext context, bool childSession)
        {
            int.TryParse(await context.Request.GetFromRequest("pid"), out var pid);
            await ZBrainController.StopWorkerAsync(pid > 0 ? (int?)pid : null, childSession);
            await context.Response.WriteResponse(new ZBrainResponse<string> { });
        }
        
        public static async Task StopWorker(IOwinContext context)
        {
            int.TryParse(await context.Request.GetFromRequest("pid"), out var pid);
            await WorkerControlController.StopWorkerAsync(pid, context.Request.CallCancelled);
            await context.Response.WriteResponse(new {});
        }

        public static async Task StartMonitor(IOwinContext context)
        {
            await WorkerControlController.StartEventMonitorAsync(context.Request.CallCancelled);
        }

        public static async Task EngineAction(IOwinContext context)
        {
            var role = context.Request.Query.Get("role");

            var body = await context.Request.ReadFromRequest();
            await WorkerControlController.HandleEngineActionAsync(
                role,
                body,
                context.Request.CallCancelled);
        }

        public static async Task EngineProxyRestart(IOwinContext context)
        {
            await WorkerControlController.RestartEngineWorkersAsync(
                includeInterpreter: true,
                context.Request.CallCancelled);
        }

        public static async Task GetTopShare(IOwinContext context)
        {
            await context.Response.WriteResponse(WorkerControlController.GetTopShare());
        }

        public static async Task SetShare(IOwinContext context)
        {
            var role = context.Request.Query.Get("role");
            var name = context.Request.Query.Get("name");
            var body = await context.Request.ReadFromRequest();
            var json = JObject.Parse(body);

            var key = context.Request.Query.Get("key");
            WorkerControlController.SetShare(new WorkerQuery
            {
                Role = role ?? string.Empty,
                Name = name ?? string.Empty
            }, key, json);
        }

        public static async Task SetMeta(IOwinContext context)
        {
            var id = context.Request.Query.Get("id");
            var role = context.Request.Query.Get("role");
            var name = context.Request.Query.Get("name");
            var body = await context.Request.ReadFromRequest();
            var json = JObject.Parse(body);

            WorkerControlController.SetMeta(new WorkerQuery
            {
                Id = id ?? string.Empty,
                Role = role ?? string.Empty,
                Name = name ?? string.Empty
            }, new WorkerMetadata
            {
                Name = json["name"]?.Value<string>() ?? string.Empty,
                Role = json["role"]?.Value<string>() ?? string.Empty,
                Pid = json["pid"]?.Value<int>() ?? 0
            });
        }

        public static async Task CreateHostWorker(IOwinContext context)
        {
            var worker = await WorkerControlController.EnsureHostWorkerAsync(context.Request.CallCancelled);
            await context.Response.WriteResponse(worker);
        }

        public static async Task HostRun(IOwinContext context)
        {
            var body = await context.Request.ReadFromRequest();
            var worker = await WorkerControlController.RunHostAsync(body, context.Request.CallCancelled);
            await context.Response.WriteResponse(worker);
        }

        public static async Task HostReset(IOwinContext context)
        {
            var body = await context.Request.ReadFromRequest();
            var worker = await WorkerControlController.RunHostAsync(body, context.Request.CallCancelled);
            await context.Response.WriteResponse(worker);
        }

        public static async Task HostStop(IOwinContext context)
        {
            var actionId = await context.Request.GetFromRequest("id");
            await WorkerControlController.StopHostAsync(actionId, context.Request.CallCancelled);
            await context.Response.WriteResponse(new ZBrainResponse<string> { });
        }

        public static void SwitchWorker(IOwinContext context)
        {
            throw new NotImplementedException();
        }

        public static async Task GetZBrainWorkers(IOwinContext context)
        {
            await context.Response.WriteResponse(new ZBrainResponse<IReadOnlyCollection<ZBrainWorkerSnapshot>>
            {
                Result = ZBrainController.GetWorkers(),
            });
        }

        public static async Task GetWorkerDetail(IOwinContext context)
        {
            var talkers = WorkerControlController.GetWorkers(new WorkerQuery
            {
                Id = context.Request.Query.Get("id") ?? string.Empty,
                Role = context.Request.Query.Get("role") ?? string.Empty,
                Name = context.Request.Query.Get("name") ?? string.Empty
            });

            await context.Response.WriteResponse(talkers);
        }

        public static async Task GetWorkers(IOwinContext context, string group = null)
        {
            var talkers = WorkerControlController.GetWorkers(new WorkerQuery
            {
                Group = group ?? string.Empty
            });
            await context.Response.WriteResponse(talkers);
        }
    }
}
