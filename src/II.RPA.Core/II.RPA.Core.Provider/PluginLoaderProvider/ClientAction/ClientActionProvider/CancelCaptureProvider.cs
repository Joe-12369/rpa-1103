using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("cancelComboSelectUi")]
    public class CancelCaptureProvider : BaseActionProvider
    {
        public CancelCaptureProvider(ILogger aLogger, IRuntimeHostController aRuntimeHostController) : base(aLogger, aRuntimeHostController)
        {
        }

        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Element.CancelCapture",
            };
        }
    }
}
