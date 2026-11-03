using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("GetWindowSize")]
    public class GetWindowSizeProvider : BaseActionProvider
    {
        public GetWindowSizeProvider(ILogger aLogger, IRuntimeHostController aRuntimeHostController) : base(aLogger, aRuntimeHostController)
        {
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Window.GetWindowRect",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            var basReqVal = base.ResolveReqVal(aReqVal);
            var jOBaseReqVal = JObject.Parse(basReqVal);
            return jOBaseReqVal.ToString();
        }

        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRetValJO = base.ResolveRetVal(aRetValFromPlugin);
            return baseRetValJO;
        }
    }
}
