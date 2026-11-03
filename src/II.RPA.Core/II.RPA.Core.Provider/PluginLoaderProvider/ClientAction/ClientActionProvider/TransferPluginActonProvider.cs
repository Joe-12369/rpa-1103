using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using II.RPA.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction
{
    [ClientActionName("transferPluginAction")]
    public class TransferPluginActonProvider : BaseActionProvider
    {
        public TransferPluginActonProvider(ILogger aLogger) : base(aLogger)
        {

        }
        public TransferPluginActonProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {

        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            TransferPluginActonParams @params = JsonConvert.DeserializeObject<TransferPluginActonParams>(aReqVal);
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = @params.PluginAction,
            };
        }
        public bool TryResolveAction(string aReqVal, out PluginRunAction pluginRunAction, out string jsonParams)
        {
            TransferPluginActonParams @params;
            try
            {
                @params = JsonConvert.DeserializeObject<TransferPluginActonParams>(aReqVal);
            }
            catch (Exception)
            {
                m_Logger.LogError($"Failed to deserialize TtransferPluginActonParams from aReqVal: {aReqVal}");
                pluginRunAction = default;
                jsonParams = "";
                return false;
            }
            pluginRunAction =  new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = @params.PluginAction,
            };
            jsonParams = @params.JsonParams;
            return true;
        }
        public override string ResolveReqVal(string aReqVal)
        {
            TransferPluginActonParams @params = JsonConvert.DeserializeObject<TransferPluginActonParams>(aReqVal);
            return @params.JsonParams;
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRetVal = base.ResolveRetVal(aRetValFromPlugin);
            return baseRetVal;
        }
    }

    
}
