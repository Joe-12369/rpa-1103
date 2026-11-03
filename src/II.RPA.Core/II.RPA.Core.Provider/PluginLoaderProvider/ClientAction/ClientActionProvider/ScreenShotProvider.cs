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
    [ClientActionName("screenshot")]
    public class ScreenShotProvider : BaseActionProvider
    {
        public ScreenShotProvider(ILogger aLogger) : base(aLogger)
        {
        }
        public ScreenShotProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {

        }
        public override string ResolveReqVal(string aReqVal)
        {
            JObject reqValJObject = JObject.Parse(base.ResolveReqVal(aReqVal));
            reqValJObject.Add("packagepath", m_PackagePath);
            return reqValJObject.ToString();
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Element.ScreenShot",
            };
        }
    }
}
