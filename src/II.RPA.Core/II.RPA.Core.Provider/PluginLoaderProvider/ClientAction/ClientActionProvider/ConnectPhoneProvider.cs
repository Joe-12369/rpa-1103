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
    [ClientActionName("connectPhone")]
    public class ConnectPhoneProvider : BaseActionProvider
    {
        public ConnectPhoneProvider(ILogger aLogger) : base(aLogger)
        {

        }
        public ConnectPhoneProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {

        }

        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginName = "Mobile.ConnectDevice",
                PluginAction = "Excute",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            var baseVal = base.ResolveReqVal(aReqVal);
            var baseValJo = JObject.Parse(baseVal);
            JObject jo = new JObject
            {
                { "customDeviceName", baseValJo.GetValue("customDeviceName") },
                { "syncFrequency", 5 },
                { "deviceName", baseValJo.GetValue("deviceName")  },
                { "appiumAddress", baseValJo.GetValue("appiumAddress")  },
                { "platformName", baseValJo.GetValue("platformName")  },
                { "platformVersion", baseValJo.GetValue("platformVersion")  },
                { "automationName", baseValJo.GetValue("automationName")  },
                { "timeout", 100000 },
            };
            return jo.ToString();
        }
    }
}
