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
    [ClientActionName("screenshotSelectUi")]
    internal class UI_Selector_Screenshot_Provider : BaseActionProvider
    {
        public UI_Selector_Screenshot_Provider(ILogger aLogger) : base(aLogger)
        {

        }
        public UI_Selector_Screenshot_Provider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {
            NeedSTA = true;
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction 
            {
                PluginAction = "Excute",
                PluginName = "Element.CaptureByRegion",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            //var baseReqVal = base.ResolveReqVal(aReqVal);
            return new JObject 
            {
                { "filepath", m_APicturePath}
            }.ToString();
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRetVal = base.ResolveRetVal(aRetValFromPlugin);
            if (baseRetVal.Code == "0")
            {
                if (string.IsNullOrEmpty(baseRetVal.Result))
                {
                    baseRetVal.Result = "fail";
                    return baseRetVal;
                }
                else
                {
                    var selector = JToken.Parse(baseRetVal.Result);
                    var uiSelector = new JObject();
                    if (selector is JArray)
                    {
                        uiSelector = new JObject
                        {
                            {"selector",selector},
                            {"shot",m_RPicturePath}
                        };
                        try
                        {
                            uiSelector.Add("info", new JObject
                            {
                               {"windowType",uiSelector["selector"][0]["selector"]},
                               {"windowName",uiSelector["selector"][1]["ClassName"]},
                               {"elementName",uiSelector["selector"][1]["ControlType"]},
                               {"catureType","cvScreenshot"},
                            });
                        }
                        catch (Exception ex)
                        {
                            m_Logger.LogWarning("", ex);
                            uiSelector.Add("info", new JObject
                            {
                               {"windowType",uiSelector["selector"][0]["selector"]},
                               {"windowName",""},
                               {"elementName",""},
                               {"catureType","cvScreenshot"},
                            });
                        }
                        uiSelector["info"]["catureType"] = "cvScreenshot";
                        var filePathParent = uiSelector["selector"][0] as JObject;
                        filePathParent?.Remove("filePath");
                    }
                    else
                    {
                        uiSelector = selector as JObject;
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
                            catch (Exception ex)
                            {
                                m_Logger.LogWarning("", ex);
                            }
                        }
                    }
                                     
                    baseRetVal.Result = new JObject
                    {
                       {"uiSelector",uiSelector},
                    }.ToString();
                    return baseRetVal;
                }
            }
            else
            {
                baseRetVal.Result = "fail";
                return baseRetVal;
            }
        }
    }
}
