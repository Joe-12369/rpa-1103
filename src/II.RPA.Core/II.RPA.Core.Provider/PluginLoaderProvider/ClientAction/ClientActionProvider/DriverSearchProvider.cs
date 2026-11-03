using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("DriverSearch")]
    class DriverSearchProvider: BaseActionProvider
    {
        public DriverSearchProvider(ILogger aLogger) : base(aLogger)
        {

        }

        public DriverSearchProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {

        }

        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginName = "DriverInput.DriverSearch",
                PluginAction = "Excute",
            };
        }

        public override string ResolveReqVal(string aReqVal)
        {
            var baseVal = base.ResolveReqVal(aReqVal);
            return baseVal;
        }

        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            return base.ResolveRetVal(aRetValFromPlugin);
        }
    }
}
