using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("ConfigDialogPreview")]
    public class ConfigDialogPreviewProvider : BaseActionProvider
    {
        public ConfigDialogPreviewProvider(ILogger aLogger, IRuntimeHostController aRuntimeHostController) : base(aLogger, aRuntimeHostController)
        {
            NeedSTA = true;
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Dialog.ConfigDialog",
            };
        }
    }
}
