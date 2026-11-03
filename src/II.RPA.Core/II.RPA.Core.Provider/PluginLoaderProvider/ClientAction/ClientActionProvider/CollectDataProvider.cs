using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction
{
    [ClientActionName("collectData")]
    internal class CollectDataProvider : BaseActionProvider
    {
        public CollectDataProvider(ILogger aLogger) : base(aLogger)
        {

        }
        public CollectDataProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {

        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "WebBrowser.StartConfig",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            var basePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            var jsPath = Path.Combine(Path.GetFullPath(@"..//..//..//..//.."), "support\\xiaozhangyu\\executor\\index.js");
            var urlPath = Path.Combine(Path.GetFullPath(@"..//..//.."), "resource\\data_crawl\\configure\\configure.html");

            if (!File.Exists(jsPath))
            {
                
            }
            if (!File.Exists(urlPath))
            {

            }

            var jo = JObject.Parse(aReqVal);
            return new JObject
            {   
                {"jsPath",jsPath},
                {"config",jo.GetValue("config").ToString()},
                {"url",urlPath},
                {"browserType",jo.GetValue("browserType")},
            }.ToString();
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRetVal = base.ResolveRetVal(aRetValFromPlugin);
            if (baseRetVal.Result.Contains("数据爬取已取消"))
            {
                baseRetVal.Code = "1";
                baseRetVal.Message = baseRetVal.Result;
            }
            return baseRetVal;
        }
    }
}
