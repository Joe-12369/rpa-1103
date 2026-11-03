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
    [ClientActionName("mouseRecordPhone")]
    public class MouseRecordPhoneProvider : BaseActionProvider
    {
        public MouseRecordPhoneProvider(ILogger aLogger) : base(aLogger)
        {

        }
        public MouseRecordPhoneProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {

        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Mobile.GetMobileElementPoint",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            return new JObject
            {
                {"timeOut",300000},
            }.ToString();
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseval = base.ResolveRetVal(aRetValFromPlugin);
            var loaction = baseval.Result?.Split(',');
            if (loaction == null || loaction.Length != 2)
            {
                return baseval;
            }
            else
            {
                baseval.Result = new JObject
                {
                    {"x",loaction[0] },
                    {"y",loaction[1] },
                }.ToString();
                return baseval;
            }
        }
    }
}
