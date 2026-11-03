using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System.Configuration;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("GetWindowListConfiguration")]
    internal class GetWindowListProvider : BaseActionProvider
    {
        public GetWindowListProvider(ILogger aLogger, IRuntimeHostController aRuntimeHostController) : base(aLogger, aRuntimeHostController)
        {
        }
        public override int TimeoutSeconds
        {
            get
            {
                if (int.TryParse(ConfigurationManager.AppSettings["CaptureActionTimeoutSecond"], out int anCaptureActionTimeoutSecond))
                {
                    m_Logger.LogDebug($"TimeoutSeconds from config is {anCaptureActionTimeoutSecond}");
                    return anCaptureActionTimeoutSecond;
                }
                else
                {
                    m_Logger.LogDebug($"Can not read CaptureActionTimeoutSecond from config.reture base.TimeoutSeconds instead");
                    return base.TimeoutSeconds;
                }
            }
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Window.GetWindowList",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            var basReqVal = base.ResolveReqVal(aReqVal);
            var jOBaseReqVal = JObject.Parse(basReqVal);
            return jOBaseReqVal.ToString();
        }

        /// <summary>
        /// 数据采集结果返回加工
        /// </summary>
        /// <param name="aRetValFromPlugin"></param>
        /// <returns></returns>
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRetValJO = base.ResolveRetVal(aRetValFromPlugin);
            return baseRetValJO;
        }
    }
}
