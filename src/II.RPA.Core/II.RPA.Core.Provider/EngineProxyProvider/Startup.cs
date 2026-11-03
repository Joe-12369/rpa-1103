using System;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Windows.Input;
using CommonServiceLocator;
using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Provider.ZBrainProxyProvider;
using Microsoft.Owin;
using Newtonsoft.Json.Linq;
using Owin;

namespace II.RPA.Core.Provider.EngineProxyProvider
{
    public class Startup
    {
        private ILogger Logger =>
            ServiceLocator.Current.GetInstance<ILogger>();

        private static async Task CheckConnection(IOwinContext context)
        {
            await context.Response.WriteResponse(new
            {
                code = 0,
                msg = "SUCCESS",
            });
        }

        private static async Task MobileMsg(IOwinContext context)
        {
            var body = await context.Request.ReadFromRequest();
            var json = JObject.Parse(body);

            if (json.TryGetValue("data", out var data))
            {
                var dataStr = data.Value<string>();

                var insideJson = JObject.Parse(dataStr);
                if (insideJson.TryGetValue("encryData", out var encryData))
                {
                    var encryDataStr = encryData.Value<string>().Replace("\n", "");
                    var controller = ServiceLocator.Current.GetInstance<IWorkerControlController>();

                    controller.WriteShareChannel(PredefineChannel.MobileMessageQueueName, JObject.FromObject(new
                    {
                        data = encryDataStr,
                    }));

                    await context.Response.WriteResponse(new
                    {
                        code = 0,
                        msg = "SUCCESS",
                    });
                    return;
                }
            }

            await context.Response.WriteResponse(new
            {
                code = 1,
                msg = "FAILD",
            });
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            appBuilder.Use<LoggingMiddleware>();

            appBuilder.Map("/mobile_msg", config => config.Run(MobileMsg));
            appBuilder.Map("/check_connection", config => config.Run(CheckConnection));

            appBuilder.Run(async (ctx) => await ctx.Response.WriteResponse(new
            {
                code = -1,
                msg = "Not implemented",
            }, isSuccess: false));
        }
    }
}
