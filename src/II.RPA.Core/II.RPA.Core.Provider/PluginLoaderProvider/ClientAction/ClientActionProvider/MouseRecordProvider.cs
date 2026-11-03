using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using II.RPA.Core.Provider.PluginLoaderProvider;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction
{
    [ClientActionName("mouseRecord")]
    public class MouseRecordProvider : BaseActionProvider
    {
        public MouseRecordProvider(ILogger aLogger) : base(aLogger)
        {

        }
        public MouseRecordProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {
            NeedSTA = true;
        }
        public override string ActionName => "mouseRecord";
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Element.CapturePosition",
            };
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var ret = base.ResolveRetVal(aRetValFromPlugin);
            if (ret == default || ret == null)
            {
                return null;
            }
            else if (string.IsNullOrEmpty(ret.Result))
            {
                return ret;
            }
            else
            {
                var retobj = JObject.Parse(ret.Result);
                retobj.Remove("positiontype");
                ret.Result = retobj.ToString();
                return ret;
            }
        }
    }
}
