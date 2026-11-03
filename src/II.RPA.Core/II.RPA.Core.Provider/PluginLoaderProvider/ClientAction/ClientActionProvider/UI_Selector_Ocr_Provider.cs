using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("uiselectorOcr")]
    internal class UI_Selector_Ocr_Provider : BaseActionProvider
    {
        public UI_Selector_Ocr_Provider(ILogger aLogger) : base(aLogger)
        {
        }
        public UI_Selector_Ocr_Provider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {
            NeedSTA = true;
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Element.CaptureCV",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            //var baseReqVal = base.ResolveReqVal(aReqVal);
            return new JObject 
            {
                {"filepath",m_APicturePath},
            }.ToString();
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRetVal = base.ResolveRetVal(aRetValFromPlugin);
            if (baseRetVal.Code == "0")
            {
                if (string.IsNullOrEmpty(baseRetVal.Result))
                {
                    baseRetVal.Code = "1";
                    baseRetVal.Message = "拾取失败";
                    return baseRetVal;
                }
                else
                {
                    JObject uiSelector;
                   
                    var info = JToken.Parse(baseRetVal.Result);
                    if (info is JArray)
                    {
                        uiSelector = new JObject
                        {
                            { "not_ie",info},
                            { "shot", m_RPicturePath },
                            {
                                "info", 
                                new JObject
                                {
                                    {"windowType","CV" },
                                    {"windowName","" },
                                    {"elementName","" },
                                }
                            }
                        };
                    }
                    else
                    {
                        uiSelector = info as JObject;
                        if (uiSelector["commonProps"]["filePath"] != null)
                        {
                            uiSelector["commonProps"]["filePath"] = this.GetRPicturePath(uiSelector["commonProps"]["filePath"]);
                        }
                        if (uiSelector["elementDetail"] != null)
                        {
                            var icon_name = uiSelector["elementDetail"]["categoryIcon"];
                            try
                            {
                                if (icon_name != null && !string.IsNullOrEmpty(icon_name.ToString()))
                                {
                                    uiSelector["elementDetail"]["categoryIcon"] = Path.Combine("res", icon_name.ToString());
                                }
                                else
                                {
                                    uiSelector["elementDetail"]["categoryIcon"] = icon_name?.ToString();
                                }
                            }
                            catch(Exception ex)
                            {
                                m_Logger.LogWarning("", ex);
                            }
                        }  
                    }
                    baseRetVal.Result = new JObject
                    {
                        {"uiSelector", uiSelector}
                    }.ToString();
                    return baseRetVal;
                }
            }
            else if (baseRetVal.Code == "-1")
            {
                baseRetVal.Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage();
                return baseRetVal;
            }
            else
            {
                baseRetVal.Result = "fail";
                return baseRetVal;
            }
        }
    }
}
