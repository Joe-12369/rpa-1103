using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("outlookAccountsGet")]
    public class OutLookAccountsProvider : BaseActionProvider
    {
        public OutLookAccountsProvider(ILogger aLogger, IRuntimeHostController aRuntimeHostController) : base(aLogger, aRuntimeHostController)
        {
        }

        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "OutLook.CheckAccount",
            };
        }
    }
}
