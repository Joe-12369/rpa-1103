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
    [ClientActionName("cvselectUi")]
    internal class UI_Selector_CV_Provider : BaseActionProvider
    {
        private bool m_HasKey_IsScreenshot;
        public UI_Selector_CV_Provider(ILogger aLogger) : base(aLogger)
        {

        }
        public UI_Selector_CV_Provider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {
            NeedSTA = true;
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            var jo = JObject.Parse(aReqVal);
            var IsScreenshot = jo.GetValue("IsScreenshot");
            if (IsScreenshot != null)
            {
                m_HasKey_IsScreenshot = true;
                return new PluginRunAction
                {
                    PluginAction = "Excute",
                    PluginName = "Element.CaptureByRegion",
                };
            }
            else
            {
                m_HasKey_IsScreenshot = false;
                return new PluginRunAction
                {
                    PluginAction = "Excute",
                    PluginName = "Element.Capture",
                };
            }
        }

        public override string ResolveReqVal(string aReqVal)
        {
            //var baseReqVal = base.ResolveReqVal(aReqVal);
            if (m_HasKey_IsScreenshot)
            {
                return new JObject 
                {
                    {"filepath",m_APicturePath }
                }.ToString();
            }
            else
            {
                return new JObject
                {
                    {"filepath",m_APicturePath },
                    {"isCVCapture",true },
                    {"cvServer",cvPickServer },
                    {"InspectStatus","0" },
                }.ToString();
            }
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
                    var uiSelector = new JObject
                    {
                        { "cv", JToken.Parse(baseRetVal.Result) },
                        { "shot", m_RPicturePath }
                    };
                    try
                    {
                        uiSelector.Add("info", new JObject
                        {
                            {"windowType",uiSelector["cv"][0]["selector"]},
                            {"windowName",uiSelector["cv"][1]["ClassName"]},
                            {"elementName",uiSelector["cv"][1]["ControlType"]},
                        });

                    }
                    catch(Exception ex)
                    {
                        m_Logger.LogWarning("", ex);
                        uiSelector.Add("info", new JObject
                        {
                            {"windowType",uiSelector["cv"][0]["selector"]},
                            {"windowName",""},
                            {"elementName",""},
                        });
                    }
                    JObject parentJO = uiSelector["cv"][0] as JObject;
                    parentJO.Remove("filePath");

                    baseRetVal.Result = new JObject
                    {
                        {"uiSelector",uiSelector },
                    }.ToString();
                    return baseRetVal;
                }    
            }
            else
            {
                return baseRetVal;
            }
        }
    }
}
