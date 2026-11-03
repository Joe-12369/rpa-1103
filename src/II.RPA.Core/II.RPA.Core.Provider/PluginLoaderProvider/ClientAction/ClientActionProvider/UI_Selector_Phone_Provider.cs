using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("phoneSelector")]
    internal class UI_Selector_Phone_Provider : BaseActionProvider
    {
        public UI_Selector_Phone_Provider(ILogger aLogger) : base(aLogger)
        {

        }
        public UI_Selector_Phone_Provider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {
            NeedSTA = true;
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction 
            {
                PluginName = "Mobile.GetMobileElementInfo",
                PluginAction = "Excute",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            JObject reqValObject = JObject.Parse(base.ResolveReqVal(aReqVal));
            reqValObject.Add("filepath", m_APicturePath);
            reqValObject.Add("timeout", "1000000");
            return reqValObject.ToString();
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRetVal = base.ResolveRetVal(aRetValFromPlugin);
            if (baseRetVal.Code == "0")
            {
                var selector = JToken.Parse(baseRetVal.Result);
                JObject uiSelector;
                if (selector is JArray)
                {
                    m_Logger.LogDebug("旧版selector");
                    uiSelector = new JObject
                    {
                       { "selector",selector},
                       { "shot",m_RPicturePath},
                       {
                            "info",
                            new JObject
                            {
                                { "windowType","mobile"},
                                { "windowName",""},
                                { "elementName",""},
                            }
                       },
                    };
                }
                else
                {
                    m_Logger.LogDebug("新版selector");
                    selector["commonProps"]["windowType"] = "mobile";
                    if (selector["commonProps"]["filePath"] != null)
                    {
                        selector["commonProps"]["filePath"] = m_RPicturePath;
                    }
                    if (selector["elementDetail"] != null)
                    {
                        var icon_name = selector["elementDetail"]["categoryIcon"];
                        selector["elementDetail"]["categoryIcon"] = Path.Combine("res", icon_name.ToString());
                    }
                    uiSelector = selector as JObject;
                }
                
                baseRetVal.Result = new JObject
                {
                    {"uiSelector",uiSelector},
                }.ToString();
                return baseRetVal;
            }
            else
            {
                return baseRetVal;
            }
        }
    }
}
