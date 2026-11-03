using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction
{
    [ClientActionName("USBGetComs")]
    public class USBGetComsProvider : BaseActionProvider
    {
        public USBGetComsProvider(ILogger aLogger) : base(aLogger)
        {
        }
        public USBGetComsProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {

        }
        public override string ActionName => "USBGetComs";

        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "UsbHub.LocalGetComs",
            };
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var ret = base.ResolveRetVal(aRetValFromPlugin);
            return ret;
        }
    }
}
