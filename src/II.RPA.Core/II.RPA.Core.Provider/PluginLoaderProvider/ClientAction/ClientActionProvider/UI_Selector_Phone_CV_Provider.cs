using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("phoneSelectorCv")]
    internal class UI_Selector_Phone_CV_Provider : BaseActionProvider
    {
        public UI_Selector_Phone_CV_Provider(ILogger aLogger) : base(aLogger)
        {

        }
        public UI_Selector_Phone_CV_Provider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {
            NeedSTA = true;
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginName = "Mobile.GetMobileElementCV",
                PluginAction = "Excute",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            return new JObject
            {
                { "filepath",m_APicturePath},
                //{ "CVExecutionServiceURL",cv_saas_server},
                { "CVExecutionServiceURL",cvPhonePickServer},
                { "timeout","1000000"},
            }.ToString();
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
                                { "windowType","mobileCv"},
                                { "windowName",""},
                                { "elementName",""},
                            }
                       },
                    };
                }
                else
                {
                    m_Logger.LogDebug("新版selector");
                    selector["commonProps"]["windowType"] = "mobileCv";
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
