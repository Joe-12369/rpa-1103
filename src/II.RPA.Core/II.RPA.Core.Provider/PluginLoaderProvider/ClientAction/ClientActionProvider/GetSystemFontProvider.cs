using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction
{
    [ClientActionName("getSystemFont")]
    internal class GetSystemFontProvider : BaseActionProvider
    {
        public GetSystemFontProvider(ILogger aLogger) : base(aLogger)
        {

        }
        public GetSystemFontProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {

        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Excel.GetSystemFont",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            return aReqVal;
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRetVal = base.ResolveRetVal(aRetValFromPlugin);
            return baseRetVal;
        }
    }
}
